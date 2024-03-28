using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLTexture : Texture, IGLTexture
    {

        #region Fields

        private bool _isDisposed;

        protected int _id;

        protected readonly GLGraphicsDevice _device;
        protected readonly int _pixelDataSize;

        #endregion

        #region Properties

        public int ID => _id;

        #endregion

        #region Constructors

        protected GLTexture(GLGraphicsDevice device, in Vector3UInt extent, uint layers, uint mipLevels, DataFormat dataFormat, TextureType type) :
            base(extent, layers, mipLevels, dataFormat, type)
        {
            _device = device;
            _pixelDataSize = dataFormat.GetFormatBytes();
        }

        protected GLTexture(GLGraphicsDevice device, GLTexture referenceTexture, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange, DataFormat dataFormat) :
            base(referenceTexture, swizzle, mipmapRange, layerRange, dataFormat)
        {
            _device = device;
            _pixelDataSize = dataFormat.GetFormatBytes();
        }

        ~GLTexture() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                //dispose managed state (managed objects)

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGLCore Texture from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine("Warning: GLCoreTexture cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, IGLTexture texture, in ReadOnlySpan<T> data, in TextureRange mipLevels, in TextureRange layers) where T : unmanaged
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (mipLevels.count == 1u)
            {
                if (IsUsingStagingBuffers)
                {
                    if (TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? stagingBuffer, out uint bufferElementIndexOffset))
                    {
                        stagingBuffer.StoreData(commandBuffer, data, bufferElementIndexOffset);
                        stagingBuffer.CopyTo(commandBuffer, texture, new CopyBufferTextureRange(
                            extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, texture.Extent),
                            mipLevel: mipLevels.start,
                            layers: layers,
                            bufferOffset: bufferElementIndexOffset * (ulong)Marshal.SizeOf<T>(),
                            textureOffset: new Vector3UInt()));
                    }
                    else throw new Exception("Suitable staging buffer not found for GLTexture StoreData");
                }
                else glCommandBuffer.AddDataTransferCommand(new GLTexSubImageCommand<T>(texture, glCommandBuffer.MemoryAllocator, data, new CopyBufferTextureRange(
                    extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, texture.Extent),
                    mipLevel: mipLevels.start,
                    layers: layers)));
            }
            else
            {
                Span<CopyBufferTextureRange> ranges = stackalloc CopyBufferTextureRange[(int)mipLevels.count];

                if (IsUsingStagingBuffers)
                {
                    if (TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? stagingBuffer, out uint bufferElementIndexOffset))
                    {
                        stagingBuffer.StoreData(commandBuffer, data, bufferElementIndexOffset);

                        ulong dataTypeSize = (ulong)Marshal.SizeOf<T>();
                        CopyBufferTextureRange.FillForMultipleMipLevels(ranges, texture.Extent, mipLevels, layers, bufferElementIndexOffset * dataTypeSize, dataTypeSize);
                        stagingBuffer.CopyTo(commandBuffer, texture, ranges);
                    }
                    else throw new Exception("Suitable staging buffer not found for GLTexture StoreData");
                }
                else
                {
                    CopyBufferTextureRange.FillForMultipleMipLevels(ranges, texture.Extent, mipLevels, layers, 0ul, (ulong)Marshal.SizeOf<T>());
                    glCommandBuffer.AddDataTransferCommand(new GLTexSubImageMultipleRangesCommand<T>(texture, glCommandBuffer.MemoryAllocator, data, ranges));
                }
            }
        }
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, IGLTexture texture, in ReadOnlyMemory<T> data, in TextureRange mipLevels, in TextureRange layers) where T : unmanaged
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (mipLevels.count == 1u)
            {
                if (IsUsingStagingBuffers)
                {
                    if (TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? stagingBuffer, out uint bufferElementIndexOffset))
                    {
                        stagingBuffer.StoreData(commandBuffer, data, bufferElementIndexOffset);
                        stagingBuffer.CopyTo(commandBuffer, texture, new CopyBufferTextureRange(
                            extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, texture.Extent),
                            mipLevel: mipLevels.start,
                            layers: layers,
                            bufferOffset: bufferElementIndexOffset * (ulong)Marshal.SizeOf<T>(),
                            textureOffset: new Vector3UInt()));
                    }
                    else throw new Exception("Suitable staging buffer not found for GLTexture StoreData");
                }
                else glCommandBuffer.AddDataTransferCommand(new GLTexSubImageCommand<T>(texture, glCommandBuffer.MemoryAllocator, data, new CopyBufferTextureRange(
                    extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, texture.Extent),
                    mipLevel: mipLevels.start,
                    layers: layers)));
            }
            else
            {
                Span<CopyBufferTextureRange> ranges = stackalloc CopyBufferTextureRange[(int)mipLevels.count];

                if (IsUsingStagingBuffers)
                {
                    if (TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? stagingBuffer, out uint bufferElementIndexOffset))
                    {
                        stagingBuffer.StoreData(commandBuffer, data, bufferElementIndexOffset);

                        ulong dataTypeSize = (ulong)Marshal.SizeOf<T>();
                        CopyBufferTextureRange.FillForMultipleMipLevels(ranges, texture.Extent, mipLevels, layers, bufferElementIndexOffset * dataTypeSize, dataTypeSize);
                        stagingBuffer.CopyTo(commandBuffer, texture, ranges);
                    }
                    else throw new Exception("Suitable staging buffer not found for GLTexture StoreData");
                }
                else
                {
                    CopyBufferTextureRange.FillForMultipleMipLevels(ranges, texture.Extent, mipLevels, layers, 0ul, (ulong)Marshal.SizeOf<T>());
                    glCommandBuffer.AddDataTransferCommand(new GLTexSubImageMultipleRangesCommand<T>(texture, glCommandBuffer.MemoryAllocator, data, ranges));
                }
            }
        }

        public override void GenerateMipmaps(GraphicsCommandBuffer commandBuffer)
            => Unsafe.As<GLCommandBufferList>(commandBuffer).AddDataTransferCommand(new GLGenerateMipMapsCommand(this));

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in CopyBufferTextureRange range)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (destination is IGLEmulatedStagingDataBuffer stagingDestination)
                glCommandBuffer.AddDataTransferCommand(new GLGetTexSubImageToEmulatedStagingBufferCommand(this, stagingDestination, range));
            else glCommandBuffer.AddDataTransferCommand(new GLGetTexSubImageToPackBufferCommand(this, Unsafe.As<IGLDataBuffer>(destination), range));
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (destination is IGLEmulatedStagingDataBuffer stagingDestination)
                glCommandBuffer.AddDataTransferCommand(new GLGetTexSubImageMultipleRangesToEmulatedStagingBufferCommand(this, glCommandBuffer.MemoryAllocator, stagingDestination, ranges));
            else glCommandBuffer.AddDataTransferCommand(new GLGetTexSubImageMultipleRangesToPackBufferCommand(this, glCommandBuffer.MemoryAllocator, Unsafe.As<IGLDataBuffer>(destination), ranges));
        }


        public abstract void GLInitialize();
        public abstract void GLFree();

        public abstract void GLBind(int binding, int textureUnit);
        public abstract void GLUnBind(int textureUnit);

        public abstract void GLStoreData(IntPtr data, in CopyBufferTextureRange range);
        public abstract void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges);
        public abstract void GLReadData(IntPtr data, in CopyBufferTextureRange range);
        public abstract void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges);
        public abstract void GLGenerateMipMaps();

        public abstract void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex);

        #endregion

    }
}
