using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.OpenGL
{

    public enum DefaultFrameBufferOption
    {
        NotUsed,
        LastIsDefaultFBO,
        NeedToBlitDefault,
    }

    public interface IGLFrameBuffer : IFrameBuffer, IGLResource
    {

        [Flags]
        public enum BlitMask : int
        {
            None = 0,
            Color = 1, Depth = 2, Stencil = 4,
        }

        bool MustBlitFromDefaultOnStart { get; }
        bool MustBlitToDefaultOnEnd { get; }

        void GLBind(int index);
        void GLUnBind();

        void GLBlitFromDefault();
        void GLBlitToDefault();

    }
    public interface IGLFrameBuffer<T, GLT> : IGLFrameBuffer, IFrameBuffer<T> where T : ITexture where GLT : IGLTexture, T
    {

    }

    public abstract class GLFrameBuffer<T, GLT> : FrameBuffer<T>, IGLFrameBuffer<T, GLT> where T : ITexture where GLT : IGLTexture, T
    {

        #region Fields

        private bool _isDisposed;

        protected readonly GLGraphicsDevice _device;

        protected readonly IRenderPass _renderPass;
        protected readonly DefaultFrameBufferOption _defaultFBOOption;
        protected readonly IGLFrameBuffer.BlitMask _readBlitMask;
        protected readonly IGLFrameBuffer.BlitMask _writeBlitMask;
        protected readonly int[] _ids;

        #endregion

        #region Properties

        public bool MustBlitFromDefaultOnStart { get; }
        public bool MustBlitToDefaultOnEnd { get; }

        #endregion

        #region Constructors

        protected GLFrameBuffer(GLGraphicsDevice device, IRenderPass renderPass, DefaultFrameBufferOption defaultFBOOption, in ReadOnlySpan<T> images, Vector2UInt resolution, uint layers, bool ownImages) : base(images, resolution, layers, ownImages)
        {
            Debug.Assert(renderPass.Attachments.Length == images.Length, $"RenderPass has {renderPass.Attachments.Length} Attachments, but {images.Length} textures/images are provided for FrameBuffer creation.");

            _device = device;
            _renderPass = renderPass;
            _defaultFBOOption = defaultFBOOption;
            _ids = new int[_renderPass.Steps.Length];

            if (defaultFBOOption == DefaultFrameBufferOption.NeedToBlitDefault)
            {
                ReadOnlySpan<RenderPassAttachment> attachments = renderPass.Attachments;
                RenderPassStep lastStep = renderPass.Steps[renderPass.Steps.Length - 1];
                IGLFrameBuffer.BlitMask readBlitMask = IGLFrameBuffer.BlitMask.None;
                IGLFrameBuffer.BlitMask writeBlitMask = IGLFrameBuffer.BlitMask.None;

                //Will blit only the first Color Attachment
                ReadOnlySpan<uint> colorAttachmentIndices = lastStep.ColorAttachmentIndices;
                if (colorAttachmentIndices.Length > 0)
                {
                    if (attachments[(int)colorAttachmentIndices[0]].loadOperation == AttachmentLoadOperation.Load)
                        readBlitMask |= IGLFrameBuffer.BlitMask.Color;
                    if (attachments[(int)colorAttachmentIndices[0]].storeOperation == AttachmentStoreOperation.Store)
                        writeBlitMask |= IGLFrameBuffer.BlitMask.Color;
                }

                if (lastStep.HasDepthStencilAttachment)
                {
                    if (attachments[lastStep.DepthStencilAttachmentIndex].loadOperation == AttachmentLoadOperation.Load)
                        readBlitMask |= IGLFrameBuffer.BlitMask.Depth;
                    if (attachments[lastStep.DepthStencilAttachmentIndex].storeOperation == AttachmentStoreOperation.Store)
                        writeBlitMask |= IGLFrameBuffer.BlitMask.Depth;

                    if (attachments[lastStep.DepthStencilAttachmentIndex].stencilLoadOperation == AttachmentLoadOperation.Load)
                        readBlitMask |= IGLFrameBuffer.BlitMask.Stencil;
                    if (attachments[lastStep.DepthStencilAttachmentIndex].stencilStoreOperation == AttachmentStoreOperation.Store)
                        writeBlitMask |= IGLFrameBuffer.BlitMask.Stencil;
                }

                if (readBlitMask != IGLFrameBuffer.BlitMask.None)
                {
                    _readBlitMask = readBlitMask;
                    MustBlitFromDefaultOnStart = true;
                }
                else MustBlitFromDefaultOnStart = false;

                if (writeBlitMask != IGLFrameBuffer.BlitMask.None)
                {
                    _writeBlitMask = writeBlitMask;
                    MustBlitToDefaultOnEnd = true;
                }
                else MustBlitToDefaultOnEnd = false;
            }
            else
            {
                MustBlitFromDefaultOnStart = false;
                MustBlitToDefaultOnEnd = false;
            }
        }

        ~GLFrameBuffer() => Dispose(false);

        #endregion

        #region Protected Methods

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                // Dispose managed state (managed objects).

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL FrameBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine($"Warning: OpenGL Framebuffer<{typeof(T).FullName}> cannot be disposed properly because parent Device is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public abstract void GLInitialize();
        public abstract void GLFree();

        public abstract void GLBind(int index);
        public abstract void GLUnBind();

        public abstract void GLBlitFromDefault();
        public abstract void GLBlitToDefault();

        #endregion

    }
}
