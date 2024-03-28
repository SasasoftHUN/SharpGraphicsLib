using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;
using System.Runtime.InteropServices;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Allocator;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30Texture2D : GLES30Texture, IGLTexture2D
    {

        #region Fields

#if ANDROID
        private readonly TextureTarget2D _target2D;
#else
        private readonly TextureTarget2d _target2D;
#endif

        #endregion

        #region Constructors

        //Create 2D Texture by allocating memory
        internal GLES30Texture2D(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint mipLevels) :
            base(device, TextureTarget.Texture2D, new Vector3UInt(resolution.x, resolution.y, 1u), type, dataFormat, 1u, mipLevels)
        {
#if ANDROID
            _target2D = TextureTarget2D.Texture2D;
#else
            _target2D = TextureTarget2d.Texture2D;
#endif
        }
        //Create 2D Texture View
        internal GLES30Texture2D(GLGraphicsDevice device, GLES30Texture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, TextureTarget.Texture2D, referenceTexture, dataFormat, swizzle, mipmapRange, layerRange)
        {
#if ANDROID
            _target2D = TextureTarget2D.Texture2D;
#else
            _target2D = TextureTarget2d.Texture2D;
#endif
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, new ReadOnlyMemory<T>(data), mipLevels, 0u);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, 0u);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, 0u);

        public ITexture2D CreateView(DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
            => throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");


        public override void GLInitialize()
        {
            _id = GL.GenTexture();

            if (_referenceTexture == null)
            {
                GL.BindTexture(_target, _id);

                if (_device.GLFeatures.IsTextureStorageSupported)
                    GL.TexStorage2D(_target2D, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                else
                {
                    int x = (int)Width;
                    int y = (int)Height;
                    for (int i = 0; i < MipLevels; i++)
                    {
#if ANDROID
                        GL.TexImage2D(_target, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
#else
                        GL.TexImage2D(_target2D, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
#endif
                        x = Math.Max(1, x / 2);
                        y = Math.Max(1, y / 2);
                    }
                }

                GL.TexParameter(_target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(_target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(_target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(_target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                GLInitializeSwizzle();
                GL.BindTexture(_target, 0);
            }
            else throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");
        }

        public override void GLStoreData(IntPtr data, in CopyBufferTextureRange range)
        {
            GL.BindTexture(_target, _id);
#if ANDROID
            GL.TexSubImage2D(_target, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
#else
            GL.TexSubImage2D(_target2D, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
#endif
            GL.BindTexture(_target, 0);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }
        public override void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GL.BindTexture(_target, _id);
            for (int i = 0; i < ranges.Length; i++)
            {
                ref readonly CopyBufferTextureRange range = ref ranges[i];
#if ANDROID
                GL.TexSubImage2D(_target, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
#else
                GL.TexSubImage2D(_target2D, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
#endif
            }
            GL.BindTexture(_target, 0);
        }

        public override void GLReadData(IntPtr data, in CopyBufferTextureRange range)
        {
            GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in GLES 3.0 to get texture sub-data?
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
#if ANDROID
            GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, (FramebufferSlot)FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel);
#else
            GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target2D, _id, (int)range.mipLevel);
#endif
            GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.DeleteFramebuffers(1, ref fboID);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }
        public override void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in GLES 3.0 to get texture sub-data?
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
            for (int i = 0; i < ranges.Length; i++)
            {
                ref readonly CopyBufferTextureRange range = ref ranges[i];
#if ANDROID
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, (FramebufferSlot)FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel);
#else
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target2D, _id, (int)range.mipLevel);
#endif
                GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
            }
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.DeleteFramebuffers(1, ref fboID);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }


        public override void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex)
        {
#if ANDROID
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, (FramebufferSlot)attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _target, _id, 0);
#else
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _target2D, _id, 0);
#endif
        }

        #endregion

    }
}
