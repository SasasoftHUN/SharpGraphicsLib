using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{

    internal interface IVulkanMappableDataBuffer : IVulkanDataBuffer, IMappableDataBuffer
    {
        void BufferData(IntPtr data, ulong size, ulong offset);
    }
    internal interface IVulkanMappableDataBuffer<T> : IVulkanDataBuffer<T>, IMappableDataBuffer<T>, IVulkanMappableDataBuffer where T : unmanaged
    {
        void BufferData(ref T data, uint elementIndexOffset);
        void BufferData(in ReadOnlySpan<T> data, uint elementIndexOffset);
        void BufferData(in ReadOnlyMemory<T> data, uint elementIndexOffset);
    }

    internal class VulkanMappableDataBuffer<T> : VulkanDataBuffer<T>, IVulkanMappableDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private IntPtr _mappedMemory;
        private ulong _mappedMemoryOffset;
        private ulong _mappedMemorySize;

        private bool _isDisposed;

        #endregion

        #region Properties

        public bool IsMapped => _mappedMemory != IntPtr.Zero;
        public IntPtr MappedPointer => _mappedMemory;
        public unsafe void* MappedRawPointer => _mappedMemory.ToPointer();

        public unsafe Span<T> MappedUnAlignedElements => new Span<T>(_mappedMemory.ToPointer(), (int)(_mappedMemorySize / _dataTypeSize));
        public unsafe ReadOnlySpan<T> MappedReadOnlyUnAlignedElements => new ReadOnlySpan<T>(_mappedMemory.ToPointer(), (int)(_mappedMemorySize / _dataTypeSize));

        #endregion

        #region Constructors

        protected internal VulkanMappableDataBuffer(VulkanGraphicsDevice device, uint dataCapacity, DataBufferType type, DataBufferType alignmentType, Vk.VkMemoryPropertyFlags memoryProperty) :
            base(device, dataCapacity, type, alignmentType, type.ToVkBufferUsageFlags(false), memoryProperty)
        {
            _isDisposed = false;

            _mappedMemory = IntPtr.Zero;
        }

        ~VulkanMappableDataBuffer() => Dispose(false);

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CalculateMappingRange(ulong offset, ulong size, ulong maxSize, out ulong mapOffset, out ulong mapSize)
        {
            ulong atomSize = _device.VkLimits.limits.nonCoherentAtomSize;

            mapOffset = (ulong)(MathF.Floor(offset / (float)atomSize)) * atomSize;
            mapSize = (ulong)(MathF.Ceiling(size / (float)atomSize)) * atomSize;

            if (mapSize + mapOffset >= maxSize)
                mapSize = VK.WholeSize;
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing VulkanMappableDataBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    UnMapMemory();
                else Debug.WriteLine($"Warning: VulkanMappableDataBuffer<{typeof(T).FullName}> cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public void BufferData(IntPtr data, ulong size, ulong offset)
        {
            Debug.Assert(_type.HasFlag(DataBufferType.Store), $"VulkanMappableDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
            AssertStoreData(size + offset);

            IntPtr mappedMemory = MapMemory(offset, size);

            unsafe { Buffer.MemoryCopy(data.ToPointer(), mappedMemory.ToPointer(), size, size); }

            FlushMappedSystemMemory(offset, size);
            UnMapMemory();
        }
        public void BufferData(ref T data, uint elementIndexOffset)
        {
            Debug.Assert(_type.HasFlag(DataBufferType.Store), $"VulkanMappableDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
            AssertStoreDataElements(1u + elementIndexOffset);

            int offset = (int)(elementIndexOffset * _elementOffset);
            IntPtr mappedMemory = MapMemory((ulong)offset, _dataTypeSize);

            Marshal.StructureToPtr(data, mappedMemory + offset, false);
            FlushMappedSystemMemory((ulong)offset, _dataTypeSize);

            UnMapMemory();
        }
        public void BufferData(in ReadOnlySpan<T> data, uint elementIndexOffset)
        {
            Debug.Assert(_type.HasFlag(DataBufferType.Store), $"VulkanMappableDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
            AssertStoreDataElements((uint)data.Length + elementIndexOffset);

            ulong offset = _elementOffset * (ulong)elementIndexOffset;
            ulong size = _elementOffset * (ulong)data.Length;
            IntPtr mappedMemory = MapMemory(offset, size);

            if (_dataTypeSize != _elementOffset)
            {
                mappedMemory.CopyWithAlignment(data, (int)_elementOffset, (int)_dataTypeSize);
                FlushMappedSystemMemory(offset, size);
            }
            else
            {
                unsafe
                {
                    fixed (T* ptr = data)
                        Buffer.MemoryCopy(ptr, mappedMemory.ToPointer(), size, size);
                }
            }

            FlushMappedSystemMemory(offset, size);
            UnMapMemory();
        }
        public void BufferData(in ReadOnlyMemory<T> data, uint elementIndexOffset)
        {
            Debug.Assert(_type.HasFlag(DataBufferType.Store), $"VulkanMappableDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
            AssertStoreDataElements((uint)data.Length + elementIndexOffset);

            ulong offset = _elementOffset * (ulong)elementIndexOffset;
            ulong size = _elementOffset * (ulong)data.Length;
            IntPtr mappedMemory = MapMemory(offset, size);

            if (_dataTypeSize != _elementOffset)
            {
                mappedMemory.CopyWithAlignment(data, (int)_elementOffset, (int)_dataTypeSize);
                FlushMappedSystemMemory(offset, size);
            }
            else
            {
                unsafe
                {
                    using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
                    Buffer.MemoryCopy(pinnedData.RawPointer, mappedMemory.ToPointer(), size, size);
                }
            }

            FlushMappedSystemMemory(offset, size);
            UnMapMemory();
        }

        public override void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul) => BufferData(data, size, offset);
        public override void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u) => BufferData(ref data, elementIndexOffset);
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u) => BufferData(data, elementIndexOffset);
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u) => BufferData(data, elementIndexOffset);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr MapMemory() => MapMemory(0ul, _memoryRequirements.size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr MapMemory(ulong offset, ulong size)
        {
#if DEBUG
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
            Debug.Assert(_mappedMemory == IntPtr.Zero, $"Trying to MapMemory VulkanMappableDataBuffer<{typeof(T).FullName}> which is already Mapped!");
#endif

            CalculateMappingRange(offset, size, _memoryRequirements.size, out ulong mapOffset, out ulong mapSize);
            VK.vkMapMemory(_device.Device, _memory, mapOffset, mapSize, 0u, ref _mappedMemory);
            _mappedMemoryOffset = mapOffset;
            _mappedMemorySize = mapSize == VK.WholeSize ? _memoryRequirements.size : mapSize;

            if (offset != mapOffset)
                _mappedMemory += (int)(offset - mapOffset);

            return _mappedMemory;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr MapMemoryElements(uint elementIndexOffset, uint elementCount) => MapMemory(elementIndexOffset * _elementOffset, elementCount * _elementOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedSystemMemory() => FlushMappedSystemMemory(_mappedMemoryOffset, _mappedMemorySize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedSystemMemory(ulong offset, ulong size)
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to FlushMappedSystemMemory of VulkanMappableDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif

            CalculateMappingRange(offset, size, _mappedMemorySize, out ulong mapOffset, out ulong mapSize);
            Vk.VkMappedMemoryRange flushRange = new Vk.VkMappedMemoryRange()
            {
                sType = Vk.VkStructureType.MappedMemoryRange,
                memory = _memory,
                offset = mapOffset,
                size = mapSize,
            };
            VK.vkFlushMappedMemoryRanges(_device.Device, 1u, ref flushRange);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedDeviceMemory() => FlushMappedDeviceMemory(_mappedMemoryOffset, _mappedMemorySize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedDeviceMemory(ulong offset, ulong size)
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to FlushMappedDeviceMemory of VulkanMappableDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif

            CalculateMappingRange(offset, size, _mappedMemorySize, out ulong mapOffset, out ulong mapSize);
            Vk.VkMappedMemoryRange flushRange = new Vk.VkMappedMemoryRange()
            {
                sType = Vk.VkStructureType.MappedMemoryRange,
                memory = _memory,
                offset = mapOffset,
                size = mapSize,
            };
            VK.vkInvalidateMappedMemoryRanges(_device.Device, 1u, ref flushRange);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedSystemMemoryElements(uint elementIndexOffset, uint elementCount) => FlushMappedSystemMemory(elementIndexOffset * _elementOffset, elementCount * _elementOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedDeviceMemoryElements(uint elementIndexOffset, uint elementCount) => FlushMappedDeviceMemory(elementIndexOffset * _elementOffset, elementCount * _elementOffset);

        public T[] GatherMappedDataElements()
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to Gather from VulkanMappableDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif
            return _mappedMemory.GatherAlignedElements<T>((int)_capacity, (int)_elementOffset, (int)_dataTypeSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnMapMemory()
        {
            if (_mappedMemory != IntPtr.Zero)
            {
                VK.vkUnmapMemory(_device.Device, _memory);
                _mappedMemory = IntPtr.Zero;
            }
        }

        #endregion

    }
}
