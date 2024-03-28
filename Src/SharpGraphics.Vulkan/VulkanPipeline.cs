using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal abstract class VulkanPipeline : IPipeline
    {

        #region Fields

        private bool _isDisposed;

        protected readonly VulkanGraphicsDevice _device;

        protected Vk.VkPipeline _pipeline;
        protected Vk.VkPipelineBindPoint _pipelineBindPoint;
        protected Vk.VkPipelineLayout _layout;

        #endregion

        #region Properties

        internal Vk.VkPipeline Pipeline => _pipeline;
        internal Vk.VkPipelineBindPoint BindPoint => _pipelineBindPoint;
        internal Vk.VkPipelineLayout Layout => _layout;

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        protected VulkanPipeline(VulkanGraphicsDevice device)
        {
            _device = device;
        }

        ~VulkanPipeline() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected static void ConstructLayout(Vk.VkDevice device, in GraphicsPipelineConstuctionParameters constuction, out Vk.VkPipelineLayout layout)
        {
            //Layout (active textures, uniforms etc.)
            ReadOnlySpan<PipelineResourceLayout> resourceLayouts = constuction.ResourceLayouts;
            Span<Vk.VkDescriptorSetLayout> layouts = stackalloc Vk.VkDescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++)
                layouts[i] = Unsafe.As<VulkanPipelineResourceLayout>(resourceLayouts[i]).Layout;

            Vk.VkPipelineLayoutCreateInfo layoutCreateInfo = new Vk.VkPipelineLayoutCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineLayoutCreateInfo,

                setLayoutCount = (uint)layouts.Length,
                pSetLayouts = layouts.Length > 0 ? UnsafeExtension.AsIntPtr(layouts) : IntPtr.Zero,
            };
            if (VK.vkCreatePipelineLayout(device, ref layoutCreateInfo, IntPtr.Zero, out layout) != Vk.VkResult.Success)
                throw new Exception("Pipeline Layout Creation error!");
        }


        protected void CreatePipeline(Vk.VkGraphicsPipelineCreateInfo graphicsPipelineCreateInfo)
        {
            _pipelineBindPoint = Vk.VkPipelineBindPoint.Graphics;

            if (VK.vkCreateGraphicsPipelines(_device.Device, Vk.VkPipelineCache.Null, 1u, ref graphicsPipelineCreateInfo, IntPtr.Zero, out _pipeline) != Vk.VkResult.Success)
                throw new Exception("Pipeline Create error!");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer

                if (_pipeline != Vk.VkPipeline.Null || _layout != Vk.VkPipelineLayout.Null)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        if (_pipeline != Vk.VkPipeline.Null)
                        {
                            VK.vkDestroyPipeline(_device.Device, _pipeline, IntPtr.Zero);
                            _pipeline = Vk.VkPipeline.Null;
                        }
                        if (_layout != Vk.VkPipelineLayout.Null)
                        {
                            VK.vkDestroyPipelineLayout(_device.Device, _layout, IntPtr.Zero);
                            _layout = Vk.VkPipelineLayout.Null;
                        }
                    }
                    else Debug.WriteLine("Warning: VulkanPipeline cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                // Set large fields to null
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
