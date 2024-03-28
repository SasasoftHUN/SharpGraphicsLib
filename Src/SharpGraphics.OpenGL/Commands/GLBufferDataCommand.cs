using SharpGraphics.Allocator;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{

    internal sealed class GLBufferDataPtrCommand : IGLCommand
    {

        private readonly IGLDataBuffer _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IntPtr _data;
        private readonly IntPtr _size;

        internal GLBufferDataPtrCommand(IGLDataBuffer dataBuffer, IntPtr offset, IMemoryAllocator allocator, IntPtr data, ulong size)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _data = allocator.AllocateThenCopy(data, size).address;
            _size = new IntPtr((int)size);
        }

        public void Execute() => _dataBuffer.GLBufferData(_offset, _size, _data);

        public override string ToString() => $"Buffer Data Pointer (Size: {(int)_size}, Offset: {(int)_offset})";

    }

    internal sealed class GLBufferDataFromEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLDataBuffer _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly int _stagingBufferOffset;
        private readonly IntPtr _size;

        internal GLBufferDataFromEmulatedStagingBufferCommand(IGLDataBuffer dataBuffer, IntPtr offset, IGLEmulatedStagingDataBuffer stagingBuffer, int stagingBufferOffset, int size)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _stagingBuffer = stagingBuffer;
            _stagingBufferOffset = stagingBufferOffset;
            _size = new IntPtr(size);
        }

        public void Execute() => _dataBuffer.GLBufferData(_offset, _size, _stagingBuffer.Pointer + _stagingBufferOffset);

        public override string ToString() => $"Buffer Data From Emulated Staging Buffer (Size: {(int)_size}, Destination Offset: {(int)_offset}, Staging Buffer Offset: {_stagingBufferOffset})";

    }

    internal sealed class GLBufferDataSingleCommand<T> : IGLCommand where T : unmanaged
    {

        private readonly IGLDataBuffer<T> _dataBuffer;
        private readonly IntPtr _offset;
        private readonly T _data;

        internal GLBufferDataSingleCommand(IGLDataBuffer<T> dataBuffer, IntPtr offset, in T data)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _data = data;
        }

        public void Execute() => _dataBuffer.GLBufferData(_offset, _data);

        public override string ToString() => $"Buffer Data Single {typeof(T).FullName} (Offset: {(int)_offset})";

    }

    internal sealed class GLBufferDataArrayCommand<T> : IGLCommand where T : unmanaged
    {

        private readonly IGLDataBuffer<T> _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IntPtr _data;
        private readonly IntPtr _size;

        internal GLBufferDataArrayCommand(IGLDataBuffer<T> dataBuffer, IntPtr offset, IMemoryAllocator allocator, in Span<T> data)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            MemoryAllocation allocation = allocator.Allocate(dataBuffer.ElementOffset * data.Length);
            _data = allocation.address;
            _size = new IntPtr(allocation.size);
            if (GLDataBuffer<T>.DataTypeSize == dataBuffer.ElementOffset)
                unsafe
                {
                    fixed (T* ptr = data)
                        Buffer.MemoryCopy(ptr, allocation.address.ToPointer(), allocation.size, allocation.size);
                }
            else allocation.address.CopyWithAlignment(data, (int)dataBuffer.ElementOffset, (int)GLDataBuffer<T>.DataTypeSize);
        }
        internal GLBufferDataArrayCommand(IGLDataBuffer<T> dataBuffer, IntPtr offset, IMemoryAllocator allocator, in ReadOnlySpan<T> data)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            MemoryAllocation allocation = allocator.Allocate(dataBuffer.ElementOffset * data.Length);
            _data = allocation.address;
            _size = new IntPtr(allocation.size);
            if (GLDataBuffer<T>.DataTypeSize == dataBuffer.ElementOffset)
                unsafe
                {
                    fixed (T* ptr = data)
                        Buffer.MemoryCopy(ptr, allocation.address.ToPointer(), allocation.size, allocation.size);
                }
            else allocation.address.CopyWithAlignment(data, (int)dataBuffer.ElementOffset, (int)GLDataBuffer<T>.DataTypeSize);
        }
        internal GLBufferDataArrayCommand(IGLDataBuffer<T> dataBuffer, IntPtr offset, IMemoryAllocator allocator, in Memory<T> data)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            MemoryAllocation allocation = allocator.Allocate(dataBuffer.ElementOffset * data.Length);
            _data = allocation.address;
            _size = new IntPtr(allocation.size);
            if (GLDataBuffer<T>.DataTypeSize == dataBuffer.ElementOffset)
                unsafe
                {
                    using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
                    Buffer.MemoryCopy(pinnedData.RawPointer, allocation.address.ToPointer(), allocation.size, allocation.size);
                }
            else allocation.address.CopyWithAlignment(data, (int)dataBuffer.ElementOffset, (int)GLDataBuffer<T>.DataTypeSize);
        }
        internal GLBufferDataArrayCommand(IGLDataBuffer<T> dataBuffer, IntPtr offset, IMemoryAllocator allocator, in ReadOnlyMemory<T> data)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            MemoryAllocation allocation = allocator.Allocate(dataBuffer.ElementOffset * data.Length);
            _data = allocation.address;
            _size = new IntPtr(allocation.size);
            if (GLDataBuffer<T>.DataTypeSize == dataBuffer.ElementOffset)
                unsafe
                {
                    using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
                    Buffer.MemoryCopy(pinnedData.RawPointer, allocation.address.ToPointer(), allocation.size, allocation.size);
                }
            else allocation.address.CopyWithAlignment(data, (int)dataBuffer.ElementOffset, (int)GLDataBuffer<T>.DataTypeSize);
        }

        public void Execute() => _dataBuffer.GLBufferData(_offset, _size, _data);

        public override string ToString() => $"Buffer Data Array {typeof(T).FullName} (Size: {(int)_size}, Offset: {(int)_offset})";

    }
}
