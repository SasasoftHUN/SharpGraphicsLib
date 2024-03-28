using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Allocator;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreTextureCube : GLCoreTexture, IGLTextureCube
    {

        #region Constructors

        //Create Cube Texture by allocating memory
        internal GLCoreTextureCube(GLGraphicsDevice device, Vector2UInt resolution, TextureType type, DataFormat dataFormat, uint mipLevels) :
            base(device, TextureTarget.TextureCubeMap, new Vector3UInt(resolution.x, resolution.y, 1u), type, dataFormat, 6u, mipLevels)
        { }
        //Create just View
        internal GLCoreTextureCube(GLGraphicsDevice device, GLCoreTexture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
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
        {
            if (_device.Features.IsTextureViewSupported)
            {
                GLCoreTexture referenceTexture = _referenceTexture != null ? Unsafe.As<GLCoreTexture>(_referenceTexture) : this;
                GLCoreTextureCube textureView = new GLCoreTextureCube(_device, referenceTexture, dataFormat ?? DataFormat, _swizzle.Combine(swizzle), _mipmapRange.Combine(levels), _layerRange);
                _device.InitializeResource(textureView);
                return textureView;
            }
            else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
        }
        public ITexture2D CreateView(CubeFace face, DataFormat? dataFormat = null, in TextureSwizzle? swizzle = null, in TextureRange? levels = null)
        {
            if (_device.Features.IsTextureViewSupported)
            {
                GLCoreTexture referenceTexture = _referenceTexture != null ? Unsafe.As<GLCoreTexture>(_referenceTexture) : this;
                GLCoreTexture2D textureView = new GLCoreTexture2D(_device, referenceTexture, dataFormat ?? DataFormat, _swizzle.Combine(swizzle), _mipmapRange.Combine(levels), new TextureRange(_layerRange.start + (uint)face, 1u));
                _device.InitializeResource(textureView);
                return textureView;
            }
            else throw new NotSupportedException("Texture View is not supported in this OpenGL version!");
        }

        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, CubeFace face, uint elementIndexOffset = 0)
            => UseStagingBuffer(stagingBuffer, mipLevels, (uint)face, elementIndexOffset);


        public override void GLInitialize()
        {
            bool dsa = _device.GLFeatures.IsDirectStateAccessSupported && _device.GLFeatures.IsTextureStorageSupported;
            if (dsa)
                GL.CreateTextures(_target, 1, out _id);
            else _id = GL.GenTexture();

            if (_referenceTexture == null)
            {
                if (dsa)
                {
                    GL.TextureStorage2D(_id, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                    GLInitializeSwizzle(true);
                }
                else
                {
                    GL.BindTexture(_target, _id);

                    if (_device.GLFeatures.IsTextureStorageSupported)
                        GL.TexStorage2D(TextureTarget2d.TextureCubeMap, (int)MipLevels, (SizedInternalFormat)_pixelInternalFormat, (int)Width, (int)Height);
                    else
                    {
                        int x = (int)Width;
                        int y = (int)Height;
                        for (int i = 0; i < MipLevels; i++)
                        {
                            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            GL.TexImage2D(TextureTarget.TextureCubeMapNegativeX, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveY, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            GL.TexImage2D(TextureTarget.TextureCubeMapNegativeY, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
                            GL.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, i, _pixelInternalFormat, x, y, 0, _pixelFormat, _pixelType, IntPtr.Zero);
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
                GL.TextureSubImage3D(_id, (int)range.mipLevel,
                    (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.layers.start,
                    (int)range.extent.x, (int)range.extent.y, (int)range.layers.count,
                    _pixelFormat, _pixelType, data);
            else
            {
                GL.BindTexture(_target, _id);
                if (range.layers.count == 1u)
                    GL.TexSubImage2D((TextureTarget)((uint)TextureTarget.TextureCubeMapPositiveX + range.layers.start),
                        (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y,
                        (int)range.extent.x, (int)range.extent.y,
                        _pixelFormat, _pixelType, data);
                else
                {
                    int layerAddressOffset = (int)range.extent.x * (int)range.extent.y * _pixelFormat.ToElementCount() * _pixelType.ToByteCount();
                    for (int i = 0; i < range.layers.count; i++)
                        GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start + i),
                            (int)range.mipLevel,
                            (int)range.textureOffset.x, (int)range.textureOffset.y,
                            (int)range.extent.x, (int)range.extent.y,
                            _pixelFormat, _pixelType, data + layerAddressOffset * i);
                }
                GL.BindTexture(_target, 0);
            }
        }
        public override void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    GL.TextureSubImage3D(_id, (int)range.mipLevel,
                        (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.layers.start,
                        (int)range.extent.x, (int)range.extent.y, (int)range.layers.count,
                        _pixelFormat, _pixelType, data);
                }
            else
            {
                GL.BindTexture(_target, _id);
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    if (range.layers.count == 1u)
                        GL.TexSubImage2D((TextureTarget)((uint)TextureTarget.TextureCubeMapPositiveX + range.layers.start),
                            (int)range.mipLevel,
                            (int)range.textureOffset.x, (int)range.textureOffset.y,
                            (int)range.extent.x, (int)range.extent.y,
                            _pixelFormat, _pixelType, data);
                    else
                    {
                        int layerAddressOffset = (int)range.extent.x * (int)range.extent.y * _pixelFormat.ToElementCount() * _pixelType.ToByteCount();
                        for (int j = 0; i < range.layers.count; j++)
                            GL.TexSubImage2D((TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)range.layers.start + j),
                                (int)range.mipLevel,
                                (int)range.textureOffset.x, (int)range.textureOffset.y,
                                (int)range.extent.x, (int)range.extent.y,
                                _pixelFormat, _pixelType, data + layerAddressOffset * j);
                    }
                }
                GL.BindTexture(_target, 0);
            }
        }

        public override void GLReadData(IntPtr data, in CopyBufferTextureRange range)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.GetTextureSubImage(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.layers.start, (int)range.extent.x, (int)range.extent.y, (int)range.layers.count, _pixelFormat, _pixelType,
                    (int)range.extent.x * (int)range.extent.y * _pixelDataSize, data + (int)range.bufferOffset);
            else
            {
                if (range.textureOffset == Vector3UInt.Zero() && range.extent == _extent && range.layers == new TextureRange(0u, 6u))
                {
                    GL.BindTexture(_target, _id);
                    GL.GetTexImage(_target, (int)range.mipLevel, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                    GL.BindTexture(_target, 0);
                }
                else
                {
                    GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
                    for (int j = 0; j < range.layers.count; j++)
                    {
                        GL.FramebufferTexture3D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel, (int)range.layers.start + j);
                        GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                    }
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
                    GL.GetTextureSubImage(_id, (int)range.mipLevel, (int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.layers.start, (int)range.extent.x, (int)range.extent.y, (int)range.layers.count, _pixelFormat, _pixelType,
                        (int)range.extent.x * (int)range.extent.y * _pixelDataSize, data + (int)range.bufferOffset);
                }
            else
            {
                GL.GenFramebuffers(1, out int fboID); //TODO: Is this the only way in pre 4.5 OpenGL to get texture sub-data?
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboID);
                for (int i = 0; i < ranges.Length; i++)
                {
                    ref readonly CopyBufferTextureRange range = ref ranges[i];
                    for (int j = 0; j < range.layers.count; j++)
                    {
                        GL.FramebufferTexture3D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _target, _id, (int)range.mipLevel, (int)range.layers.start + j);
                        GL.ReadPixels((int)range.textureOffset.x, (int)range.textureOffset.y, (int)range.extent.x, (int)range.extent.y, _pixelFormat, _pixelType, data + (int)range.bufferOffset);
                    }
                }
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                GL.DeleteFramebuffers(1, ref fboID);
            }

            if (data == IntPtr.Zero)
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }

        public override void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex)
        {
            //TODO: Implement TextureCube as FBO attachment
            throw new NotImplementedException();
            //GL.FramebufferTexture(FramebufferTarget.Framebuffer, attachmentType.ToFramebufferAttachment(attachmentTypeIndex), _id, 0);
        }

        #endregion

    }
}
