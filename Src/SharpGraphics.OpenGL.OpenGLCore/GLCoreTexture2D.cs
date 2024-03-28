using OpenTK.Graphics.OpenGL;
using SharpGraphics.Allocator;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreTexture2D : GLCoreTexture, IGLTexture2D
    {

        #region Constructors

        //Create 2D Texture by allocating memory
        internal GLCoreTexture2D(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint mipLevels) :
            base(device, TextureTarget.Texture2D, new Vector3UInt(resolution.x, resolution.y, 1u), type, dataFormat, 1u, mipLevels) { }
        //Create just View
        internal GLCoreTexture2D(GLGraphicsDevice device, GLCoreTexture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, TextureTarget.Texture2D, referenceTexture, dataFormat, swizzle, mipmapRange, layerRange) { }

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
        {
            if (_device.Features.IsTextureViewSupported)
            {
                GLCoreTexture referenceTexture = _referenceTexture != null ? Unsafe.As<GLCoreTexture>(_referenceTexture) : this;
                GLCoreTexture2D textureView = new GLCoreTexture2D(_device, referenceTexture, dataFormat ?? DataFormat, _swizzle.Combine(swizzle), _mipmapRange.Combine(levels), _layerRange);
                _device.InitializeResource(textureView);
                return textureView;
            }
            else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
        }


        public override void GLInitialize()
        {
            bool dsa = _device.GLFeatures.IsDirectStateAccessSupported && _device.GLFeatures.IsTextureStorageSupported;

            if (_referenceTexture == null)
            {
                if (dsa)
                {
                    GL.CreateTextures(_target, 1, out _id);
                    GL.TextureStorage2D(_id, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                    GLInitializeSwizzle(true);
                }
                else
                {
                    _id = GL.GenTexture();
                    GL.BindTexture(_target, _id);

                    if (_device.GLFeatures.IsTextureStorageSupported)
                        GL.TexStorage2D(TextureTarget2d.Texture2D, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                    else
                    {
                        int x = (int)Width;
                        int y = (int)Height;
                        for (int i = 0; i < MipLevels; i++)
                        {
                            GL.TexImage2D(_target, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            x = Math.Max(1, x / 2);
                            y = Math.Max(1, y / 2);
                        }
                    }

                    GLInitializeSwizzle(false);
                    GL.BindTexture(_target, 0);
                }
            }
            else
            {
                if (_device.GLFeatures.IsTextureViewSupported)
                {
                    _id = GL.GenTexture();
                    GL.TextureView(_id, _target, Unsafe.As<IGLTexture>(_referenceTexture).ID, _pixelInternalFormat, (int)_mipmapRange.start, (int)_mipmapRange.count, (int)_layerRange.start, (int)_layerRange.count);

                    if (!dsa)
                        GL.BindTexture(_target, _id);
                    GLInitializeSwizzle(dsa);
                    if (!dsa)
                        GL.BindTexture(_target, 0);
                }
                else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
            }
        }

        public override void GLStoreData(IntPtr data, in CopyBufferTextureRange range)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.TextureSubImage2D(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
            else
            {
                GL.BindTexture(_target, _id);
                GL.TexSubImage2D(_target, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                GL.BindTexture(_target, 0);
            }

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }
        public override void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    GL.TextureSubImage2D(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                }
            else
            {
                GL.BindTexture(_target, _id);
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    GL.TexSubImage2D(_target, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                }
                GL.BindTexture(_target, 0);
            }

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        public override void GLReadData(IntPtr data, in CopyBufferTextureRange range)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.GetTextureSubImage(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, 0, (int)range.extent.x, (int)range.extent.y, 1, _pixelFormat, _pixelType,
                    (int)range.extent.x * (int)range.extent.y * _pixelDataSize, data + (int)range.bufferOffset);
            else
            {
                if (range.textureOffset == Vector3UInt.Zero() && range.extent == _extent)
                {
                    GL.BindTexture(_target, _id);
                    GL.GetTexImage(_target, (int)range.mipLevel, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                    GL.BindTexture(_target, 0);
                }
                else
                {
                    GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
                    GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel);
                    GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                    GL.DeleteFramebuffers(1, ref fboID);
                }
            }

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }
        public override void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    GL.GetTextureSubImage(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, 0, (int)range.extent.x, (int)range.extent.y, 1, _pixelFormat, _pixelType,
                        (int)range.extent.x * (int)range.extent.y * _pixelDataSize, data + (int)range.bufferOffset);
                }
            else
            {
                GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel);
                    GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                }
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                GL.DeleteFramebuffers(1, ref fboID);
            }

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }

        public override void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.NamedFramebufferTexture(frameBufferID, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _id, 0);
            else GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _target, _id, 0);
        }

        #endregion

    }
}
