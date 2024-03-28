using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace SharpGraphics
{
    public abstract class Texture : ITexture
    {

        #region Fields

        private bool _isDisposed = false;

        private bool _ownStagingBuffers = true;
        private List<TextureStagingBuffer>? _stagingBuffers = null; //Null is valid for optimization (not allocating empty list)

        protected readonly ITexture? _referenceTexture = null; //Null is valid when it is not a TextureView

        protected readonly Vector3UInt _extent;
        protected readonly DataFormat _dataFormat;
        protected readonly TextureType _type;

        protected readonly TextureSwizzle _swizzle;
        protected readonly TextureRange _mipmapRange;
        protected readonly TextureRange _layerRange;

        #endregion

        #region Properties

        protected bool OwnStagingBuffers => _ownStagingBuffers;
        protected bool IsUsingStagingBuffers => _stagingBuffers != null && _stagingBuffers.Count > 0;

        public ITexture? ReferenceTexture => _referenceTexture;
        public bool IsView => _referenceTexture != null;

        public uint Width => _extent.x;
        public uint Height => _extent.y;
        public Vector2UInt Resolution => new Vector2UInt(_extent.x, _extent.y);
        public uint Depth => _extent.z;
        public Vector3UInt Extent => _extent;
        public uint Layers => _layerRange.count;
        public uint MipLevels => _mipmapRange.count;
        public DataFormat DataFormat => _dataFormat;
        public TextureType Type => _type;

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        //Create a new Texture
        protected Texture(in Vector3UInt extent, uint layers, uint mipLevels, DataFormat dataFormat, TextureType type)
        {
            _extent = extent;
            _dataFormat = dataFormat;
            _type = type;

            _swizzle = new TextureSwizzle(TextureSwizzleType.Original);
            _mipmapRange = new TextureRange(0u, mipLevels);
            _layerRange = new TextureRange(0u, layers);
        }
        //Create a new Texture View
        protected Texture(ITexture referenceTexture, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange, DataFormat dataFormat)
        {
            _referenceTexture = referenceTexture;

            _extent = mipmapRange.start.CalculateMipLevelExtent(referenceTexture.Extent);
            _dataFormat = dataFormat;
            _type = referenceTexture.Type;

            _swizzle = swizzle;
            _mipmapRange = mipmapRange;
            _layerRange = layerRange;
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Texture()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddStagingBuffer(in TextureStagingBuffer textureStagingBuffer)
        {
            if (_stagingBuffers == null)
                _stagingBuffers = new List<TextureStagingBuffer>();
            _stagingBuffers.Add(textureStagingBuffer);
        }
        protected bool TryGetStagingBuffer(in TextureRange layers, in TextureRange mipLevels, out TextureStagingBuffer result)
        {
            if (_stagingBuffers != null)
                foreach (TextureStagingBuffer stagingBuffer in _stagingBuffers)
                    if (stagingBuffer.IsInRange(layers, mipLevels))
                    {
                        result = stagingBuffer;
                        return stagingBuffer.stagingBuffer != null;
                    }

            result = new TextureStagingBuffer();
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetStagingBuffer<T>(in TextureRange layers, in TextureRange mipLevels, [NotNullWhen(returnValue: true)] out IStagingDataBuffer<T>? stagingBuffer, out uint bufferElementIndexOffset) where T : unmanaged
        {
            if (TryGetStagingBuffer(layers, mipLevels, out TextureStagingBuffer textureStagingBuffer))
                if (textureStagingBuffer.stagingBuffer is IStagingDataBuffer<T> typedStagingBuffer)
                {
                    stagingBuffer = typedStagingBuffer;
                    bufferElementIndexOffset = textureStagingBuffer.bufferElementIndexOffset;
                    return true;
                }

            stagingBuffer = null;
            bufferElementIndexOffset = 0u;
            return false;
        }
        protected bool RemoveStagingBuffer(in TextureRange layers, in TextureRange mipLevels)
        {
            bool removed = false;

            if (_stagingBuffers != null)
                for (int i = 0; i < _stagingBuffers.Count; i++)
                    if (_stagingBuffers[i].IsInRange(layers, mipLevels))
                    {
                        if (_stagingBuffers[i].stagingBuffer != null)
                            _stagingBuffers[i].stagingBuffer.Dispose();
                        _stagingBuffers.RemoveAt(i--);
                        removed = true;
                    }

            return removed;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                ReleaseStagingBuffers();

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public abstract void GenerateMipmaps(GraphicsCommandBuffer commandBuffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, uint elementIndexOffset = 0u)
            => UseStagingBuffer(stagingBuffer, new TextureRange(0u, MipLevels), new TextureRange(0u, Layers), elementIndexOffset);
        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, uint elementIndexOffset = 0u)
            => UseStagingBuffer(stagingBuffer, mipLevels, new TextureRange(0u, Layers), elementIndexOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, in TextureRange layers, uint elementIndexOffset = 0u)
        {
            if (_stagingBuffers == null)
                _stagingBuffers = new List<TextureStagingBuffer>();
            else if (_ownStagingBuffers)
                ReleaseStagingBuffers(true);

            //TODO: Check max layer and level count!
            _stagingBuffers.Add(new TextureStagingBuffer(stagingBuffer, layers, mipLevels, elementIndexOffset));
            _ownStagingBuffers = false;
        }
        public void ReleaseStagingBuffers(bool keepInternalList = false)
        {
            if (_stagingBuffers != null)
            {
                if (_ownStagingBuffers)
                    foreach (TextureStagingBuffer stagingBuffer in _stagingBuffers)
                        if (stagingBuffer.stagingBuffer != null)
                            stagingBuffer.stagingBuffer.Dispose();

                if (keepInternalList)
                    _stagingBuffers.Clear();
                else _stagingBuffers = null;
                _ownStagingBuffers = true;
            }
        }

        public abstract void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in CopyBufferTextureRange range);
        public virtual void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            foreach (CopyBufferTextureRange range in ranges)
                CopyTo(commandBuffer, destination, range);
        }
        public void CopyTo<T>(GraphicsCommandBuffer commandBuffer, IDataBuffer<T> destination, in TextureRange mipLevels, in TextureRange layers, uint destinationElementIndexOffset = 0u) where T : unmanaged
        {
            if (mipLevels.count == 1u)
                CopyTo(commandBuffer, destination, new CopyBufferTextureRange(
                    extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, Extent),
                    mipLevel: mipLevels.start,
                    layers: layers,
                    bufferOffset: destinationElementIndexOffset * destination.DataTypeSize,
                    textureOffset: new Vector3UInt(0u)
                    ));
            else
            {
                Span<CopyBufferTextureRange> ranges = stackalloc CopyBufferTextureRange[(int)mipLevels.count];
                CopyBufferTextureRange.FillForMultipleMipLevels(ranges, Extent, mipLevels, layers, destinationElementIndexOffset * destination.DataTypeSize, destination.DataTypeSize);

                CopyTo(commandBuffer, destination, ranges);
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
