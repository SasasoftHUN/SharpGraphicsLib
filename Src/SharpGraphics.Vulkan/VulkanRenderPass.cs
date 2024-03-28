using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanRenderPass : RenderPass
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;

        private Vk.VkRenderPass _renderPass;

        #endregion

        #region Properties

        internal Vk.VkRenderPass Pass => _renderPass;

        #endregion

        #region Constructors

        internal VulkanRenderPass(VulkanGraphicsDevice device, in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps): base(attachments, steps)
        {
            _isDisposed = false;
            _device = device;

            //Create VK Attachment Descriptions and References
            Span<Vk.VkAttachmentDescription> attachmentDescriptions = stackalloc Vk.VkAttachmentDescription[attachments.Length];
            for (int i = 0; i < attachments.Length; i++)
            {
                attachmentDescriptions[i] = new Vk.VkAttachmentDescription()
                {
                    flags = 0u,
                    format = attachments[i].format.ToVkFormat(),
                    samples = attachments[i].samples.ToVkSampleCount(),
                    loadOp = attachments[i].loadOperation.ToVkLoadOperation(),
                    storeOp = attachments[i].storeOperation.ToVkStoreOperation(),
                    stencilLoadOp = attachments[i].stencilLoadOperation.ToVkLoadOperation(),
                    stencilStoreOp = attachments[i].stencilStoreOperation.ToVkStoreOperation(),
                    initialLayout = Vk.VkImageLayout.Undefined,
                    finalLayout = attachments[i].type.ToVkImageLayout(false), //TODO: Query Device
                };
            }

            //Create VK Subpasses - Prepare
            Span<Vk.VkSubpassDescription> subpasses = stackalloc Vk.VkSubpassDescription[steps.Length];

            int attachmentCount = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                attachmentCount += steps[i].ColorAttachmentIndices.Length;

                if (steps[i].HasDepthStencilAttachment)
                    ++attachmentCount;

                attachmentCount += steps[i].InputAttachmentIndices.Length;
            }
            Span<Vk.VkAttachmentReference> attachmentReferences = stackalloc Vk.VkAttachmentReference[attachmentCount]; //Forcing GC to not dispose arrays before vkCreateRenderPass

            //Create VK Subpasses - Begin Render Pass dependency
            using RawList<Vk.VkSubpassDependency> dependencies = new RawList<Vk.VkSubpassDependency>((uint)_steps.Length * 2u);
            dependencies.Add(
                new Vk.VkSubpassDependency()
                {
                    srcSubpass = VK.SubpassExternal,
                    dstSubpass = 0u,

                    srcStageMask = Vk.VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = Vk.VkPipelineStageFlags.ColorAttachmentOutput,

                    srcAccessMask = Vk.VkAccessFlags.MemoryRead,
                    dstAccessMask = Vk.VkAccessFlags.ColorAttachmentWrite | Vk.VkAccessFlags.ColorAttachmentRead,

                    dependencyFlags = Vk.VkDependencyFlags.ByRegion,
                });

            //Create VK Subpasses - Subpasses
            int attachmentIndex = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                subpasses[i] = new Vk.VkSubpassDescription()
                {
                    pipelineBindPoint = Vk.VkPipelineBindPoint.Graphics,
                };

                ReadOnlySpan<uint> colorAttachmentIndices = steps[i].ColorAttachmentIndices;
                if (colorAttachmentIndices.Length > 0) //Color Attachments
                {
                    for (int j = 0; j < colorAttachmentIndices.Length; j++)
                        attachmentReferences[attachmentIndex++] = new Vk.VkAttachmentReference(colorAttachmentIndices[j], Vk.VkImageLayout.ColorAttachmentOptimal);
                    subpasses[i].colorAttachmentCount = (uint)colorAttachmentIndices.Length;
                    subpasses[i].pColorAttachments = UnsafeExtension.AsIntPtr(attachmentReferences.Slice(attachmentIndex - colorAttachmentIndices.Length, colorAttachmentIndices.Length));
                    //TODO: Detect Preserved attachments
                    //TODO: Resolve Attachment? Multisample resolve at the end of subpass
                }
                //else No Color Attachments

                if (steps[i].HasDepthStencilAttachment) //One DepthStencil Attachment
                {
                    attachmentReferences[attachmentIndex++] = new Vk.VkAttachmentReference((uint)steps[i].DepthStencilAttachmentIndex, Vk.VkImageLayout.DepthStencilAttachmentOptimal);
                    subpasses[i].pDepthStencilAttachment = UnsafeExtension.AsIntPtr(attachmentReferences.Slice(attachmentIndex - 1, 1));
                }
                //else No DepthStencil Attachment

                ReadOnlySpan<uint> inputAttachmentIndices = steps[i].InputAttachmentIndices;
                if (inputAttachmentIndices.Length > 0)
                {
                    for (int j = 0; j < inputAttachmentIndices.Length; j++)
                        attachmentReferences[attachmentIndex++] = new Vk.VkAttachmentReference(inputAttachmentIndices[j], Vk.VkImageLayout.ShaderReadOnlyOptimal);
                    subpasses[i].inputAttachmentCount = (uint)inputAttachmentIndices.Length;
                    subpasses[i].pInputAttachments = UnsafeExtension.AsIntPtr(attachmentReferences.Slice(attachmentIndex - inputAttachmentIndices.Length, inputAttachmentIndices.Length));
                }

                //Dependencies
                for (int j = i + 1; j < steps.Length; j++)
                    if (_steps[j].IsUsing(steps[i]))
                        dependencies.Add(
                            new Vk.VkSubpassDependency()
                            {
                                srcSubpass = (uint)i,
                                dstSubpass = (uint)j,

                                srcStageMask = Vk.VkPipelineStageFlags.ColorAttachmentOutput,
                                dstStageMask = Vk.VkPipelineStageFlags.FragmentShader,

                                srcAccessMask = Vk.VkAccessFlags.ColorAttachmentWrite,
                                dstAccessMask = Vk.VkAccessFlags.ShaderRead,

                                dependencyFlags = Vk.VkDependencyFlags.ByRegion,
                            });
            }

            //Create VK Subpasses - End Render Pass dependency
            dependencies.Add(
                new Vk.VkSubpassDependency()
                {
                    srcSubpass = (uint)_steps.Length - 1u,
                    dstSubpass = VK.SubpassExternal,

                    srcStageMask = Vk.VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = Vk.VkPipelineStageFlags.BottomOfPipe,

                    srcAccessMask = Vk.VkAccessFlags.ColorAttachmentWrite | Vk.VkAccessFlags.ColorAttachmentRead,
                    dstAccessMask = Vk.VkAccessFlags.MemoryRead,

                    dependencyFlags = Vk.VkDependencyFlags.ByRegion,
                });

            //Create Render Pass
            Vk.VkRenderPassCreateInfo renderPassCreateInfo = new Vk.VkRenderPassCreateInfo()
            {
                sType = Vk.VkStructureType.RenderPassCreateInfo,
                attachmentCount = (uint)attachmentDescriptions.Length,
                pAttachments = UnsafeExtension.AsIntPtr(attachmentDescriptions),

                subpassCount = (uint)subpasses.Length,
                pSubpasses = UnsafeExtension.AsIntPtr(subpasses),

                dependencyCount = dependencies.Count,
                pDependencies = dependencies.Pointer,
            };

            if (VK.vkCreateRenderPass(_device.Device, ref renderPassCreateInfo, IntPtr.Zero, out _renderPass) != Vk.VkResult.Success)
                throw new Exception("Render Pass creation failed");
        }

        ~VulkanRenderPass() => Dispose(false);

        #endregion

        #region Protected Methods

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects).
                }*/

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing Vulkan Render Pass from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_renderPass != Vk.VkRenderPass.Null)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        VK.vkDestroyRenderPass(_device.Device, _renderPass, IntPtr.Zero);
                        _renderPass = Vk.VkRenderPass.Null;
                    }
                    else Debug.WriteLine("Warning: VulkanRenderPass cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in Vector2UInt resolution)
        {
            ReadOnlySpan<RenderPassAttachment> attachments = Attachments;
            VulkanTexture2D[] images = new VulkanTexture2D[attachments.Length];
            for (int i = 0; i < attachments.Length; i++)
                images[i] = new VulkanTexture2D(_device, attachments[i].format.ToVkFormat(), resolution.ToVkExtent2D(),
                    Vk.VkImageLayout.Undefined,
                    attachments[i].type.ToTextureType(),
                    Vk.VkMemoryPropertyFlags.DeviceLocal, Vk.VkPipelineStageFlags.BottomOfPipe, 1u); //TODO: Memory and Pipeline State?
            return new VulkanFrameBuffer<ITexture2D, VulkanTexture2D>(_device, images, this, true);
        }
        public override IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in ReadOnlySpan<ITexture2D> textures) => new VulkanFrameBuffer<ITexture2D, VulkanTexture2D>(_device, textures, this, false);

        #endregion

    }
}
