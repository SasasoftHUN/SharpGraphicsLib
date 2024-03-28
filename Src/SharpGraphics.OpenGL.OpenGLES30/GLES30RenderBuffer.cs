using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Graphics.ES30;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30RenderBuffer : GLTexture, IGLTexture2D
    {

        #region Fields

        private readonly RenderbufferInternalFormat _internalFormat;

        #endregion

        #region Properties

        #endregion

        #region Constructors

        //Create RenderBuffer
        internal GLES30RenderBuffer(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint layers, uint mipLevels) :
            base(device, new Vector3UInt(resolution.x, resolution.y, 1u), layers, mipLevels, dataFormat, type)
        {
            _internalFormat = dataFormat.ToRenderbufferInternalFormat();
        }

        //Not needed, base calls it!
        //~GLES30RenderBuffer() => Dispose(disposing: false);

        #endregion

        #region Public Methods

        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");

        public ITexture2D CreateView(DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
            => throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");


        public override void GLInitialize()
        {
            if (_referenceTexture == null)
            {
                GL.GenRenderbuffers(1, out _id);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _id);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, _internalFormat, (int)Width, (int)Height);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            }
            else throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");
        }
        public override void GLFree()
        {
            if (_id != 0 && _referenceTexture == null)
            {
                GL.DeleteRenderbuffers(1, ref _id);
                _id = 0;
            }
        }

        public override void GLBind(int binding, int textureUnit)
            => Debug.Fail("Sampling RenderBuffer Textures is not supported!");
        public override void GLUnBind(int textureUnit)
            => Debug.Fail("Sampling RenderBuffer Textures is not supported!");

        public override void GLGenerateMipMaps()
            => Debug.Fail("Generating MipMaps on RenderBuffer Textures is not supported!");

        public override void GLStoreData(IntPtr data, in CopyBufferTextureRange range)
            => Debug.Fail("Storing Data on RenderBuffer Textures is not supported!");
        public override void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
            => Debug.Fail("Storing Data on RenderBuffer Textures is not supported!");
        public override void GLReadData(IntPtr data, in CopyBufferTextureRange range)
            => Debug.Fail("Reading Data on RenderBuffer Textures is not supported!");
        public override void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
            => Debug.Fail("Reading Data on RenderBuffer Textures is not supported!");

        public override void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex)
        {
#if ANDROID
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, (FramebufferSlot)attachmentType.ToFramebufferAttachment(attachmentTypeIndex), RenderbufferTarget.Renderbuffer, _id);
#else
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), RenderbufferTarget.Renderbuffer, _id);
#endif
        }

        #endregion

    }
}
