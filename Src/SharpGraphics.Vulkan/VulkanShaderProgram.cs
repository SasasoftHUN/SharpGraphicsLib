using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal abstract class VulkanShaderProgram : IShaderProgram
    {

        #region Fields

        private readonly VulkanGraphicsDevice _device;

        private Vk.VkShaderModule _shaderModule;
        private readonly RawString _mainString; //TODO: Will store the "same" string for ALL shaders
        private readonly Vk.VkPipelineShaderStageCreateInfo _pipelineShaderStageCreateInfo;

        private bool _isDisposed;

        #endregion

        #region Properties

        internal Vk.VkPipelineShaderStageCreateInfo PipelineShaderStageCreateInfo => _pipelineShaderStageCreateInfo;

        #endregion

        #region Constructors

        protected VulkanShaderProgram(VulkanGraphicsDevice device, ShaderSourceBinary shaderSource, Vk.VkShaderStageFlags stage)
        {
            _device = device;

            //Compile Shader
            ReadOnlyMemory<byte> sourceBytes = shaderSource.Source;
            using PinnedObjectReference<byte> pinnedSourceBytes = new PinnedObjectReference<byte>(sourceBytes);
            Vk.VkShaderModuleCreateInfo shaderModuleCreateInfo = new Vk.VkShaderModuleCreateInfo()
            {
                sType = Vk.VkStructureType.ShaderModuleCreateInfo,
                codeSize = new UIntPtr((uint)sourceBytes.Length),
                pCode = pinnedSourceBytes.pointer,
            };

            Vk.VkResult result = VK.vkCreateShaderModule(device.Device, ref shaderModuleCreateInfo, IntPtr.Zero, out _shaderModule);
            if (result != Vk.VkResult.Success)
            {
                if (_shaderModule != Vk.VkShaderModule.Null)
                {
                    VK.vkDestroyShaderModule(device.Device, _shaderModule, IntPtr.Zero);
                    _shaderModule = Vk.VkShaderModule.Null;
                }
                throw new Exception($"Failed to create VkShaderModule: {result}!"); //TODO: Debug info? Which shader? Why?
            }

            //Prepare PipelineShaderStageCreateInfo
            _mainString = new RawString("main");
            _pipelineShaderStageCreateInfo = new Vk.VkPipelineShaderStageCreateInfo()
             {
                 sType = Vk.VkStructureType.PipelineShaderStageCreateInfo,
                 stage = stage,
                 module = _shaderModule,
                 pName = _mainString,
             };
        }

        ~VulkanShaderProgram() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                if (_shaderModule.Handle != 0ul)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        VK.vkDestroyShaderModule(_device.Device, _shaderModule, IntPtr.Zero);
                        _shaderModule = Vk.VkShaderModule.Null;
                    }
                    else Debug.WriteLine("Warning: VulkanShaderProgram cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                if (_mainString != null)
                    _mainString.Dispose();

                // set large fields to null
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
