using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static SharpGraphics.Vulkan.VulkanGraphicsDevice;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanGraphicsDeviceLimits : GraphicsDeviceLimits
    {

        public readonly Vk.VkPhysicalDeviceLimits limits;
        public readonly Vk.VkPhysicalDeviceMemoryProperties memoryProperties;

        internal VulkanGraphicsDeviceLimits(in Vk.VkPhysicalDeviceLimits limits, in Vk.VkPhysicalDeviceMemoryProperties memoryProperties)
        {
            this.limits = limits;
            this.memoryProperties = memoryProperties;
            UniformBufferAlignment = (uint)limits.minUniformBufferOffsetAlignment;
            MaxAnisotropy = limits.maxSamplerAnisotropy;
        }
    }
    internal sealed class VulkanGraphicsDeviceFeatures : GraphicsDeviceFeatures
    {

        public override IEnumerable<ShaderAPIVersion> ShaderAPIVersions { get; }

        public readonly Vk.VkPhysicalDeviceFeatures features;

        internal VulkanGraphicsDeviceFeatures(in Vk.VkPhysicalDeviceFeatures features)
        {
            ShaderAPIVersions = new ShaderAPIVersion[] { new ShaderAPIVersion(ShaderSourceType.Binary, "SPIRV_VK1_0") };

            this.features = features;
            IsBufferPersistentMappingSupported = true;
            IsBufferCoherentMappingSupported = true; //TODO: Sure? How to check support?
            IsBufferCachedMappingSupported = true;
            IsTextureViewSupported = true;
        }
    }

    internal sealed class VulkanGraphicsCommandProcessorGroupInfo : GraphicsCommandProcessorGroupInfo
    {
        private readonly VulkanGraphicsManagement _management;
        private readonly Vk.VkPhysicalDevice _physicalDevice;
        private readonly uint _groupIndex;
        private readonly bool _isDeviceSupportsPresent;

        public VulkanGraphicsCommandProcessorGroupInfo(VulkanGraphicsManagement management, in Vk.VkPhysicalDevice physicalDevice, uint groupIndex, in Vk.VkQueueFamilyProperties properties, bool isDeviceSupportsPresent) :
            base(properties.queueFlags.ToGraphicsCommandProcessorType(), properties.queueCount)
        {
            _management = management;
            _physicalDevice = physicalDevice;
            _groupIndex = groupIndex;
            _isDeviceSupportsPresent = isDeviceSupportsPresent;
        }

        internal bool IsSurfaceSupported(in Vk.VkSurfaceKHR surface)
        {
            if (_isDeviceSupportsPresent && surface != Vk.VkSurfaceKHR.Null)
            {
                VK.vkGetPhysicalDeviceSurfaceSupportKHR(_physicalDevice, _groupIndex, surface, out Vk.VkBool32 presentSupported);
                return presentSupported == Vk.VkBool32.True;
            }
            else return false;
        }

        public override bool IsViewSupported(IGraphicsView graphicsView)
        {
            if (_isDeviceSupportsPresent)
            {
                Vk.VkSurfaceKHR surface = Vk.VkSurfaceKHR.Null;
                try
                {
                    _management.CreateSurface(graphicsView, out surface);
                    return IsSurfaceSupported(surface);
                }
                finally
                {
                    if (surface != Vk.VkSurfaceKHR.Null)
                        VK.vkDestroySurfaceKHR(_management.Instance, surface, IntPtr.Zero);
                }
            }
            else return false;
        }
    }
    internal sealed class VulkanGraphicsDeviceInfo : GraphicsDeviceInfo
    {

        private readonly VulkanGraphicsManagement _management;
        private readonly VulkanGraphicsCommandProcessorGroupInfo[] _commandProcessorGroups;

        private readonly Version _apiVersion;
        private readonly Version _driverVersion;
        private readonly string _name;

        internal readonly Vk.VkPhysicalDevice _vkPhysicalDevice;
        internal readonly VulkanGraphicsDeviceLimits _limits;
        internal readonly VulkanGraphicsDeviceFeatures _features;
        internal readonly IEnumerable<string> _extensions;

        internal ReadOnlySpan<VulkanGraphicsCommandProcessorGroupInfo> VkCommandProcessorGroups => _commandProcessorGroups;

        public override ReadOnlySpan<GraphicsCommandProcessorGroupInfo> CommandProcessorGroups => _commandProcessorGroups;
        public override Version APIVersion => _apiVersion;
        public override Version DriverVersion => _driverVersion;
        public override string Name => _name;

        public override GraphicsDeviceLimits Limits => _limits;
        public override GraphicsDeviceFeatures Features => _features;


        internal VulkanGraphicsDeviceInfo(VulkanGraphicsManagement management, in Vk.VkPhysicalDevice physicalDevice, uint queueFamilyCount, IEnumerable<string> extensions, bool isPresentSupported)
        {
            _management = management;
            AreDetailsAvailable = true;
            IsPresentSupported = isPresentSupported;
            _vkPhysicalDevice = physicalDevice;
            _extensions = extensions;

            VK.vkGetPhysicalDeviceFeatures(physicalDevice, out Vk.VkPhysicalDeviceFeatures features); //TODO: Get Features of Vk1.1, 1.2, 1.3...
            _features = new VulkanGraphicsDeviceFeatures(features);

            VK.vkGetPhysicalDeviceProperties(physicalDevice, out Vk.VkPhysicalDeviceProperties properties);

            //Query Memories
            IntPtr memoryPropertiesPointer = IntPtr.Zero;
            Vk.VkPhysicalDeviceMemoryProperties memoryProperties;
            try
            {
                memoryPropertiesPointer = Marshal.AllocHGlobal(Marshal.SizeOf<Vk.VkPhysicalDeviceMemoryProperties>());
                VK.vkGetPhysicalDeviceMemoryProperties(physicalDevice, memoryPropertiesPointer); // Readonly field prevents using it directly on the field
                memoryProperties = Marshal.PtrToStructure<Vk.VkPhysicalDeviceMemoryProperties>(memoryPropertiesPointer);
            }
            finally
            {
                if (memoryPropertiesPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(memoryPropertiesPointer);
            }

            _limits = new VulkanGraphicsDeviceLimits(properties.limits, memoryProperties);
            _apiVersion = new Version((int)(properties.apiVersion >> 22), (int)((properties.apiVersion >> 12) & 0x3ff), (int)(properties.apiVersion & 0xfff));
            _driverVersion = new Version((int)(properties.driverVersion >> 22), (int)((properties.driverVersion >> 12) & 0x3ff), (int)(properties.driverVersion & 0xfff));
            VendorID = properties.vendorID;
            DeviceID = properties.deviceID;
            Type = properties.deviceType.ToGraphicsDeviceType();
            _name = properties.deviceName.Trim('\0');

            //Get Queue Families
            Span<Vk.VkQueueFamilyProperties> queueFamilyProperties = stackalloc Vk.VkQueueFamilyProperties[(int)queueFamilyCount];
            Span<bool> presentSupportedQueues = stackalloc bool[(int)queueFamilyCount];
            VK.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, out queueFamilyCount, out queueFamilyProperties[0]);

            //Get Queue Family Info
            _commandProcessorGroups = new VulkanGraphicsCommandProcessorGroupInfo[queueFamilyCount];
            for (int i = 0; i < queueFamilyCount; i++)
            {
                _commandProcessorGroups[i] = new VulkanGraphicsCommandProcessorGroupInfo(management, physicalDevice, (uint)i, queueFamilyProperties[i], isPresentSupported);
            }
        }

        public override uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView)
        {
            if (IsPresentSupported)
            {
                List<uint> indicesSupportingView = new List<uint>(_commandProcessorGroups.Length);

                Vk.VkSurfaceKHR surface = Vk.VkSurfaceKHR.Null;
                try
                {
                    _management.CreateSurface(graphicsView, out surface);
                    if (surface != Vk.VkSurfaceKHR.Null)
                        for (int i = 0; i < _commandProcessorGroups.Length; i++)
                            if (_commandProcessorGroups[i].IsSurfaceSupported(surface))
                                indicesSupportingView.Add((uint)i);
                }
                finally
                {
                    if (surface != Vk.VkSurfaceKHR.Null)
                        VK.vkDestroySurfaceKHR(_management.Instance, surface, IntPtr.Zero);
                }

                return indicesSupportingView.ToArray();
            }
            else return new uint[0];
        }

        public override uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView, GraphicsCommandProcessorType requiredType)
        {
            if (IsPresentSupported)
            {
                List<uint> indicesSupportingView = new List<uint>(_commandProcessorGroups.Length);

                Vk.VkSurfaceKHR surface = Vk.VkSurfaceKHR.Null;
                try
                {
                    _management.CreateSurface(graphicsView, out surface);
                    if (surface != Vk.VkSurfaceKHR.Null)
                        for (int i = 0; i < _commandProcessorGroups.Length; i++)
                            if (_commandProcessorGroups[i].Type.HasFlag(requiredType) && _commandProcessorGroups[i].IsSurfaceSupported(surface))
                                indicesSupportingView.Add((uint)i);
                }
                finally
                {
                    if (surface != Vk.VkSurfaceKHR.Null)
                        VK.vkDestroySurfaceKHR(_management.Instance, surface, IntPtr.Zero);
                }

                return indicesSupportingView.ToArray();
            }
            else return new uint[0];
        }
    }

}
