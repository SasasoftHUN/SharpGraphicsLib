using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Allocator;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30TextureCube : GLES30Texture, IGLTextureCube
    {

        #region Constructors

        //Create Cube Texture by allocating memory
        internal GLES30TextureCube(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint mipLevels) :
            base(device, TextureTarget.TextureCubeMap, new Vector3UInt(resolution.x, resolution.y, 1u), type, dataFormat, 6u, mipLevels)
        { }
        //Create just View
        internal GLES30TextureCube(GLGraphicsDevice device, GLES30Texture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, TextureTarget.TextureCubeMap, referenceTexture, dataFormat, swizzle, mipmapRange, layerRange)
        { }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, new ReadOnlyMemory<T>(data), mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, new ReadOnlyMemory<T>(data), mipLevels, new TextureRange(0u, 6u));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, new TextureRange(0u, 6u));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreData(commandBuffer, this, data, mipLevels, new TextureRange(0u, 6u));

        public ITextureCube CreateView(DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
            => throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");
        public ITexture2D CreateView(CubeFace face, DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
            => throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");

        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, CubeFace face, uint elementIndexOffset = 0)
            => UseStagingBuffer(stagingBuffer, mipLevels, (uint)face, elementIndexOffset);


        public override void GLInitialize()
        {
            _id = GL.GenTexture();

            if (_referenceTexture == null)
            {
                GL.BindTexture(_target, _id);

#if ANDROID
                if (_device.GLFeatures.IsTextureStorageSupported)
                    GL.TexStorage2D(TextureTarget2D.TextureCubeMap, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                else
                {
                    int x = (int)Width;
                    int y = (int)Height;
                    for (int i = 0; i < MipLevels; i++)
                    {
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeX, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveY, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeY, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, i, (PixelInternalFormat)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        x = Math.Max(1, x / 2);
                        y = Math.Max(1, y / 2);
                    }
                }
#else
                if (_device.GLFeatures.IsTextureStorageSupported)
                    GL.TexStorage2D((TextureTarget2d)TextureTarget.TextureCubeMap, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                else
                {
                    int x = (int)Width;
                    int y = (int)Height;
                    for (int i = 0; i < MipLevels; i++)
                    {
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapPositiveX, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapNegativeX, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapPositiveY, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapNegativeY, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapPositiveZ, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        GL.TexImage2D(TextureTarget2d.TextureCubeMapNegativeZ, i, (TextureComponentCount)_pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                        x = Math.Max(1, x / 2);
                        y = Math.Max(1, y / 2);
                    }
                }
#endif

                GLInitializeSwizzle();
                GL.BindTexture(_target, 0);
            }
            else throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");
        }

        public override void GLStoreData(IntPtr data, in CopyBufferTextureRange range)
        {
            GL.BindTexture(_target, _id);
            if (range.layers.count == 1u)
            {
#if ANDROID
                GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start),
                    (int)range.mipLevel,
                    (int)range.textureOffset.x, (int)range.textureOffset.y,
                    (int)range.extent.x, (int)range.extent.y,
                    _pixelFormat, _pixelType, data);
#else
                GL.TexSubImage2D((TextureTarget2d)((int)TextureTarget2d.TextureCubeMapPositiveX + (int)range.layers.start),
                    (int)range.mipLevel,
                    (int)range.textureOffset.x, (int)range.textureOffset.y,
                    (int)range.extent.x, (int)range.extent.y,
                    _pixelFormat, _pixelType, data);
#endif
            }
            else
            {
                int layerAddressOffset = (int)range.extent.x * (int)range.extent.y * _pixelFormat.ToElementCount() * _pixelType.ToByteCount();
                for (int i = 0; i < range.layers.count; i++)
#if ANDROID
                    GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start + i),
                        (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y,
                        (int)range.extent.x, (int)range.extent.y,
                        _pixelFormat, _pixelType, data + layerAddressOffset * i);
#else
                    GL.TexSubImage2D((TextureTarget2d)((int)TextureTarget2d.TextureCubeMapPositiveX + (int)range.layers.start + i),
                        (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y,
                        (int)range.extent.x, (int)range.extent.y,
                        _pixelFormat, _pixelType, data + layerAddressOffset * i);
#endif
            }
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
                if (range.layers.count == 1u)
                {
#if ANDROID
                    GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start),
                        (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y,
                        (int)range.extent.x, (int)range.extent.y,
                        _pixelFormat, _pixelType, data);
#else
                    GL.TexSubImage2D((TextureTarget2d)((int)TextureTarget2d.TextureCubeMapPositiveX + (int)range.layers.start),
                        (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y,
                        (int)range.extent.x, (int)range.extent.y,
                        _pixelFormat, _pixelType, data);
#endif
                }
                else
                {
                    int layerAddressOffset = (int)range.extent.x * (int)range.extent.y * _pixelFormat.ToElementCount() * _pixelType.ToByteCount();
                    for (int j = 0; j < range.layers.count; j++)
#if ANDROID
                        GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start + j),
                            (int)range.mipLevel,
                            (int)range.textureOffset.x, (int)range.textureOffset.y,
                            (int)range.extent.x, (int)range.extent.y,
                            _pixelFormat, _pixelType, data + layerAddressOffset * j);
#else
                        GL.TexSubImage2D((TextureTarget2d)((int)TextureTarget2d.TextureCubeMapPositiveX + (int)range.layers.start + j),
                            (int)range.mipLevel,
                            (int)range.textureOffset.x, (int)range.textureOffset.y,
                            (int)range.extent.x, (int)range.extent.y,
                            _pixelFormat, _pixelType, data + layerAddressOffset * j);
#endif
                }
            }
            GL.BindTexture(_target, 0);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        public override void GLReadData(IntPtr data, in CopyBufferTextureRange range)
        {
            GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
            for (int j = 0; j < range.layers.count; j++)
            {
                GL.FramebufferTextureLayer(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _id, (int)range.mipLevel, (int)range.layers.start + j);
                GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
            }
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.DeleteFramebuffers(1, ref fboID);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }
        public override void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
            for (int i = 0; i < ranges.Length; i++)
            {
                ref readonly CopyBufferTextureRange range = ref ranges[i];
                for (int j = 0; j < range.layers.count; j++)
                {
                    GL.FramebufferTextureLayer(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _id, (int)range.mipLevel, (int)range.layers.start + j);
                    GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                }
            }
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.DeleteFramebuffers(1, ref fboID);

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }


        public override void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex)
        {
            //TODO: Implement TextureCube as FBO attachment
            throw new NotImplementedException();
            //GL.FramebufferTexture(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _target, _id, 0);
        }

        #endregion

    }
}
