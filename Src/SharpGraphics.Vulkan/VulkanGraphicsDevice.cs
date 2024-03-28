using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;
using Vk = Vulkan;
using VK = Vulkan.Vk;
using SharpGraphics.Shaders;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpGraphics.Vulkan
{
    /// <summary>
    /// Represents the <see cref="Vk.VkPhysicalDevice"/> and <see cref="Vk.VkDevice"/> for a <see cref="Vk.VkInstance"/>.
    /// Contains all of the requested <see cref="VulkanCommandProcessor"/>s, Memory and other Device properties.
    /// </summary>
    internal sealed class VulkanGraphicsDevice : GraphicsDevice, IGraphicsComputeDevice
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsManagement _instance;

        private readonly Vk.VkPhysicalDevice _physicalDevice;
        private Vk.VkDevice _device;

        private readonly VulkanCommandProcessor[] _commandProcessors;
        private readonly VulkanCommandProcessor? _presentProcessor;

        private readonly VulkanGraphicsSwapChain? _swapChain;

        private readonly VulkanGraphicsDeviceInfo _vkDeviceInfo;
        private readonly VulkanGraphicsDeviceFeatures _vkFeatures;
        private readonly VulkanGraphicsDeviceLimits _vkLimits;

        #endregion

        #region Properties

        internal VulkanGraphicsManagement GraphicsManagement => _instance;
        internal Vk.VkPhysicalDevice PhysicalDevice => _physicalDevice;
        internal Vk.VkDevice Device => _device;

        internal VulkanGraphicsDeviceInfo VkDeviceInfo => _vkDeviceInfo;
        internal VulkanGraphicsDeviceFeatures VkFeatures => _vkFeatures;
        internal VulkanGraphicsDeviceLimits VkLimits => _vkLimits;

        public override GraphicsDeviceInfo DeviceInfo => _vkDeviceInfo;
        public override GraphicsDeviceFeatures Features => _vkFeatures;
        public override GraphicsDeviceLimits Limits => _vkLimits;

        public override ReadOnlySpan<GraphicsCommandProcessor> CommandProcessors => _commandProcessors;
        public GraphicsSwapChain? SwapChain => _swapChain;

        #endregion

        #region Constructors

        internal VulkanGraphicsDevice(VulkanGraphicsManagement instance, in GraphicsDeviceRequest deviceRequest, in Vk.VkSurfaceKHR surface): base(instance)
        {
            _isDisposed = false;

            _instance = instance;

            _vkDeviceInfo = instance.VkAvailableDevices[(int)deviceRequest.deviceIndex];
            _vkFeatures = _vkDeviceInfo._features;
            _vkLimits = _vkDeviceInfo._limits;
            _physicalDevice = _vkDeviceInfo._vkPhysicalDevice;

            //Device Create Info
            ReadOnlySpan<CommandProcessorGroupRequest> commandProcessorGroupRequests = deviceRequest.CommandProcessorGroupRequests;
            Span<Vk.VkDeviceQueueCreateInfo> deviceQueueCreateInfos = stackalloc Vk.VkDeviceQueueCreateInfo[commandProcessorGroupRequests.Length];
            int queueTotalCount = 0;
            for (int i = 0; i < commandProcessorGroupRequests.Length; i++)
                queueTotalCount += (int)commandProcessorGroupRequests[i].Count;

            Span<float> queuePriorities = stackalloc float[queueTotalCount];
            int queuePriorityIndex = 0;
            for (int i = 0; i < commandProcessorGroupRequests.Length; i++)
            {
                ReadOnlySpan<CommandProcessorRequest> commandProcessorRequests = commandProcessorGroupRequests[i].CommandProcessorRequests;
                for (int j = 0; j < commandProcessorRequests.Length; j++)
                    queuePriorities[queuePriorityIndex++] = commandProcessorRequests[j].priority;

                deviceQueueCreateInfos[i] = new Vk.VkDeviceQueueCreateInfo()
                {
                    sType = Vk.VkStructureType.DeviceQueueCreateInfo,
                    queueFamilyIndex = commandProcessorGroupRequests[i].groupIndex,
                    queueCount = (uint)commandProcessorRequests.Length,
                    pQueuePriorities = UnsafeExtension.AsIntPtr(queuePriorities.Slice(queuePriorityIndex - commandProcessorRequests.Length, commandProcessorRequests.Length)),
                };
            }

            //Create Device and Queues
            //Create NativeList of requested extensions for VKInstance creation
            RawList<IntPtr> extensions = new RawList<IntPtr>();
            List<RawString> extensionStrings = new List<RawString>();
            if (_vkDeviceInfo._extensions != null)
                foreach (string requestedExtension in _vkDeviceInfo._extensions)
                {
                    extensionStrings.Add(new RawString(requestedExtension));
                    extensions.Add(extensionStrings.Last());
                }

            using PinnedObject<Vk.VkPhysicalDeviceFeatures> pinnedFeatures = new PinnedObject<Vk.VkPhysicalDeviceFeatures>(_vkFeatures.features);
            Vk.VkDeviceCreateInfo deviceCreateInfo = new Vk.VkDeviceCreateInfo()
            {
                sType = Vk.VkStructureType.DeviceCreateInfo,
                queueCreateInfoCount = (uint)deviceQueueCreateInfos.Length,
                pQueueCreateInfos = UnsafeExtension.AsIntPtr(deviceQueueCreateInfos),

                enabledExtensionCount = extensions.Count,
                ppEnabledExtensionNames = extensions.Pointer,

                pEnabledFeatures = pinnedFeatures.pointer,
            };

            Vk.VkResult result = VK.vkCreateDevice(_physicalDevice, ref deviceCreateInfo, IntPtr.Zero, out _device);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkDevice creation error: {result}!");

            extensions.Dispose();
            foreach (RawString extensionString in extensionStrings)
                extensionString.Dispose();
            extensionStrings.Clear();


            VK.LoadDeviceFunctionPointers(_device);

            _commandProcessors = new VulkanCommandProcessor[queueTotalCount];
            int queueIndex = 0;
            for (int i = 0; i < commandProcessorGroupRequests.Length; i++)
            {
                ReadOnlySpan<CommandProcessorRequest> commandProcessorRequests = commandProcessorGroupRequests[i].CommandProcessorRequests;
                for (int j = 0; j < commandProcessorRequests.Length; j++)
                {
                    _commandProcessors[queueIndex] = new VulkanCommandProcessor(this, commandProcessorGroupRequests[i].groupIndex, (uint)j, commandProcessorGroupRequests[i].CommandProcessorRequests[j].priority);
                    if (deviceRequest.presentCommandProcessor.groupIndex == i && deviceRequest.presentCommandProcessor.commandProcessorIndex == j)
                        _presentProcessor = _commandProcessors[queueIndex];
                    queueIndex++;
                }
            }
            
            if (surface.Handle != 0ul && _presentProcessor != null && deviceRequest.graphicsView != null) //TODO: Let user choose seperate present and graphics processors with PresentCommandProcessorRequest
                _swapChain = new VulkanGraphicsSwapChain(this, _presentProcessor, _presentProcessor, surface, deviceRequest.graphicsView);

            Debug.WriteLine("Vulkan Device Created!");
        }

        ~VulkanGraphicsDevice() => Dispose(false);

        #endregion

        #region Private Methods



        #endregion

        #region Protected Methods

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing Vulkan Device from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_swapChain != null)
                    _swapChain.Dispose();

                if (_commandProcessors != null)
                    for (int i = 0; i < _commandProcessors.Length; i++)
                        if (_commandProcessors[i] != null)
                            _commandProcessors[i].Dispose();

                if (_device != Vk.VkDevice.Null)
                {
                    VK.vkDestroyDevice(_device, IntPtr.Zero);
                    _device = Vk.VkDevice.Null;
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        internal Vk.VkSemaphore CreateSemaphore()
        {
            Vk.VkSemaphoreCreateInfo semaphoreCreateInfo = new Vk.VkSemaphoreCreateInfo() { sType = Vk.VkStructureType.SemaphoreCreateInfo };

            if (VK.vkCreateSemaphore(_device, ref semaphoreCreateInfo, IntPtr.Zero, out Vk.VkSemaphore semaphore) == Vk.VkResult.Success)
                return semaphore;

            return Vk.VkSemaphore.Null;
        }
        internal bool CreateSemaphores(uint count, [NotNullWhen(returnValue: true)] out Vk.VkSemaphore[]? semaphores) => CreateSemaphores((int)count, out semaphores);
        internal bool CreateSemaphores(int count, [NotNullWhen(returnValue: true)] out Vk.VkSemaphore[]? semaphores)
        {
            Vk.VkSemaphoreCreateInfo semaphoreCreateInfo = new Vk.VkSemaphoreCreateInfo() { sType = Vk.VkStructureType.SemaphoreCreateInfo };

            semaphores = new Vk.VkSemaphore[count];
            for (int i = 0; i < count; i++)
                if (VK.vkCreateSemaphore(_device, ref semaphoreCreateInfo, IntPtr.Zero, out semaphores[i]) != Vk.VkResult.Success)
                {
                    for (int j = 0; j < i; j++)
                        VK.vkDestroySemaphore(_device, semaphores[j], IntPtr.Zero);
                    semaphores = null;
                    return false;
                }

            return true;
        }
        internal void DestroySemaphores(Vk.VkSemaphore[] semaphores)
        {
            for (int i = 0; i < semaphores.Length; i++)
                if (semaphores[i] != Vk.VkSemaphore.Null)
                {
                    VK.vkDestroySemaphore(_device, semaphores[i], IntPtr.Zero);
                    semaphores[i] = Vk.VkSemaphore.Null;
                }
        }

        internal Vk.VkFence CreateFence(bool isSignaled)
        {
            Vk.VkFenceCreateInfo fenceCreateInfo = new Vk.VkFenceCreateInfo() { sType = Vk.VkStructureType.FenceCreateInfo, flags = isSignaled ? Vk.VkFenceCreateFlags.Signaled : 0u };
            return VK.vkCreateFence(_device, ref fenceCreateInfo, IntPtr.Zero, out Vk.VkFence fence) == Vk.VkResult.Success ? fence : Vk.VkFence.Null;
        }
        internal bool CreateFences(bool isSignaled, uint count, [NotNullWhen(returnValue: true)] out Vk.VkFence[]? fences) => CreateFences(isSignaled, (int)count, out fences);
        internal bool CreateFences(bool isSignaled, int count, [NotNullWhen(returnValue: true)] out Vk.VkFence[]? fences)
        {
            Vk.VkFenceCreateInfo fenceCreateInfo = new Vk.VkFenceCreateInfo() { sType = Vk.VkStructureType.FenceCreateInfo, flags = isSignaled ? Vk.VkFenceCreateFlags.Signaled : 0u };

            fences = new Vk.VkFence[count];
            for (int i = 0; i < count; i++)
                if (VK.vkCreateFence(_device, ref fenceCreateInfo, IntPtr.Zero, out fences[i]) != Vk.VkResult.Success)
                {
                    for (int j = 0; j < i; j++)
                        VK.vkDestroyFence(_device, fences[j], IntPtr.Zero);
                    fences = null;
                    return false;
                }

            return true;
        }
        internal void DestroyFences(in ReadOnlySpan<Vk.VkFence> fences)
        {
            for (int i = 0; i < fences.Length; i++)
                if (fences[i] != Vk.VkFence.Null)
                    VK.vkDestroyFence(_device, fences[i], IntPtr.Zero);
        }

        internal Vk.VkResult AllocateMemory(Vk.VkMemoryRequirements memoryRequirements, Vk.VkMemoryPropertyFlags memoryProperty, ulong size, out Vk.VkDeviceMemory memory)
        {
            //TODO: Use Vulkan GPU Memory Allocator
            //TODO: Listing 2.5 in Learning Vulkan
            for (uint i = 0; i < _vkLimits.memoryProperties.memoryTypeCount; i++)
            {
                if ((memoryRequirements.memoryTypeBits & (1 << (int)i)) != 0 &&
                    _vkLimits.memoryProperties.memoryTypes[i].propertyFlags.HasFlag(memoryProperty))
                {
                    Vk.VkMemoryAllocateInfo memoryAllocateInfo = new Vk.VkMemoryAllocateInfo()
                    {
                        sType = Vk.VkStructureType.MemoryAllocateInfo,
                        allocationSize = size,
                        memoryTypeIndex = i,
                    };

                    return VK.vkAllocateMemory(_device, ref memoryAllocateInfo, IntPtr.Zero, out memory);
                }
            }

            memory = Vk.VkDeviceMemory.Null;
            return Vk.VkResult.ErrorUnknown;
        }

        #endregion

        #region Public Methods

        public override void WaitForIdle() => VK.vkDeviceWaitIdle(_device);

        public IRenderPass CreateRenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps) => new VulkanRenderPass(this, attachments, steps);

        public IGraphicsShaderProgram CompileShaderProgram(in GraphicsShaderSource shaderSource)
        {
            if (shaderSource.shaderSource is ShaderSourceBinary binaryShaderSource)
                return new VulkanGraphicsShaderProgram(this, binaryShaderSource, shaderSource.stage);
            else throw new ArgumentException("VulkanGraphicsDevice can only use Binary Shader Sources!", "shaderSource.shaderSource");
        }

        public IGraphicsPipeline CreatePipeline(in GraphicsPipelineConstuctionParameters constuction) => new VulkanGraphicsPipeline(this, constuction);

        public override IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0u, bool isAligned = true)
             => new VulkanDeviceOnlyDataBuffer<T>(this, dataCapacity, bufferType, isAligned ? bufferType : DataBufferType.Unknown, Vk.VkMemoryPropertyFlags.DeviceLocal);
        public override IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, uint dataCapacity = 0u, bool isAligned = true)
            => new VulkanMappableDataBuffer<T>(this, dataCapacity, bufferType, isAligned ? bufferType : DataBufferType.Unknown, memoryType.ToMemoryPropertyFlags());
        public override IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType alignmentType, uint dataCapacity = 0u)
            => new VulkanStagingDataBuffer<T>(this, alignmentType, dataCapacity);

        public override PipelineResourceLayout CreatePipelineResourceLayout(in PipelineResourceProperties properties) => new VulkanPipelineResourceLayout(this, properties);

        public override ITexture2D CreateTexture2D(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
            => new VulkanTexture2D(this, format.ToVkFormat(), resolution.ToVkExtent2D(), Vk.VkImageLayout.Undefined, textureType, memoryType.ToVkMemoryPropertyFlags(), Vk.VkPipelineStageFlags.VertexShader, mipLevels.CalculateMipLevels(resolution)); //TODO: Find out PipelineStage of earliest usage
        public override ITextureCube CreateTextureCube(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
            => new VulkanTextureCube(this, format.ToVkFormat(), resolution.ToVkExtent2D(), Vk.VkImageLayout.Undefined, textureType, memoryType.ToVkMemoryPropertyFlags(), Vk.VkPipelineStageFlags.VertexShader, mipLevels.CalculateMipLevels(resolution)); //TODO: Find out PipelineStage of earliest usage

        public override TextureSampler CreateTextureSampler(in TextureSamplerConstruction construction) => new VulkanTextureSampler(this, construction);

        #endregion

    }
}
