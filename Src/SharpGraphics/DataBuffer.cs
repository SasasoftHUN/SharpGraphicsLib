using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.Utils;

namespace SharpGraphics
{
    
    public readonly ref struct MemoryType
    {
        
        private readonly MappableMemoryType? _type;

        public static MemoryType DeviceOnly => new MemoryType(default(MappableMemoryType?));

        public bool IsDeviceOnly => !_type.HasValue;
        public bool IsMappable => _type.HasValue;
        public MappableMemoryType MappableType => _type.HasValue ? _type.Value : MappableMemoryType.DontCare;

        private MemoryType(MappableMemoryType? type) => _type = type;

        public static MemoryType Mappable(MappableMemoryType mappableType) => new MemoryType(mappableType);

    }

    [Flags]
    public enum MappableMemoryType : uint
    {
        DontCare = 0u,
        DeviceLocal = 1u,
        Coherent = 2u, Cached = 4u,
    }

    [Flags]
    public enum DataBufferType : uint
    {
        Unknown = 0u,
        Read = 1u, Store = 2u,
        CopySource = 4u, CopyDestination = 8u,
        VertexData = 16u, IndexData = 32u,
        UniformData = 64u,

        /*
        Vulkan:
            TransferSrc, TransferDst,
            UniformTexel, StorageTexel, Uniform, Storage,
            Index, Vertex, Indirect,
        Vulkan 1.2:
            ShaderDeviceAddress
        Vulkan Extensions:
            VideoDecodeSrc, VideoDecodeDst,
            TransformFeedbackBufferEXT, TransformFeedbackCounterBufferEXT, ConditionalRenderingEXT,
            AccelerationStructureBuildInputReadOnly, AccelerationStructureStorage, ShaderBindingTable,
            RayTracingNV, ShaderDeviceAddress

        OpenGL:
            GL_ARRAY_BUFFER,
            GL_ATOMIC_COUNTER_BUFFER,
            GL_COPY_READ_BUFFER,
            GL_COPY_WRITE_BUFFER,
            GL_DISPATCH_INDIRECT_BUFFER,
            GL_DRAW_INDIRECT_BUFFER,
            GL_ELEMENT_ARRAY_BUFFER,
            GL_PIXEL_PACK_BUFFER,
            GL_PIXEL_UNPACK_BUFFER
            GL_QUERY_BUFFER,
            GL_SHADER_STORAGE_BUFFER,
            GL_TEXTURE_BUFFER,
            GL_TRANSFORM_FEEDBACK_BUFFER,
            GL_UNIFORM_BUFFER
        */
    }

    public interface IDataBuffer : IDisposable
    {

        /// <summary>
        /// Allocated bytes in this Buffer
        /// </summary>
        ulong Size { get; }
        /// <summary>
        /// The Type of this Buffer determines it's allowed usage and <see cref="DeviceOffset"/>
        /// </summary>
        DataBufferType Type { get; }

        /// <summary>
        /// Has the Buffer been Disposed. If true, then this Buffer must be not used in any way neither on System or on Device side! Disposed resources cannot be reinitialized.
        /// </summary>
        public bool IsDisposed { get; }

        /// <summary>
        /// Copy data from System side to the Buffer.
        /// This method expects this <see cref="IDataBuffer"/> to be either an <see cref="IMappableDataBuffer"/> or <see cref="IStagingDataBuffer"/> (currently in an UnMapped state with <see cref="DataBufferType.Store"/> type) or to be an
        /// <see cref="IDeviceOnlyDataBuffer"/> (with <see cref="DataBufferType.Store"/> type and with a correct <see cref="IStagingDataBuffer"/> assigned in it).
        /// </summary>
        /// <param name="commandBuffer">Data copy commands will be added into this <see cref="GraphicsCommandBuffer"/> to be executed on the Device. The method expects the <see cref="GraphicsCommandBuffer"/> to be in a Started state with no active <see cref="IRenderPass"/>.</param>
        /// <param name="data">Pointer to the data in System memory to be copied. The pointer should be Pinned!</param>
        /// <param name="size">Size of the data to be copied in bytes. <paramref name="offset"/> + <paramref name="size"/> must be less then this <see cref="Size"/>!</param>
        /// <param name="offset">Offset in this Buffer for writing the <paramref name="data"/> (0 means the beginning of the <see cref="IDataBuffer"/>). <paramref name="offset"/> + <paramref name="size"/> must be less then this <see cref="Size"/>!</param>
        void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul);


        void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, ulong size, ulong sourceOffset = 0ul, ulong destinationOffset = 0ul);
        void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in CopyBufferTextureRange range);
        void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in ReadOnlySpan<CopyBufferTextureRange> ranges);
        void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in TextureRange mipLevels, in TextureRange layers, ulong sourceOffset = 0ul);


        /// <summary>
        /// Reallocates this Buffer with the given size in bytes. All data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="size">New size of the buffer in bytes.</param>
        void Resize(ulong size);
        /// <summary>
        /// If this <see cref="Size"/> is less then the given size in bytes, then it reallocates this Buffer with the given size. On resize, all data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="size">New size of the buffer in bytes.</param>
        void EnsureSize(ulong size);

    }
    public interface IDataBuffer<T> : IDataBuffer where T : unmanaged
    {

        /// <summary>
        /// Maximum number of elements of T (<see cref="IDataBuffer{T}"/>) to be stored in this Buffer
        /// </summary>
        uint Capacity { get; }

        /// <summary>
        /// Size of an element T (<see cref="IDataBuffer{T}"/>) stored in this Buffer in bytes
        /// </summary>
        uint DataTypeSize { get; }
        /// <summary>
        /// Requried Alignment of elements by the Device. Offset between elements in this Buffer must be a multiple of DeviceOffset. Can be different than <see cref="DataTypeSize"/>
        /// </summary>
        uint DeviceOffset { get; }
        /// <summary>
        /// Requried Padding between elements by the Device. Greater than 0 when <see cref="DataTypeSize"/> and <see cref="DeviceOffset"/> are different.
        /// </summary>
        uint AlignmentPadding { get; }
        /// <summary>
        /// Required offset of elements of T (<see cref="IDataBuffer{T}"/>) by the Device. It is a multiple of <see cref="DeviceOffset"/> and the sum of <see cref="DataTypeSize"/> and <see cref="AlignmentPadding"/>
        /// </summary>
        uint ElementOffset { get; }

        /// <summary>
        /// Copy data from System side to the Buffer.
        /// This method expects this <see cref="IDataBuffer"/> to be either an <see cref="IMappableDataBuffer"/> or <see cref="IStagingDataBuffer"/> (currently in an UnMapped state with <see cref="DataBufferType.Store"/> type) or to be an
        /// <see cref="IDeviceOnlyDataBuffer"/> (with <see cref="DataBufferType.Store"/> type and with a correct <see cref="IStagingDataBuffer"/> assigned in it).
        /// </summary>
        /// <param name="commandBuffer">Data copy commands will be added into this <see cref="GraphicsCommandBuffer"/> to be executed on the Device. The method expects the <see cref="GraphicsCommandBuffer"/> to be in a Started state with no active <see cref="IRenderPass"/>.</param>
        /// <param name="data">Instance of <see cref="T"/> to be copied into this <see cref="IDataBuffer{T}"/>.</param>
        /// <param name="elementIndexOffset">Element index in this Buffer for writing <paramref name="data"/> (0 means the beginning of the <see cref="IDataBuffer"/>). Respects the alignment setting of this <see cref="IDataBuffer{T}"/>. <paramref name="elementIndexOffset"/> must be less then this <see cref="IDataBuffer.Capacity"/>!</param>
        void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u);
        /// <summary>
        /// Copy data from System side to the Buffer.
        /// This method expects this <see cref="IDataBuffer"/> to be either an <see cref="IMappableDataBuffer"/> or <see cref="IStagingDataBuffer"/> (currently in an UnMapped state with <see cref="DataBufferType.Store"/> type) or to be an
        /// <see cref="IDeviceOnlyDataBuffer"/> (with <see cref="DataBufferType.Store"/> type and with a correct <see cref="IStagingDataBuffer"/> assigned in it).
        /// </summary>
        /// <param name="commandBuffer">Data copy commands will be added into this <see cref="GraphicsCommandBuffer"/> to be executed on the Device. The method expects the <see cref="GraphicsCommandBuffer"/> to be in a Started state with no active <see cref="IRenderPass"/>.</param>
        /// <param name="data">Instances of <see cref="T"/> to be copied into this <see cref="IDataBuffer{T}"/>. Pinning not required. <paramref name="elementIndexOffset"/> + <paramref name="data"/>.Length must be less than this <see cref="IDataBuffer.Capacity"/>!</param>
        /// <param name="elementIndexOffset">Element index in this Buffer for writing the first element of <paramref name="data"/> (0 means the beginning of the <see cref="IDataBuffer"/>). Respects the alignment setting of this <see cref="IDataBuffer{T}"/>. <paramref name="elementIndexOffset"/> + <paramref name="data"/>.Length must be less than this <see cref="IDataBuffer.Capacity"/>!</param>
        void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u);
        void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u);

        void CopyElementsTo(GraphicsCommandBuffer commandBuffer, IDataBuffer<T> destination, uint elementCount, uint sourceElementIndexOffset = 0u, ulong destinationElementIndexOffset = 0u);

        /// <summary>
        /// Reallocates this Buffer with the given capacity for elements of <see cref="T"/>. All data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="dataCapacity">New capacity of the buffer. Capacity for elements of <see cref="T"/>, respects the alignment setting of this <see cref="IDataBuffer{T}"/>.</param>
        void ResizeCapacity(int dataCapacity);
        /// <summary>
        /// Reallocates this Buffer with the given capacity for elements of <see cref="T"/>. All data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="dataCapacity">New capacity of the buffer. Capacity for elements of <see cref="T"/>, respects the alignment setting of this <see cref="IDataBuffer{T}"/>.</param>
        void ResizeCapacity(uint dataCapacity);
        /// <summary>
        /// If this <see cref="IDataBuffer.Capacity"/> is less then the given capacity for elements of <see cref="T"/>, then it reallocates this Buffer with the given capacity. On resize, all data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="dataCapacity">New capacity of the buffer. Capacity for elements of <see cref="T"/>, respects the alignment setting of this <see cref="IDataBuffer{T}"/>.</param>
        void EnsureCapacity(int dataCapacity);
        /// <summary>
        /// If this <see cref="IDataBuffer.Capacity"/> is less then the given capacity for elements of <see cref="T"/>, then it reallocates this Buffer with the given capacity. On resize, all data in the buffer will be lost and the newly allocated memory is uninitialized.
        /// </summary>
        /// <param name="dataCapacity">New capacity of the buffer. Capacity for elements of <see cref="T"/>, respects the alignment setting of this <see cref="IDataBuffer{T}"/>.</param>
        void EnsureCapacity(uint dataCapacity);

    }
    public interface IMappableDataBuffer : IDataBuffer
    {

        /// <summary>
        /// Is this <see cref="IMappableDataBuffer"/> currently in a Mapped state?
        /// </summary>
        bool IsMapped { get; }
        /// <summary>
        /// Get the <see cref="IntPtr"/> of the currently Mapped memory of this <see cref="IMappableDataBuffer"/>.
        /// </summary>
        IntPtr MappedPointer { get; }
        /// <summary>
        /// Get the unsafe pointer of the currently Mapped memory of this <see cref="IMappableDataBuffer"/>.
        /// </summary>
        unsafe void* MappedRawPointer { get; }

        /// <summary>
        /// Map the memory of the whole <see cref="IMappableDataBuffer"/> and return it's <see cref="IntPtr"/>.
        /// </summary>
        /// <returns><see cref="IntPtr"/> of the Mapped memory.</returns>
        IntPtr MapMemory();
        IntPtr MapMemory(ulong offset, ulong size);
        void FlushMappedSystemMemory();
        void FlushMappedDeviceMemory();
        void FlushMappedSystemMemory(ulong offset, ulong size);
        void FlushMappedDeviceMemory(ulong offset, ulong size);
        void UnMapMemory();
    }
    public interface IMappableDataBuffer<T> : IDataBuffer<T>, IMappableDataBuffer where T : unmanaged
    {
        //TODO: unsafe T* MappedDataRawPointer { get; }

        IntPtr MapMemoryElements(uint elementIndexOffset, uint elementCount);
        void FlushMappedSystemMemoryElements(uint elementIndexOffset, uint elementCount);
        void FlushMappedDeviceMemoryElements(uint elementIndexOffset, uint elementCount);

        T[] GatherMappedDataElements();

    }
    public interface IStagingDataBuffer : IMappableDataBuffer
    {
    }
    public interface IStagingDataBuffer<T> : IMappableDataBuffer<T>, IStagingDataBuffer where T : unmanaged
    {

    }
    public interface IDeviceOnlyDataBuffer : IDataBuffer
    {

    }
    public interface IDeviceOnlyDataBuffer<T> : IDataBuffer<T>, IDeviceOnlyDataBuffer where T : unmanaged
    {
        //TODO: BufferDatas with StagingBuffer parameter?

        void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer);
        void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer, uint elementIndexOffset);
        void ReleaseStagingBuffer();
    }

    public abstract class DataBuffer<T> : IDataBuffer<T> where T : unmanaged
    {

        #region Fields

        protected readonly static uint _dataTypeSize;

        private bool _isDisposed;

        protected readonly DataBufferType _type;
        protected readonly uint _deviceOffset;
        protected readonly uint _alignmentPadding;
        protected readonly uint _elementOffset;

        protected uint _capacity;
        protected ulong _size;

        #endregion

        #region Properties

        public static uint DataTypeSize => _dataTypeSize;

        public uint Capacity => _capacity;
        public ulong Size => _size;
        public DataBufferType Type => _type;
        uint IDataBuffer<T>.DataTypeSize => _dataTypeSize;
        public uint DeviceOffset => _deviceOffset;
        public uint AlignmentPadding => _alignmentPadding;
        public uint ElementOffset => _elementOffset;

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        static DataBuffer()
        {
            _dataTypeSize = (uint)Marshal.SizeOf<T>();
        }

        protected DataBuffer(GraphicsDevice device, uint dataCapacity, DataBufferType type, DataBufferType alignmentType)
        {
            _capacity = dataCapacity;
            _type = type;

            GetAlignment(device.Limits, alignmentType, out _deviceOffset, out _alignmentPadding, out _elementOffset);
            _size = (ulong)dataCapacity * _elementOffset;
        }
        //TODO: Custom Alignment?

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DataBuffer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void AssertStoreData(ulong sizeRequired)
        {
            Debug.Assert(_size >= sizeRequired, $"DataBuffer<{typeof(T).FullName}> has not enough allocated memory (Size: {_size}) for {sizeRequired} bytes during StoreData!");
        }
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void AssertStoreDataElements(uint dataCapacityRequired)
        {
            Debug.Assert(Capacity >= dataCapacityRequired, $"DataBuffer<{typeof(T).FullName}> has not enough allocated memory (Data Capacity: {Capacity}) for {dataCapacityRequired} elements during StoreData!");
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void AssertCopyTo(IDataBuffer destination)
        {
            Debug.Assert(_type.HasFlag(DataBufferType.CopySource), $"DataBuffer<{typeof(T).FullName}> has no CopySource usage for CopyTo!");
            Debug.Assert(destination.Type.HasFlag(DataBufferType.CopyDestination), $"DataBuffer<{typeof(T).FullName}>'s destination Buffer has no CopyDestination usage for CopyTo!");
        }
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void AssertCopyTo(IDataBuffer destination, ulong sourceSizeRequired, ulong destinationSizeRequired)
        {
            AssertCopyTo(destination);
            Debug.Assert(_size >= sourceSizeRequired, $"DataBuffer<{typeof(T).FullName}> has not enough allocated memory (Size: {_size}) for {sourceSizeRequired} bytes during CopyTo!");
            Debug.Assert(destination.Size >= destinationSizeRequired, $"DataBuffer<{typeof(T).FullName}>'s destination Buffer has not enough allocated memory (Size: {destination.Size}) for {destinationSizeRequired} bytes during CopyTo!");
        }


        protected static void GetAlignment(GraphicsDeviceLimits limits, DataBufferType type, out uint deviceOffset, out uint alignmentPadding, out uint elementOffset)
        {
            deviceOffset = 0u;

            if (type.HasFlag(DataBufferType.UniformData)) deviceOffset = limits.UniformBufferAlignment;

            if (deviceOffset > 0u)
            {
                alignmentPadding = _dataTypeSize == deviceOffset ? 0u :
                    (_dataTypeSize > deviceOffset ? _dataTypeSize % deviceOffset : deviceOffset % _dataTypeSize);
                elementOffset = ((uint)MathF.Ceiling(_dataTypeSize / (float)deviceOffset)) * deviceOffset;
            }
            else
            {
                alignmentPadding = 0u;
                elementOffset = _dataTypeSize;
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Internal Methods


        #endregion

        #region Public Methods

        public abstract void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul);
        public abstract void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u);
        public abstract void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u);
        public abstract void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u);

        public abstract void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, ulong size, ulong sourceOffset = 0ul, ulong destinationOffset = 0ul);
        public void CopyElementsTo(GraphicsCommandBuffer commandBuffer, IDataBuffer<T> destination, uint elementCount, uint sourceElementIndexOffset = 0u, ulong destinationElementIndexOffset = 0u)
            => CopyTo(commandBuffer, destination, elementCount * _elementOffset, sourceElementIndexOffset * _elementOffset, destinationElementIndexOffset * _elementOffset);

        public abstract void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in CopyBufferTextureRange range);
        public virtual void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            foreach (CopyBufferTextureRange range in ranges)
                CopyTo(commandBuffer, destination, range);
        }
        public void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in TextureRange mipLevels, in TextureRange layers, ulong sourceOffset = 0ul)
        {
            if (mipLevels.count == 1u)
                CopyTo(commandBuffer, destination, new CopyBufferTextureRange(
                    extent: GraphicsUtils.CalculateMipLevelExtent(mipLevels.start, destination.Extent),
                    mipLevel: mipLevels.start,
                    layers: layers,
                    bufferOffset: sourceOffset,
                    textureOffset: new Vector3UInt(0u)
                    ));
            else
            {
                Span<CopyBufferTextureRange> ranges = stackalloc CopyBufferTextureRange[(int)mipLevels.count];
                CopyBufferTextureRange.FillForMultipleMipLevels(ranges, destination.Extent, mipLevels, layers, sourceOffset, _dataTypeSize);

                CopyTo(commandBuffer, destination, ranges);
            }
        }

        public virtual void Resize(ulong size)
        {
            _capacity = (uint)(size / _elementOffset);
            _size = size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSize(ulong size)
        {
            if (_size < size)
                Resize(size);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeCapacity(int dataCapacity) => ResizeCapacity((uint)dataCapacity);
        public virtual void ResizeCapacity(uint dataCapacity)
        {
            _capacity = dataCapacity;
            _size = dataCapacity * _elementOffset;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int dataCapacity) => EnsureCapacity((uint)dataCapacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(uint dataCapacity)
        {
            if (_capacity < dataCapacity)
                ResizeCapacity(dataCapacity);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

    public enum DataFormat : uint
    {
        Undefined = 0,
        //R4g4UnormPack8 = 1,
        //R4g4b4a4UnormPack16 = 2,
        //B4g4r4a4UnormPack16 = 3,
        //R5g6b5UnormPack16 = 4,
        //B5g6r5UnormPack16 = 5,
        //R5g5b5a1UnormPack16 = 6,
        //B5g5r5a1UnormPack16 = 7,
        //A1r5g5b5UnormPack16 = 8,

        /// <summary>
        /// One 8-bit component, unsigned normalized value in the range [0,1]
        /// </summary>
        R8un = 9,
        /// <summary>
        /// One 8-bit component, signed normalized value in the range [-1,1]
        /// </summary>
        R8n = 10,
        /// <summary>
        /// One 8-bit component, unsigned integer value that get converted to floating-point in the range [0f,255f]
        /// </summary>
        R8us = 11,
        /// <summary>
        /// One 8-bit component, unsigned integer value that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        R8s = 12,
        /// <summary>
        /// One 8-bit component, unsigned integer value in the range [0,255]
        /// </summary>
        R8ui = 13,
        /// <summary>
        /// One 8-bit component, signed integer value in the range [-128,127]
        /// </summary>
        R8i = 14,
        /// <summary>
        /// One 8-bit component, unsigned normalized value in sRGB nonlinear encoding
        /// </summary>
        R8srgb = 15,

        /// <summary>
        /// Two 8-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RG8un = 16,
        /// <summary>
        /// Two 8-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RG8n = 17,
        /// <summary>
        /// Two 8-bit components, unsigned integer values that get converted to floating-point in the range [0f,255f]
        /// </summary>
        RG8us = 18,
        /// <summary>
        /// Two 8-bit components, unsigned integer values that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        RG8s = 19,
        /// <summary>
        /// Two 8-bit components, unsigned integer values in the range [0,255]
        /// </summary>
        RG8ui = 20,
        /// <summary>
        /// Two 8-bit components, signed integer values in the range [-128,127]
        /// </summary>
        RG8i = 21,
        /// <summary>
        /// Two 8-bit components, unsigned normalized values in sRGB nonlinear encoding
        /// </summary>
        RG8srgb = 22,

        /// <summary>
        /// Three 8-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RGB8un = 23,
        /// <summary>
        /// Three 8-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RGB8n = 24,
        /// <summary>
        /// Three 8-bit components, unsigned integer values that get converted to floating-point in the range [0f,255f]
        /// </summary>
        RGB8us = 25,
        /// <summary>
        /// Three 8-bit components, unsigned integer values that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        RGB8s = 26,
        /// <summary>
        /// Three 8-bit components, unsigned integer values in the range [0,255]
        /// </summary>
        RGB8ui = 27,
        /// <summary>
        /// Three 8-bit components, signed integer values in the range [-128,127]
        /// </summary>
        RGB8i = 28,
        /// <summary>
        /// Three 8-bit components, unsigned normalized values in sRGB nonlinear encoding
        /// </summary>
        RGB8srgb = 29,
        /// <summary>
        /// Three 8-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        BGR8un = 30,
        /// <summary>
        /// Three 8-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        BGR8n = 31,
        /// <summary>
        /// Three 8-bit components, unsigned integer values that get converted to floating-point in the range [0f,255f]
        /// </summary>
        BGR8us = 32,
        /// <summary>
        /// Three 8-bit components, unsigned integer values that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        BGR8s = 33,
        /// <summary>
        /// Three 8-bit components, unsigned integer values in the range [0,255]
        /// </summary>
        BGR8ui = 34,
        /// <summary>
        /// Three 8-bit components, signed integer values in the range [-128,127]
        /// </summary>
        BGR8i = 35,
        /// <summary>
        /// Three 8-bit components, unsigned normalized values in sRGB nonlinear encoding
        /// </summary>
        BGR8srgb = 36,


        /// <summary>
        /// Four 8-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RGBA8un = 37,
        /// <summary>
        /// Four 8-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RGBA8n = 38,
        /// <summary>
        /// Four 8-bit components, unsigned integer values that get converted to floating-point in the range [0f,255f]
        /// </summary>
        RGBA8us = 39,
        /// <summary>
        /// Four 8-bit components, unsigned integer values that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        RGBA8s = 40,
        /// <summary>
        /// Four 8-bit components, unsigned integer values in the range [0,255]
        /// </summary>
        RGBA8ui = 41,
        /// <summary>
        /// Four 8-bit components, signed integer values in the range [-128,127]
        /// </summary>
        RGBA8i = 42,
        /// <summary>
        /// Four 8-bit components, unsigned normalized values, R,G,B components stored in sRGB nonlinear encoding, A component stored in linear encoding
        /// </summary>
        RGBA8srgb = 43,
        /// <summary>
        /// Four 8-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        BGRA8un = 44,
        /// <summary>
        /// Four 8-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        BGRA8n = 45,
        /// <summary>
        /// Four 8-bit components, unsigned integer values that get converted to floating-point in the range [0f,255f]
        /// </summary>
        BGRA8us = 46,
        /// <summary>
        /// Four 8-bit components, unsigned integer values that get converted to floating-point in the range [-128f,127f]
        /// </summary>
        BGRA8s = 47,
        /// <summary>
        /// Four 8-bit components, unsigned integer values in the range [0,255]
        /// </summary>
        BGRA8ui = 48,
        /// <summary>
        /// Four 8-bit components, signed integer values in the range [-128,127]
        /// </summary>
        BGRA8i = 49,
        /// <summary>
        /// Four 8-bit components, unsigned normalized values, B,G,R components stored in sRGB nonlinear encoding, A component stored in linear encoding
        /// </summary>
        BGRA8srgb = 50,

        //A8b8g8r8UnormPack32 = 51,
        //A8b8g8r8SnormPack32 = 52,
        //A8b8g8r8UscaledPack32 = 53,
        //A8b8g8r8SscaledPack32 = 54,
        //A8b8g8r8UintPack32 = 55,
        //A8b8g8r8SintPack32 = 56,
        //A8b8g8r8SrgbPack32 = 57,
        //A2r10g10b10UnormPack32 = 58,
        //A2r10g10b10SnormPack32 = 59,
        //A2r10g10b10UscaledPack32 = 60,
        //A2r10g10b10SscaledPack32 = 61,
        //A2r10g10b10UintPack32 = 62,
        //A2r10g10b10SintPack32 = 63,
        //A2b10g10r10UnormPack32 = 64,
        //A2b10g10r10SnormPack32 = 65,
        //A2b10g10r10UscaledPack32 = 66,
        //A2b10g10r10SscaledPack32 = 67,
        //A2b10g10r10UintPack32 = 68,
        //A2b10g10r10SintPack32 = 69,




        /// <summary>
        /// One 16-bit component, unsigned normalized value in the range [0,1]
        /// </summary>
        R16un = 70,
        /// <summary>
        /// One 16-bit component, signed normalized value in the range [-1,1]
        /// </summary>
        R16n = 71,
        /// <summary>
        /// One 16-bit component, unsigned integer value that get converted to floating-point in the range [0f,65535f]
        /// </summary>
        R16us = 72,
        /// <summary>
        /// One 16-bit component, unsigned integer value that get converted to floating-point in the range [-32768f,32767f]
        /// </summary>
        R16s = 73,
        /// <summary>
        /// One 16-bit component, unsigned integer value in the range [0,65535]
        /// </summary>
        R16ui = 74,
        /// <summary>
        /// One 16-bit component, signed integer value in the range [-32768,32767]
        /// </summary>
        R16i = 75,
        /// <summary>
        /// One 16-bit component, signed floating-point value
        /// </summary>
        R16f = 76,


        /// <summary>
        /// Two 16-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RG16un = 77,
        /// <summary>
        /// Two 16-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RG16n = 78,
        /// <summary>
        /// Two 16-bit components, unsigned integer values that get converted to floating-point in the range [0f,65535f]
        /// </summary>
        RG16us = 79,
        /// <summary>
        /// Two 16-bit components, unsigned integer values that get converted to floating-point in the range [-32768f,32767f]
        /// </summary>
        RG16s = 80,
        /// <summary>
        /// Two 16-bit components, unsigned integer values in the range [0,65535]
        /// </summary>
        RG16ui = 81,
        /// <summary>
        /// Two 16-bit components, signed integer values in the range [-32768,32767]
        /// </summary>
        RG16i = 82,
        /// <summary>
        /// Two 16-bit components, signed floating-point values
        /// </summary>
        RG16f = 83,


        /// <summary>
        /// Three 16-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RGB16un = 84,
        /// <summary>
        /// Three 16-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RGB16n = 85,
        /// <summary>
        /// Three 16-bit components, unsigned integer values that get converted to floating-point in the range [0f,65535f]
        /// </summary>
        RGB16us = 86,
        /// <summary>
        /// Three 16-bit components, unsigned integer values that get converted to floating-point in the range [-32768f,32767f]
        /// </summary>
        RGB16s = 87,
        /// <summary>
        /// Three 16-bit components, unsigned integer values in the range [0,65535]
        /// </summary>
        RGB16ui = 88,
        /// <summary>
        /// Three 16-bit components, signed integer values in the range [-32768,32767]
        /// </summary>
        RGB16i = 89,
        /// <summary>
        /// Three 16-bit components, signed floating-point values
        /// </summary>
        RGB16f = 90,


        /// <summary>
        /// Four 16-bit components, unsigned normalized values in the range [0,1]
        /// </summary>
        RGBA16un = 91,
        /// <summary>
        /// Four 16-bit components, signed normalized values in the range [-1,1]
        /// </summary>
        RGBA16n = 92,
        /// <summary>
        /// Four 16-bit components, unsigned integer values that get converted to floating-point in the range [0f,65535f]
        /// </summary>
        RGBA16us = 93,
        /// <summary>
        /// Four 16-bit components, unsigned integer values that get converted to floating-point in the range [-32768f,32767f]
        /// </summary>
        RGBA16s = 94,
        /// <summary>
        /// Four 16-bit components, unsigned integer values in the range [0,65535]
        /// </summary>
        RGBA16ui = 95,
        /// <summary>
        /// Four 16-bit components, signed integer values in the range [-32768,32767]
        /// </summary>
        RGBA16i = 96,
        /// <summary>
        /// Four 16-bit components, signed floating-point values
        /// </summary>
        RGBA16f = 97,




        /// <summary>
        /// One 32-bit component, unsigned integer value in the range [0,4294967295]
        /// </summary>
        R32ui = 98,
        /// <summary>
        /// One 32-bit component, signed integer value in the range [-2147483648,2147483647]
        /// </summary>
        R32i = 99,
        /// <summary>
        /// One 32-bit component, signed floating-point value
        /// </summary>
        R32f = 100,

        /// <summary>
        /// Two 32-bit components, unsigned integer values in the range [0,4294967295]
        /// </summary>
        RG32ui = 101,
        /// <summary>
        /// Two 32-bit components, signed integer values in the range [-2147483648,2147483647]
        /// </summary>
        RG32i = 102,
        /// <summary>
        /// Two 32-bit components, signed floating-point values
        /// </summary>
        RG32f = 103,

        /// <summary>
        /// Three 32-bit components, unsigned integer values in the range [0,4294967295]
        /// </summary>
        RGB32ui = 104,
        /// <summary>
        /// Three 32-bit components, signed integer values in the range [-2147483648,2147483647]
        /// </summary>
        RGB32i = 105,
        /// <summary>
        /// Three 32-bit components, signed floating-point values
        /// </summary>
        RGB32f = 106,

        /// <summary>
        /// Four 32-bit components, unsigned integer values in the range [0,4294967295]
        /// </summary>
        RGBA32ui = 107,
        /// <summary>
        /// Four 32-bit components, signed integer values in the range [-2147483648,2147483647]
        /// </summary>
        RGBA32i = 108,
        /// <summary>
        /// Four 32-bit components, signed floating-point values
        /// </summary>
        RGBA32f = 109,




        /// <summary>
        /// One 64-bit component, unsigned integer value in the range [0,18446744073709551615]
        /// </summary>
        R64ui = 110,
        /// <summary>
        /// One 64-bit component, signed integer value in the range [-9223372036854775808,9223372036854775807]
        /// </summary>
        R64i = 111,
        /// <summary>
        /// One 64-bit component, signed floating-point value
        /// </summary>
        R64f = 112,

        /// <summary>
        /// Two 64-bit components, unsigned integer values in the range [0,18446744073709551615]
        /// </summary>
        RG64ui = 113,
        /// <summary>
        /// Two 64-bit components, signed integer values in the range [-9223372036854775808,9223372036854775807]
        /// </summary>
        RG64i = 114,
        /// <summary>
        /// Two 64-bit components, signed floating-point values
        /// </summary>
        RG64f = 115,

        /// <summary>
        /// Three 64-bit components, unsigned integer values in the range [0,18446744073709551615]
        /// </summary>
        RGB64ui = 116,
        /// <summary>
        /// Three 64-bit components, signed integer values in the range [-9223372036854775808,9223372036854775807]
        /// </summary>
        RGB64i = 117,
        /// <summary>
        /// Three 64-bit components, signed floating-point values
        /// </summary>
        RGB64f = 118,

        /// <summary>
        /// Four 64-bit components, unsigned integer values in the range [0,18446744073709551615]
        /// </summary>
        RGBA64ui = 119,
        /// <summary>
        /// Four 64-bit components, signed integer values in the range [-9223372036854775808,9223372036854775807]
        /// </summary>
        RGBA64i = 120,
        /// <summary>
        /// Four 64-bit components, signed floating-point values
        /// </summary>
        RGBA64f = 121,

        //B10g11r11UfloatPack32 = 122,
        //E5b9g9r9UfloatPack32 = 123,


        /// <summary>
        /// One 16-bit component, unsigned normalized depth value
        /// </summary>
        Depth16un = 124,
        /// <summary>
        /// One component, 24-bit unsigned normalized depth value (remaining 8 bits are optionally unused)
        /// </summary>
        Depth24un = 125,
        /// <summary>
        /// One 32-bit component, signed floating-point depth value
        /// </summary>
        Depth32f = 126,
        /// <summary>
        /// One 8-bit component, unsigned integer stencil value
        /// </summary>
        Stencil8ui = 127,
        /// <summary>
        /// Two components, 16-bit unsigned normalized depth and 8-bit unsigned integer stencil values
        /// </summary>
        Depth16un_Stencil8ui = 128,
        /// <summary>
        /// Two components (32-bit packed), 24-bit unsigned normalized depth and 8-bit unsigned integer stencil values
        /// </summary>
        Depth24un_Stencil8ui = 129,
        /// <summary>
        /// Two components, 32-bit signed floating-point depth and 8-bit unsigned integer stencil values (remaining 24 bits are optionally unused)
        /// </summary>
        Depth32f_Stencil8ui = 130,


        //Bc1RgbUnormBlock = 131,
        //Bc1RgbSrgbBlock = 132,
        //Bc1RgbaUnormBlock = 133,
        //Bc1RgbaSrgbBlock = 134,
        //Bc2UnormBlock = 135,
        //Bc2SrgbBlock = 136,
        //Bc3UnormBlock = 137,
        //Bc3SrgbBlock = 138,
        //Bc4UnormBlock = 139,
        //Bc4SnormBlock = 140,
        //Bc5UnormBlock = 141,
        //Bc5SnormBlock = 142,
        //Bc6hUfloatBlock = 143,
        //Bc6hSfloatBlock = 144,
        //Bc7UnormBlock = 145,
        //Bc7SrgbBlock = 146,
        //Etc2R8g8b8UnormBlock = 147,
        //Etc2R8g8b8SrgbBlock = 148,
        //Etc2R8g8b8a1UnormBlock = 149,
        //Etc2R8g8b8a1SrgbBlock = 150,
        //Etc2R8g8b8a8UnormBlock = 151,
        //Etc2R8g8b8a8SrgbBlock = 152,
        //EacR11UnormBlock = 153,
        //EacR11SnormBlock = 154,
        //EacR11g11UnormBlock = 155,
        //EacR11g11SnormBlock = 156,
        //Astc4x4UnormBlock = 157,
        //Astc4x4SrgbBlock = 158,
        //Astc5x4UnormBlock = 159,
        //Astc5x4SrgbBlock = 160,
        //Astc5x5UnormBlock = 161,
        //Astc5x5SrgbBlock = 162,
        //Astc6x5UnormBlock = 163,
        //Astc6x5SrgbBlock = 164,
        //Astc6x6UnormBlock = 165,
        //Astc6x6SrgbBlock = 166,
        //Astc8x5UnormBlock = 167,
        //Astc8x5SrgbBlock = 168,
        //Astc8x6UnormBlock = 169,
        //Astc8x6SrgbBlock = 170,
        //Astc8x8UnormBlock = 171,
        //Astc8x8SrgbBlock = 172,
        //Astc10x5UnormBlock = 173,
        //Astc10x5SrgbBlock = 174,
        //Astc10x6UnormBlock = 175,
        //Astc10x6SrgbBlock = 176,
        //Astc10x8UnormBlock = 177,
        //Astc10x8SrgbBlock = 178,
        //Astc10x10UnormBlock = 179,
        //Astc10x10SrgbBlock = 180,
        //Astc12x10UnormBlock = 181,
        //Astc12x10SrgbBlock = 182,
        //Astc12x12UnormBlock = 183,
        //Astc12x12SrgbBlock = 184,
        //Pvrtc12bppUnormBlockIMG = 1000054000,
        //Pvrtc14bppUnormBlockIMG = 1000054001,
        //Pvrtc22bppUnormBlockIMG = 1000054002,
        //Pvrtc24bppUnormBlockIMG = 1000054003,
        //Pvrtc12bppSrgbBlockIMG = 1000054004,
        //Pvrtc14bppSrgbBlockIMG = 1000054005,
        //Pvrtc22bppSrgbBlockIMG = 1000054006,
        //Pvrtc24bppSrgbBlockIMG = 1000054007,
        //Astc4x4SfloatBlockEXT = 1000066000,
        //Astc5x4SfloatBlockEXT = 1000066001,
        //Astc5x5SfloatBlockEXT = 1000066002,
        //Astc6x5SfloatBlockEXT = 1000066003,
        //Astc6x6SfloatBlockEXT = 1000066004,
        //Astc8x5SfloatBlockEXT = 1000066005,
        //Astc8x6SfloatBlockEXT = 1000066006,
        //Astc8x8SfloatBlockEXT = 1000066007,
        //Astc10x5SfloatBlockEXT = 1000066008,
        //Astc10x6SfloatBlockEXT = 1000066009,
        //Astc10x8SfloatBlockEXT = 1000066010,
        //Astc10x10SfloatBlockEXT = 1000066011,
        //Astc12x10SfloatBlockEXT = 1000066012,
        //Astc12x12SfloatBlockEXT = 1000066013,
        //G8b8g8r8422Unorm = 1000156000,
        //G8b8g8r8422UnormKHR = 1000156000,
        //B8g8r8g8422Unorm = 1000156001,
        //B8g8r8g8422UnormKHR = 1000156001,
        //G8B8R83plane420Unorm = 1000156002,
        //G8B8R83plane420UnormKHR = 1000156002,
        //G8B8r82plane420Unorm = 1000156003,
        //G8B8r82plane420UnormKHR = 1000156003,
        //G8B8R83plane422Unorm = 1000156004,
        //G8B8R83plane422UnormKHR = 1000156004,
        //G8B8r82plane422Unorm = 1000156005,
        //G8B8r82plane422UnormKHR = 1000156005,
        //G8B8R83plane444Unorm = 1000156006,
        //G8B8R83plane444UnormKHR = 1000156006,
        //R10x6UnormPack16 = 1000156007,
        //R10x6UnormPack16KHR = 1000156007,
        //R10x6g10x6Unorm2pack16 = 1000156008,
        //R10x6g10x6Unorm2pack16KHR = 1000156008,
        //R10x6g10x6b10x6a10x6Unorm4pack16 = 1000156009,
        //R10x6g10x6b10x6a10x6Unorm4pack16KHR = 1000156009,
        //G10x6b10x6g10x6r10x6422Unorm4pack16 = 1000156010,
        //G10x6b10x6g10x6r10x6422Unorm4pack16KHR = 1000156010,
        //B10x6g10x6r10x6g10x6422Unorm4pack16 = 1000156011,
        //B10x6g10x6r10x6g10x6422Unorm4pack16KHR = 1000156011,
        //G10x6B10x6R10x63plane420Unorm3pack16 = 1000156012,
        //G10x6B10x6R10x63plane420Unorm3pack16KHR = 1000156012,
        //G10x6B10x6r10x62plane420Unorm3pack16 = 1000156013,
        //G10x6B10x6r10x62plane420Unorm3pack16KHR = 1000156013,
        //G10x6B10x6R10x63plane422Unorm3pack16 = 1000156014,
        //G10x6B10x6R10x63plane422Unorm3pack16KHR = 1000156014,
        //G10x6B10x6r10x62plane422Unorm3pack16 = 1000156015,
        //G10x6B10x6r10x62plane422Unorm3pack16KHR = 1000156015,
        //G10x6B10x6R10x63plane444Unorm3pack16 = 1000156016,
        //G10x6B10x6R10x63plane444Unorm3pack16KHR = 1000156016,
        //R12x4UnormPack16 = 1000156017,
        //R12x4UnormPack16KHR = 1000156017,
        //R12x4g12x4Unorm2pack16 = 1000156018,
        //R12x4g12x4Unorm2pack16KHR = 1000156018,
        //R12x4g12x4b12x4a12x4Unorm4pack16 = 1000156019,
        //R12x4g12x4b12x4a12x4Unorm4pack16KHR = 1000156019,
        //G12x4b12x4g12x4r12x4422Unorm4pack16 = 1000156020,
        //G12x4b12x4g12x4r12x4422Unorm4pack16KHR = 1000156020,
        //B12x4g12x4r12x4g12x4422Unorm4pack16 = 1000156021,
        //B12x4g12x4r12x4g12x4422Unorm4pack16KHR = 1000156021,
        //G12x4B12x4R12x43plane420Unorm3pack16 = 1000156022,
        //G12x4B12x4R12x43plane420Unorm3pack16KHR = 1000156022,
        //G12x4B12x4r12x42plane420Unorm3pack16 = 1000156023,
        //G12x4B12x4r12x42plane420Unorm3pack16KHR = 1000156023,
        //G12x4B12x4R12x43plane422Unorm3pack16 = 1000156024,
        //G12x4B12x4R12x43plane422Unorm3pack16KHR = 1000156024,
        //G12x4B12x4r12x42plane422Unorm3pack16 = 1000156025,
        //G12x4B12x4r12x42plane422Unorm3pack16KHR = 1000156025,
        //G12x4B12x4R12x43plane444Unorm3pack16 = 1000156026,
        //G12x4B12x4R12x43plane444Unorm3pack16KHR = 1000156026,
        //G16b16g16r16422Unorm = 1000156027,
        //G16b16g16r16422UnormKHR = 1000156027,
        //B16g16r16g16422Unorm = 1000156028,
        //B16g16r16g16422UnormKHR = 1000156028,
        //G16B16R163plane420Unorm = 1000156029,
        //G16B16R163plane420UnormKHR = 1000156029,
        //G16B16r162plane420Unorm = 1000156030,
        //G16B16r162plane420UnormKHR = 1000156030,
        //G16B16R163plane422Unorm = 1000156031,
        //G16B16R163plane422UnormKHR = 1000156031,
        //G16B16r162plane422Unorm = 1000156032,
        //G16B16r162plane422UnormKHR = 1000156032,
        //G16B16R163plane444Unorm = 1000156033,
        //G16B16R163plane444UnormKHR = 1000156033
    }

}
