using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Utils;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreRenderBuffer : GLTexture, IGLTexture2D
    {

        #region Fields

        private readonly RenderbufferStorage _internalFormat;

        #endregion

        #region Properties

        #endregion

        #region Constructors

        //Create RenderBuffer
        internal GLCoreRenderBuffer(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint layers, uint mipLevels) :
            base(device, new Vector3UInt(resolution.x, resolution.y, 1u), layers, mipLevels, dataFormat, type)
        {
            _internalFormat = dataFormat.ToRenderbufferStorage();
        }
        //Create just an emulated View
        internal GLCoreRenderBuffer(GLGraphicsDevice device, GLCoreRenderBuffer referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, referenceTexture, swizzle, mipmapRange, layerRange, dataFormat)
        {
            _id = referenceTexture._id;
            _internalFormat = dataFormat.ToRenderbufferStorage();
        }

        //Not needed, base calls it!
        //~GLCoreRenderBuffer() => Dispose(disposing: false);

        #endregion

        #region Public Methods

        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => Debug.Fail("Missing Store TextureType for StoreData!");

        public ITexture2D CreateView(DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
        {
            if (_device.Features.IsTextureViewSupported)
            {
                GLCoreRenderBuffer referenceTexture = _referenceTexture != null ? Unsafe.As<GLCoreRenderBuffer>(_referenceTexture) : this;
                return new GLCoreRenderBuffer(_device, referenceTexture, dataFormat ?? DataFormat, _swizzle.Combine(swizzle), _mipmapRange.Combine(levels), _layerRange);
            }
            else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
        }


        public override void GLInitialize()
        {
            if (_referenceTexture == null)
            {
                if (_device.GLFeatures.IsDirectStateAccessSupported)
                {
                    GL.CreateRenderbuffers(1, out _id);
                    GL.NamedRenderbufferStorage(_id, _internalFormat, (int)Width, (int)Height);
                }
                else
                {
                    _id = GL.GenRenderbuffer();
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _id);
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, _internalFormat, (int)Width, (int)Height);
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                }
            }
            else //Never Called... done in Constructor
            {
                if (_device.GLFeatures.IsTextureViewSupported)
                {
                    _id = Unsafe.As<GLCoreRenderBuffer>(_referenceTexture)._id;
                }
                else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
            }
        }
        public override void GLFree()
        {
            if (_id != 0 && _referenceTexture == null)
            {
                GL.DeleteRenderbuffer(_id);
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
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.NamedFramebufferRenderbuffer(frameBufferID, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), RenderbufferTarget.Renderbuffer, _id);
            else GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), RenderbufferTarget.Renderbuffer, _id);
        }

        #endregion

    }
}
