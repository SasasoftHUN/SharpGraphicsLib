using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using static Vulkan.Ext;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{

    internal interface IVulkanFrameBuffer : IFrameBuffer
    {

        Vk.VkFramebuffer FrameBuffer { get; }
        Vk.VkExtent2D VkSize { get; }

        Vk.VkImageView GetAttachmentView(uint index);

    }
    internal interface IVulkanFrameBuffer<T, VT> : IVulkanFrameBuffer, IFrameBuffer<T> where T : ITexture where VT : VulkanTexture, T
    {

    }

    internal sealed class VulkanFrameBuffer<T, VT> : FrameBuffer<T>, IVulkanFrameBuffer<T, VT> where T : ITexture where VT : VulkanTexture, T
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;
        private Vk.VkFramebuffer _framebuffer;

        #endregion

        #region Properties

        Vk.VkFramebuffer IVulkanFrameBuffer.FrameBuffer => _framebuffer;
        Vk.VkExtent2D IVulkanFrameBuffer.VkSize => _resolution.ToVkExtent2D();

        #endregion

        #region Constructors

        internal VulkanFrameBuffer(VulkanGraphicsDevice device, in ReadOnlySpan<T> images, IRenderPass renderPass, bool ownImages) : base(images, images[0].Resolution, images[0].Layers, ownImages)
        {
            Debug.Assert(renderPass.Attachments.Length == images.Length, $"RenderPass has {renderPass.Attachments.Length} Attachments, but {images.Length} textures/images are provided for FrameBuffer creation.");

            _isDisposed = false;
            _device = device;

            Span<Vk.VkImageView> imageViews = stackalloc Vk.VkImageView[images.Length];
            for (int i = 0; i < imageViews.Length; i++)
                imageViews[i] = Unsafe.As<VT>(images[i]).VkImageView;

            VulkanRenderPass vkRenderPass = Unsafe.As<VulkanRenderPass>(renderPass);

            //Framebuffer
            Vk.VkFramebufferCreateInfo framebufferCreateInfo = new Vk.VkFramebufferCreateInfo()
            {
                sType = Vk.VkStructureType.FramebufferCreateInfo,
                renderPass = vkRenderPass.Pass,
                attachmentCount = (uint)imageViews.Length,
                pAttachments = UnsafeExtension.AsIntPtr(imageViews),

                width = images[0].Width,
                height = images[0].Height,
                layers = images[0].Layers,
            };

            if (VK.vkCreateFramebuffer(device.Device, ref framebufferCreateInfo, IntPtr.Zero, out _framebuffer) != Vk.VkResult.Success)
                throw new Exception("Framebuffer creation error!");
        }
        //TODO: Constructor with multi-layer attachments
        //TODO: Constructor with multi-view attachments

        ~VulkanFrameBuffer() => Dispose(false);

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
                    Debug.WriteLine($"Disposing Vulkan FrameBuffer<{typeof(T).FullName}, {typeof(VT).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_framebuffer != Vk.VkFramebuffer.Null)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        VK.vkDestroyFramebuffer(_device.Device, _framebuffer, IntPtr.Zero);
                        _framebuffer = Vk.VkFramebuffer.Null;
                    }
                    else Debug.WriteLine($"Warning: VulkanFrameBuffer<{typeof(T).FullName}, {typeof(VT).FullName}> cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        Vk.VkImageView IVulkanFrameBuffer.GetAttachmentView(uint index) => Unsafe.As<VT>(_images[index]).VkImageView;

        #endregion

    }
}
