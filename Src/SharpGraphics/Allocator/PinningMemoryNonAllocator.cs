using SharpGraphics.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Allocator
{
    public sealed class PinningMemoryNonAllocator<MA> : MemoryAllocatorBase where MA : IMemoryAllocator, new()
    {

        #region Fields

        private List<MemoryHandle> _handles = new List<MemoryHandle>();
        private MA _fallbackAllocator;

        #endregion

        #region Constructors

        public PinningMemoryNonAllocator()
        {
            _fallbackAllocator = new MA();
            throw new NotImplementedException(); //TODO: Class is curently implemented incorrectly, do not use
        }

        ~PinningMemoryNonAllocator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MemoryAllocation Allocate(int size) => _fallbackAllocator.Allocate(size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MemoryAllocation AllocateThenCopy(IntPtr ptr, int size) => _fallbackAllocator.AllocateThenCopy(ptr, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MemoryAllocation AllocateThenCopy<T>(in Span<T> data) => _fallbackAllocator.AllocateThenCopy(data);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MemoryAllocation AllocateThenCopy<T>(in ReadOnlySpan<T> data) => _fallbackAllocator.AllocateThenCopy(data);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override MemoryAllocation AllocateThenCopy<T>(in Memory<T> data)
        {
            MemoryHandle handle = data.Pin();
            _handles.Add(handle);
            return new MemoryAllocation(this, _handles.Count - 1, new IntPtr(handle.Pointer), Marshal.SizeOf<T>() * data.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override MemoryAllocation AllocateThenCopy<T>(in ReadOnlyMemory<T> data)
        {
            MemoryHandle handle = data.Pin();
            _handles.Add(handle);
            return new MemoryAllocation(this, _handles.Count - 1, new IntPtr(handle.Pointer), Marshal.SizeOf<T>() * data.Length);
        }

        public override void Release(in MemoryAllocation allocation)
        {
            if (allocation.allocator == this)
                _handles[allocation.bank].Dispose();
            else _fallbackAllocator.Release(allocation);
        }

        public unsafe override void ReleaseAll()
        {
            foreach (MemoryHandle handle in _handles)
                if (handle.Pointer != null)
                    handle.Dispose();
            _handles.Clear();
            _fallbackAllocator.ReleaseAll();
        }

        #endregion

    }
}
