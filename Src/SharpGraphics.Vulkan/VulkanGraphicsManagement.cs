using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;
using Vk = Vulkan;
using VK = Vulkan.Vk;
using static Vulkan.Ext;
using System.Diagnostics.CodeAnalysis;

namespace SharpGraphics.Vulkan
{

    /// <summary>
    /// Represents a <see cref="Vk.VkInstance"/>.
    /// First Creation will determine the maximum <see cref="Vk.Version"/>
    /// </summary>
    public sealed class VulkanGraphicsManagement : GraphicsManagement
    {

        public readonly struct LayerRequest : IEquatable<LayerRequest>
        {
            public readonly string name;
            public readonly bool mandatory;

            public LayerRequest(string name, bool mandatory)
            {
                this.name = name;
                this.mandatory = mandatory;
            }

            public override bool Equals(object? obj) => obj is LayerRequest request && Equals(request);
            public bool Equals(LayerRequest other) => name == other.name;
            public override int GetHashCode() => 363513814 + EqualityComparer<string>.Default.GetHashCode(name);
            public static bool operator ==(LayerRequest left, LayerRequest right) => left.Equals(right);
            public static bool operator !=(LayerRequest left, LayerRequest right) => !(left == right);
        }

        internal enum FeatureRequestType : uint { Mandatory = 0u, MandatoryOnlyForPresent = 1u, Optional = 2u }
        internal readonly struct ExtensionRequest : IEquatable<ExtensionRequest>
        {
            public readonly string name;
            public readonly FeatureRequestType mandatory;

            public ExtensionRequest(string name, FeatureRequestType mandatory)
            {
                this.name = name;
                this.mandatory = mandatory;
            }

            public override bool Equals(object? obj) => obj is ExtensionRequest request && Equals(request);
            public bool Equals(ExtensionRequest other) => name == other.name;
            public override int GetHashCode() => 363513814 + EqualityComparer<string>.Default.GetHashCode(name);
            public static bool operator ==(ExtensionRequest left, ExtensionRequest right) => left.Equals(right);
            public static bool operator !=(ExtensionRequest left, ExtensionRequest right) => !(left == right);
        }

#pragma warning disable CS0649 // The compiler detected an uninitialized private or internal field declaration that is never assigned a value.
        private struct VkXcbSurfaceCreateInfoKHR
        {
            public Vk.VkStructureType sType;
            public IntPtr pNext;
            public uint flags;
            public IntPtr connection;
            public IntPtr window;
        }
#pragma warning restore CS0649 // The compiler detected an uninitialized private or internal field declaration that is never assigned a value.

        #region Fields

        private bool _isDisposed;

        private static volatile bool _isVulkanInitialized;
        private delegate Vk.VkResult vkEnumerateInstanceVersion(out uint maxApiVersion);

        private delegate Vk.VkResult vkEnumerateInstanceLayerPropertiesCount(out uint layerCount, IntPtr layerProperties);
        private delegate Vk.VkResult vkEnumerateInstanceLayerProperties(out uint layerCount, out Vk.VkLayerProperties layerProperties);
        private static vkEnumerateInstanceLayerPropertiesCount? _vkEnumerateInstanceLayerPropertiesCountDelegate;
        private static vkEnumerateInstanceLayerProperties? _vkEnumerateInstanceLayerPropertiesDelegate;

        
        private Vk.VkInstance _instance;

        private readonly InstanceDebugExtension? _instanceDebugExtension;

        private readonly VulkanGraphicsDeviceInfo[] _deviceInfos;

        #endregion

        #region Properties

        internal Vk.VkInstance Instance { get => _instance; }
        internal ReadOnlySpan<VulkanGraphicsDeviceInfo> VkAvailableDevices => _deviceInfos;

        public static Version? VulkanAPIVersion { get; private set; }

        public bool IsDisposed => _isDisposed;

        public override ReadOnlySpan<GraphicsDeviceInfo> AvailableDevices => _deviceInfos;

        #endregion

        #region Constructors

        private VulkanGraphicsManagement(OperatingSystem operatingSystem, ISet<ExtensionRequest> requestedExtensions, ISet<LayerRequest> requestedLayers, ISet<ExtensionRequest> requestedDeviceExtensions, DebugLevel debugLevel): base(operatingSystem, debugLevel)
        {
            _isDisposed = false;

            IEnumerable<string> supportedLayers = GetSupportedVKLayers(requestedLayers);

            //Create RawList of requested extensions for VKInstance creation
            RawList<IntPtr> layers = new RawList<IntPtr>(supportedLayers.Count());
            List<RawString> layerStrings = new List<RawString>(supportedLayers.Count());
            foreach (string requestedLayer in supportedLayers)
            {
                layerStrings.Add(new RawString(requestedLayer));
                layers.Add(layerStrings.Last());
            }

            //Generate requested extension strings and check availability
            IEnumerable<string> supportedExtensions = GetSupportedVKExtensions(requestedExtensions, out bool isPresentSupported);

            //Create NativeList of requested extensions for VKInstance creation
            RawList<IntPtr> extensions = new RawList<IntPtr>(requestedExtensions.Count());
            List<RawString> extensionStrings = new List<RawString>(requestedExtensions.Count());
            foreach (string requestedExtension in supportedExtensions)
            {
                extensionStrings.Add(new RawString(requestedExtension));
                extensions.Add(extensionStrings.Last());
            }

            if (VulkanAPIVersion == null)
                throw new Exception("Vulkan Initialization failed!");

            //Instance Create Info
            using RawString applicationName = new RawString("SharpGraphicsVK"); //TODO: Vulkan Application and Engine name
            using RawString engineName = new RawString("SharpGraphicsVK");
            Vk.VkApplicationInfo applicationInfo = new Vk.VkApplicationInfo()
            {
                sType = Vk.VkStructureType.ApplicationInfo,

                apiVersion = new Vk.Version((uint)VulkanAPIVersion.Major, (uint)VulkanAPIVersion.Minor, (uint)VulkanAPIVersion.Build),
                applicationVersion = new Vk.Version(1, 0, 0),
                engineVersion = new Vk.Version(1, 0, 0),

                pApplicationName = applicationName,
                pEngineName = engineName,
            };

            Vk.VkInstanceCreateInfo instanceCreateInfo = Vk.VkInstanceCreateInfo.New();
            instanceCreateInfo.pApplicationInfo = UnsafeExtension.AsIntPtr(ref applicationInfo);
            if (requestedExtensions != null && requestedExtensions.Count > 0)
            {
                instanceCreateInfo.enabledExtensionCount = extensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = extensions.Pointer;
            }
            if (requestedLayers != null && requestedLayers.Count > 0)
            {
                instanceCreateInfo.enabledLayerCount = layers.Count;
                instanceCreateInfo.ppEnabledLayerNames = layers.Pointer;
            }

            if (supportedExtensions.Contains("VK_KHR_portability_enumeration"))
                instanceCreateInfo.flags |= 0x00000001; //VK_INSTANCE_CREATE_ENUMERATE_PORTABILITY_BIT_KHR

            //Create Instance
            Vk.VkResult instanceCreationResult = VK.vkCreateInstance(ref instanceCreateInfo, IntPtr.Zero, out _instance);
            if (instanceCreationResult != Vk.VkResult.Success)
                throw new Exception($"Vulkan Instance creation error: {instanceCreationResult}");
            VK.LoadInstanceFunctionPointers(_instance);

#if DEBUG
            if (_debugLevel != DebugLevel.None)
                _instanceDebugExtension = new InstanceDebugExtension(this, supportedExtensions);
#endif

            foreach (RawString layerString in layerStrings)
                layerString.Dispose();
            layers.Dispose();
            foreach (RawString extensionString in extensionStrings)
                extensionString.Dispose();
            extensions.Dispose();

            Debug.WriteLine("Vulkan Instance created!");


            //Getting Device Infos
            Vk.VkPhysicalDevice[] physicalDevices = GetPhysicalDevices(_instance);
            List<VulkanGraphicsDeviceInfo> deviceInfos = new List<VulkanGraphicsDeviceInfo>(physicalDevices.Length);
            for (int i = 0; i < physicalDevices.Length; i++)
            {
                //Version Check
                VK.vkGetPhysicalDeviceProperties(physicalDevices[i], out Vk.VkPhysicalDeviceProperties properties);
                Vk.Version apiVersion = new Vk.Version(properties.apiVersion >> 22, (properties.apiVersion >> 12) & 0x3ff, properties.apiVersion & 0xfff);

                if (apiVersion.Major < 1u /*&& properties.limits.maxImageDimension2D < 4096*/)
                    continue;

                //Queue Family Check
                VK.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevices[i], out uint queueFamilyCount, IntPtr.Zero);
                if (queueFamilyCount == 0)
                    continue; //Technically, this shouldn't be possible

                //Extension Check
                bool isDeviceSupportsPresent = false;
                IEnumerable<string> deviceSupportedExtensions;
                if (requestedDeviceExtensions != null && requestedDeviceExtensions.Count() > 0)
                {
                    VK.vkEnumerateDeviceExtensionProperties(physicalDevices[i], IntPtr.Zero, out uint extensionCount, IntPtr.Zero);
                    if (extensionCount == 0)
                        continue;

                    Span<Vk.VkExtensionProperties> extensionsOnDevice = stackalloc Vk.VkExtensionProperties[(int)extensionCount];
                    VK.vkEnumerateDeviceExtensionProperties(physicalDevices[i], IntPtr.Zero, out extensionCount, out extensionsOnDevice[0]);

                    try
                    {
                        deviceSupportedExtensions = VulkanUtils.GetAvailableExtensions(extensionsOnDevice, requestedDeviceExtensions, out isDeviceSupportsPresent);
                    }
                    catch { continue; }
                }
                else deviceSupportedExtensions = new List<string>();

                deviceInfos.Add(new VulkanGraphicsDeviceInfo(this, physicalDevices[i], queueFamilyCount, deviceSupportedExtensions, isDeviceSupportsPresent));
            }
            _deviceInfos = deviceInfos.ToArray();
        }

        ~VulkanGraphicsManagement() => Dispose(false);

        #endregion

        #region Private Methods

        private static IEnumerable<string> GetSupportedVKExtensions(IEnumerable<ExtensionRequest> requestedExtensions, out bool isPresentSupported)
        {
            //Get Supported Extensions on the system
            if (VK.vkEnumerateInstanceExtensionProperties(IntPtr.Zero, out uint supportedExtensionCount, IntPtr.Zero) != Vk.VkResult.Success || supportedExtensionCount == 0)
                throw new Exception("Extension count error!");

            if (supportedExtensionCount > 0 && requestedExtensions != null && requestedExtensions.Count() > 0)
            {
                Span<Vk.VkExtensionProperties> supportedExtensions = stackalloc Vk.VkExtensionProperties[(int)supportedExtensionCount];
                if (VK.vkEnumerateInstanceExtensionProperties(IntPtr.Zero, out supportedExtensionCount, out supportedExtensions[0]) != Vk.VkResult.Success)
                    throw new Exception("Extention enumeration error!");

                return VulkanUtils.GetAvailableExtensions(supportedExtensions, requestedExtensions, out isPresentSupported);
            }
            else return VulkanUtils.GetAvailableExtensions(stackalloc Vk.VkExtensionProperties[0], requestedExtensions ?? new List<ExtensionRequest>(), out isPresentSupported);
        }

        private static IEnumerable<string> GetSupportedVKLayers(IEnumerable<LayerRequest> requestedLayers)
        {
            //Get Supported Extensions on the system
            if (_vkEnumerateInstanceLayerPropertiesCountDelegate == null || _vkEnumerateInstanceLayerPropertiesCountDelegate(out uint layerCount, IntPtr.Zero) != Vk.VkResult.Success)
                throw new Exception("Layer count error!");

            if (layerCount > 0u && requestedLayers != null && requestedLayers.Count() > 0)
            {
                Vk.VkLayerProperties[] supportedLayers = new Vk.VkLayerProperties[(int)layerCount];
                if (_vkEnumerateInstanceLayerPropertiesDelegate == null || _vkEnumerateInstanceLayerPropertiesDelegate(out layerCount, out supportedLayers[0]) != Vk.VkResult.Success)
                    throw new Exception("Layer enumeration error!");

                return VulkanUtils.GetAvailableLayers(supportedLayers, requestedLayers);
            }
            else return VulkanUtils.GetAvailableLayers(stackalloc Vk.VkLayerProperties[0], requestedLayers ?? new List<LayerRequest>());
        }


        private static Vk.VkPhysicalDevice[] GetPhysicalDevices(in Vk.VkInstance instance)
        {
            uint physicalDeviceCount;
            Vk.VkResult result = VK.vkEnumeratePhysicalDevices(instance, out physicalDeviceCount, IntPtr.Zero);
            if (result != Vk.VkResult.Success || physicalDeviceCount == 0)
                throw new Exception($"Enumerate Physical Devices error: {(result != Vk.VkResult.Success ? result.ToString() : "count == 0")}!");

            Vk.VkPhysicalDevice[] physicalDevices = new Vk.VkPhysicalDevice[(int)physicalDeviceCount];
            result = VK.vkEnumeratePhysicalDevices(instance, out physicalDeviceCount, out physicalDevices[0]);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Enumerating physical devices error: {result}!");

            return physicalDevices;
        }

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
                    Debug.WriteLine($"Disposing Vulkan Instance from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_instanceDebugExtension != null)
                    _instanceDebugExtension.Dispose();

                if (_instance != Vk.VkInstance.Null)
                {
                    VK.vkDestroyInstance(_instance, IntPtr.Zero);
                    _instance = Vk.VkInstance.Null;
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        internal static void InitializeVulkan()
        {
            if (!_isVulkanInitialized)
            {
                vkEnumerateInstanceVersion vkEnumerateInstanceVersionDelegate;
                try { vkEnumerateInstanceVersionDelegate = VulkanUtils.GetVKFunction<vkEnumerateInstanceVersion>(); }
                catch (Exception e) { throw new VulkanGraphicsManagementCreationException("Failed to get vkEnumerateInstanceVersion function pointer, Vulkan is probably not supported or installed on this device!", e); }

                Vk.VkResult vkResult = vkEnumerateInstanceVersionDelegate(out uint maxApiVersion);
                if (vkResult != Vk.VkResult.Success)
                    throw new VulkanGraphicsManagementCreationException($"Failed to enumerate Vulkan Instance Version, reason: {vkResult}!");

                VulkanAPIVersion = new Version((int)(maxApiVersion >> 22), (int)((maxApiVersion >> 12) & 0x3ff), (int)(maxApiVersion & 0xfff), 0);
                Debug.WriteLine("Max Vulkan API Version: " + VulkanAPIVersion);

                //Get supported Layers
                try
                {
                    _vkEnumerateInstanceLayerPropertiesCountDelegate = VulkanUtils.GetVKFunction<vkEnumerateInstanceLayerPropertiesCount>("vkEnumerateInstanceLayerProperties");
                    _vkEnumerateInstanceLayerPropertiesDelegate = VulkanUtils.GetVKFunction<vkEnumerateInstanceLayerProperties>();
                }
                catch (Exception e)
                {
                    throw new VulkanGraphicsManagementCreationException("Failed to get vkEnumerateInstanceLayerProperties function pointers!", e);
                }


                _isVulkanInitialized = true;
            }
        }

        internal void CreateSurface(IGraphicsView view, out Vk.VkSurfaceKHR surface)
        {
            Vk.VkResult surfaceCreationResult;
            
            //Surface creation
            switch (_operatingSystem)
            {
                case OperatingSystem.Windows:
                    {
                        if (view.PlatformSpecificViewInfo is WindowsSpecificViewInfo windowsInfo)
                        {
                            Vk.VkWin32SurfaceCreateInfoKHR win32SurfaceCreateInfo = new Vk.VkWin32SurfaceCreateInfoKHR()
                            {
                                sType = Vk.VkStructureType.Win32SurfaceCreateInfoKHR,
                                hinstance = windowsInfo.ProcessHandle,
                                hwnd = view.ViewHandle,
                            };
                            surfaceCreationResult = VK.vkCreateWin32SurfaceKHR(_instance, ref win32SurfaceCreateInfo, IntPtr.Zero, out surface);
                        }
                        else throw new ArgumentException($"PlatformSpecificViewInfo is not for the specified {_operatingSystem} Platform.");
                    }
                    break;

                //case OperatingSystem.UWP: break;

                case OperatingSystem.Linux:
                    {
                        switch (view.PlatformSpecificViewInfo)
                        {
                            case LinuxX11SpecificViewInfo linuxX11Info:
                                {
                                    using PinnedObject<VkXcbSurfaceCreateInfoKHR> vkXcbSurfaceCreateInfoKHR = new PinnedObject<VkXcbSurfaceCreateInfoKHR>(new VkXcbSurfaceCreateInfoKHR()
                                    {
                                        sType = Vk.VkStructureType.XcbSurfaceCreateInfoKHR,
                                        connection = linuxX11Info.X11ServerConnectionHandle,
                                        window = view.ViewHandle,
                                    });
                                    surfaceCreationResult = VK.vkCreateXcbSurfaceKHR(_instance, vkXcbSurfaceCreateInfoKHR.pointer, IntPtr.Zero, out surface);
                                }
                                break;

                            default: throw new ArgumentException($"PlatformSpecificViewInfo is not for the specified {_operatingSystem} Platform.");
                        }
                    }
                    break;

                case OperatingSystem.Android:
                    {
                        Vk.VkAndroidSurfaceCreateInfoKHR androidSurfaceCreateInfoKHR = new Vk.VkAndroidSurfaceCreateInfoKHR()
                        {
                            sType = Vk.VkStructureType.AndroidSurfaceCreateInfoKHR,
                            window = view.ViewHandle,
                        };
                        surfaceCreationResult = VK.vkCreateAndroidSurfaceKHR(_instance, ref androidSurfaceCreateInfoKHR, IntPtr.Zero, out surface);
                    }
                    break;

                case OperatingSystem.MacOS:
                    {
                        Vk.VkMetalSurfaceCreateInfoEXT metalSurfaceCreateInfoEXT = new Vk.VkMetalSurfaceCreateInfoEXT()
                        {
                            sType = Vk.VkStructureType.MetalSurfaceCreateInfoEXT,
                            pLayer = view.ViewHandle,
                        };
                        surfaceCreationResult = VK.vkCreateMetalSurfaceEXT(_instance, ref metalSurfaceCreateInfoEXT, IntPtr.Zero, out surface);
                    }
                    break;

                /*case OperatingSystem.iOS:
                    break;
                case OperatingSystem.Tizen:
                    break;*/

                default: throw new PlatformNotSupportedException($"Current {_operatingSystem} platform does not support Presentation!");
            }

            if (surfaceCreationResult != Vk.VkResult.Success)
                throw new Exception($"Surface Creation Error: {surfaceCreationResult}");
        }

        #endregion

        #region Public Methods

        public static VulkanGraphicsManagement Create(OperatingSystem operatingSystem, DebugLevel debugLevel = DebugLevel.None) => Create(operatingSystem, null, debugLevel);
        public static VulkanGraphicsManagement Create(OperatingSystem operatingSystem, ReadOnlySpan<LayerRequest> layerRequests, DebugLevel debugLevel = DebugLevel.None)
        {
            InitializeVulkan();

            HashSet<LayerRequest> requestedLayers = new HashSet<LayerRequest>();
            for (int i = 0; i < layerRequests.Length; i++)
                requestedLayers.Add(layerRequests[i]);
            HashSet<ExtensionRequest> requestedExtensions = new HashSet<ExtensionRequest>();
            HashSet<ExtensionRequest> requestedDeviceExtensions = new HashSet<ExtensionRequest>();

            //Debug Features
#if DEBUG
            if (debugLevel != DebugLevel.None)
            {
                requestedLayers.Add(new LayerRequest("VK_LAYER_KHRONOS_validation", false));
                requestedLayers.Add(new LayerRequest("VK_LAYER_LUNARG_standard_validation", false));
                requestedExtensions.Add(new ExtensionRequest("VK_EXT_debug_report", FeatureRequestType.Optional));
                requestedExtensions.Add(new ExtensionRequest("VK_EXT_debug_utils", FeatureRequestType.Optional));
                requestedDeviceExtensions.Add(new ExtensionRequest("VK_EXT_debug_report", FeatureRequestType.Optional));
                requestedDeviceExtensions.Add(new ExtensionRequest("VK_EXT_debug_utils", FeatureRequestType.Optional));
            }
#endif

            //Present Features
            requestedExtensions.Add(new ExtensionRequest("VK_KHR_surface", FeatureRequestType.MandatoryOnlyForPresent));
            requestedDeviceExtensions.Add(new ExtensionRequest("VK_KHR_swapchain", FeatureRequestType.MandatoryOnlyForPresent));

            switch (operatingSystem)
            {
                case OperatingSystem.Windows:
                    requestedExtensions.Add(new ExtensionRequest("VK_KHR_win32_surface", FeatureRequestType.MandatoryOnlyForPresent));
                    break;

                //case OperatingSystem.UWP: break;

                case OperatingSystem.Linux:
                    //requestedExtensions.Add(new ExtensionRequest("VK_KHR_xlib_surface", FeatureRequestType.MandatoryOnlyForPresent));
                    requestedExtensions.Add(new ExtensionRequest("VK_KHR_xcb_surface", FeatureRequestType.MandatoryOnlyForPresent));
                    break;

                case OperatingSystem.Android:
                    requestedExtensions.Add(new ExtensionRequest("VK_KHR_android_surface", FeatureRequestType.MandatoryOnlyForPresent));
                    break;

                //case OperatingSystem.Tizen: break;

                case OperatingSystem.MacOS:
                    requestedExtensions.Add(new ExtensionRequest("VK_MVK_moltenvk", FeatureRequestType.Optional));
                    requestedExtensions.Add(new ExtensionRequest("VK_KHR_portability_enumeration", FeatureRequestType.Optional));
                    requestedExtensions.Add(new ExtensionRequest("VK_EXT_metal_surface", FeatureRequestType.MandatoryOnlyForPresent));
                    break;

                //case OperatingSystem.iOS: break;
            }


            return new VulkanGraphicsManagement(operatingSystem, requestedExtensions, requestedLayers, requestedDeviceExtensions, debugLevel);
        }

        public override IGraphicsDevice CreateGraphicsDevice(in GraphicsDeviceRequest deviceRequest)
        {
            //Check valid Device
            if (deviceRequest.deviceIndex >= _deviceInfos.Length)
                throw new ArgumentOutOfRangeException($"Device index {deviceRequest.deviceIndex} is greater than available Devices ({_deviceInfos.Length})!");
            VulkanGraphicsDeviceInfo deviceInfo = _deviceInfos[deviceRequest.deviceIndex];
            ReadOnlySpan<VulkanGraphicsCommandProcessorGroupInfo> deviceCommandProcessors = deviceInfo.VkCommandProcessorGroups;

            //Check valid CommandProcessors
            ReadOnlySpan<CommandProcessorGroupRequest> commandProcessorGroupRequests = deviceRequest.CommandProcessorGroupRequests;
            if (commandProcessorGroupRequests.Length == 0)
                throw new ArgumentOutOfRangeException("At least one CommandProcessorGroup must be requested!");

            for (int i = 0; i < commandProcessorGroupRequests.Length; i++)
            {
                if (commandProcessorGroupRequests[i].groupIndex >= deviceCommandProcessors.Length)
                    throw new ArgumentOutOfRangeException($"CommandProcessorGroup index {commandProcessorGroupRequests[i].groupIndex} is not supported by the Device!");
                for (int j = i + 1; j < commandProcessorGroupRequests.Length; j++)
                    if (commandProcessorGroupRequests[i].groupIndex == commandProcessorGroupRequests[j].groupIndex)
                        throw new ArgumentOutOfRangeException($"CommandProcessorGroup index {commandProcessorGroupRequests[i].groupIndex} is specified multiple times! Each index must be specified only once.");

                VulkanGraphicsCommandProcessorGroupInfo commandProcessorGroupInfo = deviceCommandProcessors[(int)commandProcessorGroupRequests[i].groupIndex];
                if (commandProcessorGroupRequests[i].Count == 0u)
                     throw new ArgumentOutOfRangeException($"CommandProcessorGroup at index {commandProcessorGroupRequests[i].groupIndex} has only 0 CommandProcessor count request! There must be at least one request for every requested CommandProcessorGroup!");
                if (commandProcessorGroupRequests[i].Count > commandProcessorGroupInfo.Count)
                    throw new ArgumentOutOfRangeException($"CommandProcessorGroup at index {commandProcessorGroupRequests[i].groupIndex} has only {commandProcessorGroupInfo.Count} available processors instead of the requested {commandProcessorGroupRequests[i].Count}!");

                foreach (CommandProcessorRequest commandProcessorRequest in commandProcessorGroupRequests[i].CommandProcessorRequests)
                    if (commandProcessorRequest.priority < 0f || commandProcessorRequest.priority > 1f)
                        throw new ArgumentOutOfRangeException($"CommandProcessorGroup at index {commandProcessorGroupRequests[i].groupIndex} must have all of the requested priorities between 0.0f and 1.0f!");
            }

            if (deviceRequest.graphicsView != null)
            {
                //Check valid Present CommandProcessor
                CommandProcessorGroupRequest presentProcessorRequest = new CommandProcessorGroupRequest(0u, 0u);
                for (int i = 0; i < commandProcessorGroupRequests.Length; i++)
                    if (commandProcessorGroupRequests[i].groupIndex == deviceRequest.presentCommandProcessor.groupIndex)
                        presentProcessorRequest = commandProcessorGroupRequests[i];
                if (presentProcessorRequest.Count == 0u)
                    throw new ArgumentOutOfRangeException($"Present CommandProcessorGroup index {deviceRequest.presentCommandProcessor.groupIndex} is not a requested Group!");

                if (deviceRequest.presentCommandProcessor.commandProcessorIndex >= presentProcessorRequest.Count)
                    throw new ArgumentOutOfRangeException($"Present CommandProcessorGroup index {deviceRequest.presentCommandProcessor.groupIndex} has only {presentProcessorRequest.Count} requested CommandProcessors, which is not enough for the requested PresentCommandProcessor Index {deviceRequest.presentCommandProcessor.commandProcessorIndex}!");

                //Check valid Surface
                Vk.VkSurfaceKHR vkSurface = Vk.VkSurfaceKHR.Null;
                try
                {
                    CreateSurface(deviceRequest.graphicsView, out vkSurface);
                    VulkanGraphicsCommandProcessorGroupInfo presentCommandProcessorGroupInfo = deviceCommandProcessors[(int)deviceRequest.presentCommandProcessor.groupIndex];
                    if (!presentCommandProcessorGroupInfo.IsSurfaceSupported(vkSurface))
                        throw new ArgumentException($"Present CommandProcessorGroup index {deviceRequest.presentCommandProcessor.groupIndex} does not support presenting on the provided GraphicsView!");

                    //Everything is fine, create device
                    VulkanGraphicsDevice device = new VulkanGraphicsDevice(this, deviceRequest, vkSurface);
                    vkSurface = Vk.VkSurfaceKHR.Null; //Everything is still fine, prevent destroying surface in finalizer
                    return device;
                }
                finally
                {
                    if (vkSurface != Vk.VkSurfaceKHR.Null)
                        VK.vkDestroySurfaceKHR(_instance, vkSurface, IntPtr.Zero);
                }
            }
            else return new VulkanGraphicsDevice(this, deviceRequest, Vk.VkSurfaceKHR.Null);
            
        }

        #endregion

    }

    public class VulkanGraphicsManagementCreationException : GraphicsManagementCreationException
    {

        public VulkanGraphicsManagementCreationException() { }
        public VulkanGraphicsManagementCreationException(string message) : base(message) { }
        public VulkanGraphicsManagementCreationException(string message, Exception innerException) : base(message, innerException) { }

    }

}
