using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanCommandBuffer : GraphicsCommandBuffer, IDisposable
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanCommandBufferFactory _commandBufferFactory;

        private Vk.VkCommandBuffer _commandBuffer;
        private Vk.VkFence _fence;

        private VulkanRenderPass? _activeRenderPass;
        private int _activeRenderPassStep = -1;
        private IVulkanFrameBuffer? _activeFrameBuffer;
        private VulkanPipeline? _activeVkPipeline;

        #endregion

        #region Properties

        internal VulkanCommandBufferFactory CommandBufferFactory => _commandBufferFactory;
        internal Vk.VkCommandBuffer CommandBuffer => _commandBuffer;
        internal Vk.VkFence Fence => _fence;

        #endregion

        #region Constructors

        internal VulkanCommandBuffer(VulkanCommandBufferFactory commandBufferFactory, in Vk.VkCommandBuffer commandBuffer) : base(commandBufferFactory.CommandProcessor)
        {
            _isDisposed = false;

            _commandBufferFactory = commandBufferFactory;
            _commandBuffer = commandBuffer;
            _fence = commandBufferFactory.CommandProcessor.Device.CreateFence(true);
        }

        ~VulkanCommandBuffer() => Dispose(false);

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRenderPass(VulkanRenderPass renderPass, IVulkanFrameBuffer framebuffer, Vk.VkSubpassContents subpassContents)
        {
            Vk.VkRenderPassBeginInfo renderPassBeginInfo = new Vk.VkRenderPassBeginInfo()
            {
                sType = Vk.VkStructureType.RenderPassBeginInfo,

                renderPass = renderPass.Pass,
                framebuffer = framebuffer.FrameBuffer,

                renderArea = framebuffer.VkSize.CreateRectFromSize(),

                clearValueCount = 0u,
                pClearValues = IntPtr.Zero,
            };
            VK.vkCmdBeginRenderPass(_commandBuffer, ref renderPassBeginInfo, subpassContents);

            _activeRenderPass = renderPass;
            _activeRenderPassStep = 0;
            _activeFrameBuffer = framebuffer;
            _activeVkPipeline = null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRenderPass(VulkanRenderPass renderPass, IVulkanFrameBuffer framebuffer, in Vk.VkClearColorValue colorValue, Vk.VkSubpassContents subpassContents)
        {
            Vk.VkClearValue clearColor = new Vk.VkClearValue()
            {
                color = colorValue,
            };
            Vk.VkRenderPassBeginInfo renderPassBeginInfo = new Vk.VkRenderPassBeginInfo()
            {
                sType = Vk.VkStructureType.RenderPassBeginInfo,

                renderPass = renderPass.Pass,
                framebuffer = framebuffer.FrameBuffer,

                renderArea = framebuffer.VkSize.CreateRectFromSize(),

                clearValueCount = 1u,
                pClearValues = UnsafeExtension.AsIntPtr(ref clearColor),
            };
            VK.vkCmdBeginRenderPass(_commandBuffer, ref renderPassBeginInfo, subpassContents);

            _activeRenderPass = renderPass;
            _activeRenderPassStep = 0;
            _activeFrameBuffer = framebuffer;
            _activeVkPipeline = null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRenderPass(VulkanRenderPass renderPass, IVulkanFrameBuffer framebuffer, in Span<Vk.VkClearValue> clearValues, Vk.VkSubpassContents subpassContents)
        {
            unsafe
            {
                fixed (Vk.VkClearValue* clearValuesPtr = clearValues)
                {
                    Vk.VkRenderPassBeginInfo renderPassBeginInfo = new Vk.VkRenderPassBeginInfo()
                    {
                        sType = Vk.VkStructureType.RenderPassBeginInfo,

                        renderPass = renderPass.Pass,
                        framebuffer = framebuffer.FrameBuffer,

                        renderArea = framebuffer.VkSize.CreateRectFromSize(),

                        clearValueCount = (uint)clearValues.Length,
                        pClearValues = new IntPtr(clearValuesPtr),
                    };
                    VK.vkCmdBeginRenderPass(_commandBuffer, ref renderPassBeginInfo, subpassContents);
                }
            }

            _activeRenderPass = renderPass;
            _activeRenderPassStep = 0;
            _activeFrameBuffer = framebuffer;
            _activeVkPipeline = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndRenderPassClear()
        {
            _activeRenderPass = null;
            _activeRenderPassStep = -1;
            _activeFrameBuffer = null;
            _activeVkPipeline = null;
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
                    Debug.WriteLine($"Disposing Vulkan Command Buffer from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_commandBuffer != Vk.VkCommandBuffer.Null || _fence != Vk.VkFence.Null)
                {
                    if (_commandBufferFactory != null && !_commandBufferFactory.IsDisposed)
                    {
                        if (_commandBuffer != Vk.VkCommandBuffer.Null)
                        {
                            _commandBufferFactory.DeleteVKCommandBuffer(_commandBuffer);
                            _commandBuffer = Vk.VkCommandBuffer.Null;
                        }

                        if (_fence != Vk.VkFence.Null)
                        {
                            if (_commandBufferFactory.CommandProcessor != null && !_commandBufferFactory.CommandProcessor.IsDisposed &&
                                _commandBufferFactory.CommandProcessor.Device != null && !_commandBufferFactory.CommandProcessor.Device.IsDisposed)
                            {
                                VK.vkDestroyFence(_commandBufferFactory.CommandProcessor.Device.Device, _fence, IntPtr.Zero);
                                _fence = Vk.VkFence.Null;
                            }
                            else Debug.WriteLine("Warning: VulkanCommandBuffer's Fence cannot be disposed properly because parent CommandProcessor (VkQueue) or GraphicsDevice is already Disposed!");
                        }
                    }
                    else Debug.WriteLine("Warning: VulkanCommandBuffer cannot be disposed properly because parent CommandBufferFactory (VkCommandPool) is already Disposed!");
                }


                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyBuffer(in Vk.VkBuffer source, in Vk.VkBuffer destination, ref Vk.VkBufferCopy bufferCopy)
            => VK.vkCmdCopyBuffer(_commandBuffer, source, destination, 1u, ref bufferCopy);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyBufferToImage(in Vk.VkBuffer source, in Vk.VkImage destination, Vk.VkImageLayout imageLayout, ref Vk.VkBufferImageCopy bufferImageCopy)
            => VK.vkCmdCopyBufferToImage(_commandBuffer, source, destination, imageLayout, 1u, ref bufferImageCopy);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyBufferToImage(in Vk.VkBuffer source, in Vk.VkImage destination, Vk.VkImageLayout imageLayout, in ReadOnlySpan<Vk.VkBufferImageCopy> bufferImageCopies)
        {
            unsafe
            {
                fixed (Vk.VkBufferImageCopy* bufferImageCopiesPtr = bufferImageCopies)
                    VK.vkCmdCopyBufferToImage(_commandBuffer, source, destination, imageLayout, (uint)bufferImageCopies.Length, new IntPtr(bufferImageCopiesPtr));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyImageToBuffer(in Vk.VkImage source, Vk.VkImageLayout imageLayout, in Vk.VkBuffer destination, ref Vk.VkBufferImageCopy bufferImageCopy)
            => VK.vkCmdCopyImageToBuffer(_commandBuffer, source, imageLayout, destination, 1u, ref bufferImageCopy);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyImageToBuffer(in Vk.VkImage source, Vk.VkImageLayout imageLayout, in Vk.VkBuffer destination, in ReadOnlySpan<Vk.VkBufferImageCopy> bufferImageCopies)
        {
            unsafe
            {
                fixed (Vk.VkBufferImageCopy* bufferImageCopiesPtr = bufferImageCopies)
                    VK.vkCmdCopyImageToBuffer(_commandBuffer, source, imageLayout, destination, (uint)bufferImageCopies.Length, new IntPtr(bufferImageCopiesPtr));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BlitImage(in Vk.VkImage image, ref Vk.VkImageBlit blitRegion, Vk.VkFilter filter)
            => VK.vkCmdBlitImage(_commandBuffer, image, Vk.VkImageLayout.TransferSrcOptimal, image, Vk.VkImageLayout.TransferDstOptimal, 1u, ref blitRegion, filter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BlitImage(in Vk.VkImage srcImage, in Vk.VkImage dstImage, ref Vk.VkImageBlit blitRegion, Vk.VkFilter filter)
            => VK.vkCmdBlitImage(_commandBuffer, srcImage, Vk.VkImageLayout.TransferSrcOptimal, dstImage, Vk.VkImageLayout.TransferDstOptimal, 1u, ref blitRegion, filter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetViewport(ref Vk.VkViewport viewport)
            => VK.vkCmdSetViewport(_commandBuffer, 0u, 1u, ref viewport);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetScissor(ref Vk.VkRect2D scissor)
            => VK.vkCmdSetScissor(_commandBuffer, 0u, 1u, ref scissor);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrier(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkBufferMemoryBarrier bufferMemoryBarrier)
        {
            //bufferMemoryBarrier.srcQueueFamilyIndex = _commandProcessor.FamilyIndex;
            //bufferMemoryBarrier.dstQueueFamilyIndex = _commandProcessor.FamilyIndex;
            bufferMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
            bufferMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 1u, ref bufferMemoryBarrier, 0u, IntPtr.Zero);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrier(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkImageMemoryBarrier imageMemoryBarrier)
        {
            //imageMemoryBarrier.srcQueueFamilyIndex = _commandProcessor.FamilyIndex;
            //imageMemoryBarrier.dstQueueFamilyIndex = _commandProcessor.FamilyIndex;
            imageMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
            imageMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 0u, IntPtr.Zero, 1u, ref imageMemoryBarrier);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrierTransferFrom(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkBufferMemoryBarrier bufferMemoryBarrier, VulkanCommandProcessor from)
        {
            if (_commandBufferFactory.CommandProcessor == from)
            {
                bufferMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
                bufferMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            }
            else
            {
                bufferMemoryBarrier.srcQueueFamilyIndex = from.FamilyIndex;
                bufferMemoryBarrier.dstQueueFamilyIndex = _commandBufferFactory.CommandProcessor.FamilyIndex;
            }
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 1u, ref bufferMemoryBarrier, 0u, IntPtr.Zero);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrierTransferFrom(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkImageMemoryBarrier imageMemoryBarrier, VulkanCommandProcessor from)
        {
            if (_commandBufferFactory.CommandProcessor == from)
            {
                imageMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
                imageMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            }
            else
            {
                imageMemoryBarrier.srcQueueFamilyIndex = from.FamilyIndex;
                imageMemoryBarrier.dstQueueFamilyIndex = _commandBufferFactory.CommandProcessor.FamilyIndex;
            }
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 0u, IntPtr.Zero, 1u, ref imageMemoryBarrier);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrierTransferTo(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkBufferMemoryBarrier bufferMemoryBarrier, in VulkanCommandProcessor to)
        {
            if (_commandBufferFactory.CommandProcessor == to)
            {
                bufferMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
                bufferMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            }
            else
            {
                bufferMemoryBarrier.srcQueueFamilyIndex = _commandBufferFactory.CommandProcessor.FamilyIndex;
                bufferMemoryBarrier.dstQueueFamilyIndex = to.FamilyIndex;
            }
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 1u, ref bufferMemoryBarrier, 0u, IntPtr.Zero);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PipelineBarrierTransferTo(Vk.VkPipelineStageFlags sourceState, Vk.VkPipelineStageFlags destinationState, ref Vk.VkImageMemoryBarrier imageMemoryBarrier, in VulkanCommandProcessor to)
        {
            if (_commandBufferFactory.CommandProcessor == to)
            {
                imageMemoryBarrier.srcQueueFamilyIndex = VK.QueueFamilyIgnored;
                imageMemoryBarrier.dstQueueFamilyIndex = VK.QueueFamilyIgnored;
            }
            else
            {
                imageMemoryBarrier.srcQueueFamilyIndex = _commandBufferFactory.CommandProcessor.FamilyIndex;
                imageMemoryBarrier.dstQueueFamilyIndex = to.FamilyIndex;
            }
            VK.vkCmdPipelineBarrier(_commandBuffer, sourceState, destinationState, 0u, 0u, IntPtr.Zero, 0u, IntPtr.Zero, 1u, ref imageMemoryBarrier);
        }


        internal void SetToDisposed()
        {
            _isDisposed = true;
            _commandBuffer = Vk.VkCommandBuffer.Null;
            _fence = Vk.VkFence.Null;
        }

        #endregion

        #region Public Methods

        public override void Reset(ResetOptions options = ResetOptions.Nothing) => VK.vkResetCommandBuffer(_commandBuffer, options.ToVkCommandBufferResetFlags());
        public override void Begin(BeginOptions options = BeginOptions.OneTimeSubmit)
        {
            Vk.VkCommandBufferBeginInfo commandBufferBeginInfo = new Vk.VkCommandBufferBeginInfo()
            {
                sType = Vk.VkStructureType.CommandBufferBeginInfo,
                flags = options.ToVkCommandBufferUsageFlags(),
            };

            Vk.VkResult beginResult = VK.vkBeginCommandBuffer(_commandBuffer, ref commandBufferBeginInfo);
            if (beginResult != Vk.VkResult.Success)
                throw new Exception($"Error beginning VkCommandBuffer: {beginResult}!");
        }
        public override void BeginAndContinue(GraphicsCommandBuffer commandBuffer, BeginOptions options = BeginOptions.OneTimeSubmit)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            if (vkCommandBuffer._activeRenderPass == null || vkCommandBuffer._activeRenderPassStep < 0 || vkCommandBuffer._activeFrameBuffer == null)
                throw new ArgumentException("Primary CommandBuffer has no active RenderPass when starting the SecondaryCommandBuffer", "commandBuffer");

            _activeRenderPass = vkCommandBuffer._activeRenderPass;
            _activeRenderPassStep = vkCommandBuffer._activeRenderPassStep;
            _activeFrameBuffer = vkCommandBuffer._activeFrameBuffer;
            _activeVkPipeline = null;

            Vk.VkCommandBufferInheritanceInfo inheritanceInfo = new Vk.VkCommandBufferInheritanceInfo()
            {
                sType = Vk.VkStructureType.CommandBufferInheritanceInfo,
                renderPass = vkCommandBuffer._activeRenderPass.Pass,
                subpass = (uint)vkCommandBuffer._activeRenderPassStep,
                framebuffer = vkCommandBuffer._activeFrameBuffer.FrameBuffer,

                //If occlusionQueryEnable is VK_TRUE, then this command buffer can be executed whether the primary command buffer has an occlusion query active or not.
                //If occlusionQueryEnable is VK_FALSE, then the primary command buffer must not have an occlusion query active.
                occlusionQueryEnable = Vk.VkBool32.False,
                //If queryFlags value includes the VK_QUERY_CONTROL_PRECISE_BIT bit, then the active occlusion query can return boolean results or actual sample counts.
                //If queryFlags bit is not set, then the active occlusion query must not use the VK_QUERY_CONTROL_PRECISE_BIT bit.
                queryFlags = 0u,
                pipelineStatistics = 0u,
            };

            Vk.VkCommandBufferBeginInfo commandBufferBeginInfo = new Vk.VkCommandBufferBeginInfo()
            {
                sType = Vk.VkStructureType.CommandBufferBeginInfo,
                flags = options.ToVkCommandBufferUsageFlags() |
                    Vk.VkCommandBufferUsageFlags.RenderPassContinue,
                pInheritanceInfo = UnsafeExtension.AsIntPtr(ref inheritanceInfo),
            };

            Vk.VkResult beginResult = VK.vkBeginCommandBuffer(_commandBuffer, ref commandBufferBeginInfo);
            if (beginResult != Vk.VkResult.Success)
                throw new Exception($"Error beginning VkCommandBuffer: {beginResult}!");
        }


        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer framebuffer, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            VulkanRenderPass vkRenderPass = Unsafe.As<VulkanRenderPass>(renderPass);
            IVulkanFrameBuffer vkFramebuffer = Unsafe.As<IVulkanFrameBuffer>(framebuffer);
            BeginRenderPass(vkRenderPass, vkFramebuffer, executionLevel.ToVkSubpassContents());
        }
        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer framebuffer, Vector4 clearColor, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            VulkanRenderPass vkRenderPass = Unsafe.As<VulkanRenderPass>(renderPass);
            IVulkanFrameBuffer vkFramebuffer = Unsafe.As<IVulkanFrameBuffer>(framebuffer);
#if DEBUG
            if (renderPass.Attachments.Length != 1)
                throw new ArgumentException($"The RenderPass has {renderPass.Attachments.Length} attachments instead of 1.", "clearColor");
#endif
            BeginRenderPass(vkRenderPass, vkFramebuffer, new Vk.VkClearColorValue(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W), executionLevel.ToVkSubpassContents());
        }
        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer framebuffer, in ReadOnlySpan<Vector4> clearValues, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            ReadOnlySpan<RenderPassAttachment> attachments = renderPass.Attachments;
            VulkanRenderPass vkRenderPass = Unsafe.As<VulkanRenderPass>(renderPass);
            IVulkanFrameBuffer vkFramebuffer = Unsafe.As<IVulkanFrameBuffer>(framebuffer);
#if DEBUG
            if (attachments.Length != clearValues.Length)
                throw new ArgumentException("Count of ClearValues is different than the attachments in the RenderPass", "clearValues");
#endif

            Span<Vk.VkClearValue> vkClearValues = stackalloc Vk.VkClearValue[clearValues.Length];
            for (int i = 0; i < vkClearValues.Length; i++)
            {
                if (attachments[i].type.HasFlag(AttachmentType.Depth))
                    vkClearValues[i].depthStencil = attachments[i].type.HasFlag(AttachmentType.Stencil) ?
                        new Vk.VkClearDepthStencilValue(clearValues[i].X, (uint)clearValues[i].Y) : //Depth-Stencil
                        new Vk.VkClearDepthStencilValue(clearValues[i].X, 0u); //Just Depth
                else if (attachments[i].type.HasFlag(AttachmentType.Stencil))
                    vkClearValues[i].depthStencil = new Vk.VkClearDepthStencilValue(0f, (uint)clearValues[i].Y); //Just Stencil
                else vkClearValues[i].color = new Vk.VkClearColorValue(clearValues[i].X, clearValues[i].Y, clearValues[i].Z, clearValues[i].W); //Color or others
            }

            BeginRenderPass(vkRenderPass, vkFramebuffer, vkClearValues, executionLevel.ToVkSubpassContents());
        }
        public override void NextRenderPassStep(CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            VK.vkCmdNextSubpass(_commandBuffer, executionLevel.ToVkSubpassContents());
            ++_activeRenderPassStep;
        }
        public override void NextRenderPassStep(PipelineResource inputAttachmentResource, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            if (_activeRenderPass != null && _activeFrameBuffer != null)
            {
                NextRenderPassStep(executionLevel);
                inputAttachmentResource.BindInputAttachments(_activeRenderPass.Steps[_activeRenderPassStep], _activeFrameBuffer);
            }
            else throw new Exception("VulkanCommandBuffer is not in a RenderPass, cannot use NextRenderPassStep");
        }
        public override void EndRenderPass()
        {
            VK.vkCmdEndRenderPass(_commandBuffer);
            EndRenderPassClear();
        }

        public override void BindPipeline(IGraphicsPipeline pipeline)
        {
            if (_activeFrameBuffer != null)
            {
                _activeVkPipeline = Unsafe.As<VulkanPipeline>(pipeline);
                VK.vkCmdBindPipeline(_commandBuffer, Vk.VkPipelineBindPoint.Graphics, _activeVkPipeline.Pipeline);
                SetViewportAndScissor(_activeFrameBuffer.VkSize);
            }
            else throw new Exception("VulkanCommandBuffer is not in a RenderPass, cannot use BindPipeline");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewportAndScissor(in Vk.VkExtent2D size)
        {
            SetViewport(size);
            SetScissor(size);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewport(in Vk.VkExtent2D size)
        {
            Vk.VkViewport viewport = size.CreateViewportFromSize();
            SetViewport(ref viewport);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScissor(in Vk.VkExtent2D size)
        {
            Vk.VkRect2D scissor = size.CreateRectFromSize();
            SetScissor(ref scissor);
        }



        public override void SetViewport(in Vector2UInt size)
        {
            Vk.VkViewport viewport = size.ToVkExtent2D().CreateViewportFromSize();
            SetViewport(ref viewport);
        }
        public override void SetScissor(in Vector2UInt size)
        {
            Vk.VkRect2D scissor = size.ToVkExtent2D().CreateRectFromSize();
            SetScissor(ref scissor);
        }

        public override void BindVertexBuffer(uint binding, IDataBuffer vertexBuffer, ulong offset = 0ul)
        {
            Vk.VkBuffer vkBuffer = Unsafe.As<IVulkanDataBuffer>(vertexBuffer).Buffer;
            VK.vkCmdBindVertexBuffers(_commandBuffer, binding, 1u, ref vkBuffer, ref offset);
        }
        public override void BindVertexBuffers(uint firstBinding, in ReadOnlySpan<IDataBuffer> vertexBuffers)
        {
            Span<Vk.VkBuffer> vkBuffers = stackalloc Vk.VkBuffer[vertexBuffers.Length];
            Span<ulong> offsets = stackalloc ulong[vertexBuffers.Length];

            for (int i = 0; i < vertexBuffers.Length; i++)
            {
                vkBuffers[i] = Unsafe.As<IVulkanDataBuffer>(vertexBuffers[i]).Buffer;
                offsets[i] = 0ul;
            }

            VK.vkCmdBindVertexBuffers(_commandBuffer, firstBinding, (uint)vkBuffers.Length, ref vkBuffers[0], ref offsets[0]);
        }
        public override void BindVertexBuffers(in ReadOnlySpan<VertexBufferBinding> bindings)
        {
            Span<Vk.VkBuffer> vkBuffers = stackalloc Vk.VkBuffer[bindings.Length];
            Span<ulong> offsets = stackalloc ulong[bindings.Length];

            int spanI = 0;
            uint firstBinding = bindings[0].binding;
            for (int i = 0; i < bindings.Length; i++)
            {
                vkBuffers[spanI] = Unsafe.As<IVulkanDataBuffer>(bindings[i].vertexBuffer).Buffer;
                offsets[spanI++] = bindings[i].offset;

                if (i == bindings.Length - 1 || bindings[i].binding != bindings[i + 1].binding - 1u)
                {
                    VK.vkCmdBindVertexBuffers(_commandBuffer, firstBinding, (uint)spanI, ref vkBuffers[0], ref offsets[0]);
                    spanI = 0;
                    if (i < bindings.Length - 1)
                        firstBinding = bindings[i + 1].binding;
                }
            }
        }
        public override void BindIndexBuffer(IDataBuffer indexBuffer, IndexType type, ulong offset = 0ul)
            => VK.vkCmdBindIndexBuffer(_commandBuffer, Unsafe.As<IVulkanDataBuffer>(indexBuffer).Buffer, offset, type.ToVkIndexType());

        public override void BindResource(uint set, PipelineResource resource)
        {
            if (_activeVkPipeline != null)
                BindResource(_activeVkPipeline, set, resource);
            else throw new Exception("VulkanCommandBuffer is not using a Pipeline, cannot use BindResource");
        }
        public override void BindResource(uint set, PipelineResource resource, int dataIndex)
        {
            if (_activeVkPipeline != null)
                BindResource(_activeVkPipeline, set, resource, dataIndex);
            else throw new Exception("VulkanCommandBuffer is not using a Pipeline, cannot use BindResource");
        }

        public override void Draw(uint vertexCount) => VK.vkCmdDraw(_commandBuffer, vertexCount, 1u, 0u, 0u);
        public override void Draw(uint vertexCount, uint firstVertex) => VK.vkCmdDraw(_commandBuffer, vertexCount, 1u, firstVertex, 0u);
        //TODO: Uncomment when Instancing support is implemented
        //public override void DrawInstanced(uint vertexCount, uint instanceCount) => VK.vkCmdDraw(_commandBuffer, vertexCount, instanceCount, 0u, 0u);
        //public override void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount) => VK.vkCmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, 0u);
        //public override void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount, uint firstInstance) => VK.vkCmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);

        public override void DrawIndexed(uint indexCount) => VK.vkCmdDrawIndexed(_commandBuffer, indexCount, 1u, 0u, 0, 0u);
        public override void DrawIndexed(uint indexCount, uint firstIndex) => VK.vkCmdDrawIndexed(_commandBuffer, indexCount, 1u, firstIndex, 0, 0u);
        //TODO: Uncomment when Instancing support is implemented
        //public override void DrawIndexedInstanced(uint indexCount, uint instanceCount) => VK.vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, 0u, 0, 0u);
        //public override void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount) => VK.vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, 0, 0u);
        //public override void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount, uint firstInstance) => VK.vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, 0, firstInstance);


        /*public override void ExecuteSecondaryCommandBuffer(GraphicsCommandBuffer secondaryBuffer)
        {
            Vk.VkCommandBuffer buffer = secondaryBuffer.Buffer;
            VK.vkCmdExecuteCommands(_commandBuffer, 1u, ref buffer);
        }*/
        public override void ExecuteSecondaryCommandBuffers(in ReadOnlySpan<GraphicsCommandBuffer> secondaryBuffers)
        {
            Span<Vk.VkCommandBuffer> buffers = stackalloc Vk.VkCommandBuffer[secondaryBuffers.Length];
            for (int i = 0; i < secondaryBuffers.Length; i++)
                buffers[i] = Unsafe.As<VulkanCommandBuffer>(secondaryBuffers[i]).CommandBuffer;

            VK.vkCmdExecuteCommands(_commandBuffer, (uint)buffers.Length, ref buffers[0]);
        }

        public override void End()
        {
            VK.vkEndCommandBuffer(_commandBuffer);
            EndRenderPassClear();
        }

        //public void Submit() => _commandProcessor.Submit(_commandBuffer);
        public void Submit(in Vk.VkSemaphore waitSemaphore, Vk.VkPipelineStageFlags waitStage, in Vk.VkSemaphore signalSemaphore)
            => _commandBufferFactory.CommandProcessor.Submit(_commandBuffer, waitSemaphore, waitStage, signalSemaphore, _fence);

        #endregion
    }
}
