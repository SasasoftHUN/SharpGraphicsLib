using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class InstanceDebugExtension : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsManagement _instance;

        private readonly Vk.PFN_vkDebugReportCallbackEXT? _debugReportCallbackDelegate;
        private Vk.VkDebugReportCallbackEXT _debugReportCallback;
        private GCHandle _debugReportCallbackHandle;

        private readonly Vk.PFN_vkDebugUtilsMessengerCallbackEXT? _debugMessengerCallbackDelegate;
        private Vk.VkDebugUtilsMessengerEXT _debugMessengerCallback;
        private GCHandle _debugMessengerCallbackHandle;

        #endregion

        #region Constructors

        internal InstanceDebugExtension(in VulkanGraphicsManagement instance, IEnumerable<string> supportedExtensions)
        {
            _instance = instance;

            if (supportedExtensions.Contains("VK_EXT_debug_utils"))
            {
                _debugMessengerCallbackDelegate = DebugMessengerCallback;
                _debugMessengerCallbackHandle = GCHandle.Alloc(_debugMessengerCallbackDelegate);
                Vk.VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = new Vk.VkDebugUtilsMessengerCreateInfoEXT()
                {
                    sType = Vk.VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                    messageSeverity = instance.DebugLevel switch
                    {
                        DebugLevel.Errors => Vk.VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT,
                        DebugLevel.Important => Vk.VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT | Vk.VkDebugUtilsMessageSeverityFlagsEXT.WarningEXT,
                        DebugLevel.Everything => Vk.VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT | Vk.VkDebugUtilsMessageSeverityFlagsEXT.WarningEXT | Vk.VkDebugUtilsMessageSeverityFlagsEXT.InfoEXT | Vk.VkDebugUtilsMessageSeverityFlagsEXT.VerboseEXT,
                        _ => Vk.VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT,
                    },
                    messageType = instance.DebugLevel switch
                    {
                        DebugLevel.Errors => Vk.VkDebugUtilsMessageTypeFlagsEXT.GeneralEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.ValidationEXT,
                        DebugLevel.Important => Vk.VkDebugUtilsMessageTypeFlagsEXT.GeneralEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.ValidationEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.PerformanceEXT,
                        DebugLevel.Everything => Vk.VkDebugUtilsMessageTypeFlagsEXT.GeneralEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.ValidationEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.PerformanceEXT,
                        _ => Vk.VkDebugUtilsMessageTypeFlagsEXT.GeneralEXT | Vk.VkDebugUtilsMessageTypeFlagsEXT.ValidationEXT,
                    },
                    pfnUserCallback = Marshal.GetFunctionPointerForDelegate(_debugMessengerCallbackDelegate),
                };
                VK.vkCreateDebugUtilsMessengerEXT(instance.Instance, ref messengerCreateInfo, IntPtr.Zero, out _debugMessengerCallback);
            }
            else if (supportedExtensions.Contains("VK_EXT_debug_report"))
            {
                _debugReportCallbackDelegate = DebugReportCallback;
                _debugReportCallbackHandle = GCHandle.Alloc(_debugReportCallbackDelegate);
                Vk.VkDebugReportCallbackCreateInfoEXT callbackCreateInfo = new Vk.VkDebugReportCallbackCreateInfoEXT()
                {
                    sType = Vk.VkStructureType.DebugReportCallbackCreateInfoEXT,
                    flags = instance.DebugLevel switch
                    {
                        DebugLevel.Errors => Vk.VkDebugReportFlagsEXT.ErrorEXT,
                        DebugLevel.Important => Vk.VkDebugReportFlagsEXT.ErrorEXT | Vk.VkDebugReportFlagsEXT.PerformanceWarningEXT | Vk.VkDebugReportFlagsEXT.WarningEXT,
                        DebugLevel.Everything => Vk.VkDebugReportFlagsEXT.ErrorEXT | Vk.VkDebugReportFlagsEXT.DebugEXT | Vk.VkDebugReportFlagsEXT.PerformanceWarningEXT | Vk.VkDebugReportFlagsEXT.WarningEXT | Vk.VkDebugReportFlagsEXT.InformationEXT,
                        _ => Vk.VkDebugReportFlagsEXT.ErrorEXT,
                    },
                    pfnCallback = Marshal.GetFunctionPointerForDelegate(_debugReportCallbackDelegate),
                };
                VK.vkCreateDebugReportCallbackEXT(instance.Instance, ref callbackCreateInfo, IntPtr.Zero, out _debugReportCallback);
            }
        }


        ~InstanceDebugExtension()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }


        #endregion

        #region Private Methods

        private unsafe Vk.VkBool32 DebugReportCallback(Vk.VkDebugReportFlagsEXT flags, Vk.VkDebugReportObjectTypeEXT objectType, ulong obj, UIntPtr location, int messageCode, IntPtr layerPrefix, IntPtr message, IntPtr userData)
        {
            Debug.WriteLine($"VK Debug Report {flags} code {messageCode}: {UnsafeExtension.ParseByteString((byte*)message)}");
            return Vk.VkBool32.False;
        }

        private unsafe Vk.VkBool32 DebugMessengerCallback(Vk.VkDebugUtilsMessageSeverityFlagsEXT severity, Vk.VkDebugUtilsMessageTypeFlagsEXT type, IntPtr callbackData, IntPtr userData)
        {
            Vk.VkDebugUtilsMessengerCallbackDataEXT callback = Marshal.PtrToStructure<Vk.VkDebugUtilsMessengerCallbackDataEXT>(callbackData);
            Debug.WriteLine($"VK Debug Messenger {severity} - {type}, code {callback.messageIdNumber} ({UnsafeExtension.ParseByteString((byte*)callback.pMessageIdName)}): {UnsafeExtension.ParseByteString((byte*)callback.pMessage)}");
            return Vk.VkBool32.False;
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                if (!disposing)
                    Debug.WriteLine($"Disposing InstanceDebugExtension from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_debugReportCallbackHandle.IsAllocated)
                    _debugReportCallbackHandle.Free();
                if (_debugMessengerCallbackHandle.IsAllocated)
                    _debugMessengerCallbackHandle.Free();

                if (_debugReportCallback != Vk.VkDebugReportCallbackEXT.Null || _debugMessengerCallback != Vk.VkDebugUtilsMessengerEXT.Null)
                {
                    if (_instance != null && !_instance.IsDisposed)
                    {
                        if (_debugReportCallback != Vk.VkDebugReportCallbackEXT.Null)
                        {
                            VK.vkDestroyDebugReportCallbackEXT(_instance.Instance, _debugReportCallback, IntPtr.Zero);
                            _debugReportCallback = Vk.VkDebugReportCallbackEXT.Null;
                        }
                        if (_debugMessengerCallback != Vk.VkDebugUtilsMessengerEXT.Null)
                        {
                            VK.vkDestroyDebugUtilsMessengerEXT(_instance.Instance, _debugMessengerCallback, IntPtr.Zero);
                            _debugMessengerCallback = Vk.VkDebugUtilsMessengerEXT.Null;
                        }
                    }
                    else Debug.WriteLine("Warning: InstanceDebugExtension cannot be disposed properly because VulkanGraphicsManagement is already Disposed!");
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
