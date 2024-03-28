using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Allocator
{

    public readonly struct MemoryAllocation
    {
        public readonly IMemoryAllocator allocator;
        public readonly int bank;
        public readonly IntPtr address;
        public readonly int size;

        internal MemoryAllocation(IMemoryAllocator allocator, int bank, IntPtr address, int size)
        {
            this.allocator = allocator;
            this.bank = bank;
            this.address = address;
            this.size = size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> Span<T>() => new Span<T>(address.ToPointer(), size / Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> Span<T>(int length) => new Span<T>(address.ToPointer(), length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan<T> ReadOnlySpan<T>() => new ReadOnlySpan<T>(address.ToPointer(), size / Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan<T> ReadOnlySpan<T>(int length) => new ReadOnlySpan<T>(address.ToPointer(), length);

        public void Release() => allocator.Release(this);

        public static implicit operator IntPtr(in MemoryAllocation allocation) => allocation.address;

    }

    public interface IMemoryAllocator : IDisposable
    {

        MemoryAllocation Allocate(int size);
        MemoryAllocation Allocate(uint size);
        MemoryAllocation Allocate(long size);
        MemoryAllocation Allocate(ulong size);

        MemoryAllocation AllocateThenCopy(IntPtr ptr, int size);
        MemoryAllocation AllocateThenCopy(IntPtr ptr, uint size);
        MemoryAllocation AllocateThenCopy(IntPtr ptr, long size);
        MemoryAllocation AllocateThenCopy(IntPtr ptr, ulong size);

        MemoryAllocation AllocateThenCopy<T>(in Span<T> data) where T : unmanaged;
        MemoryAllocation AllocateThenCopy<T>(in ReadOnlySpan<T> data) where T : unmanaged;
        MemoryAllocation AllocateThenCopy<T>(in Memory<T> data) where T : unmanaged;
        MemoryAllocation AllocateThenCopy<T>(in ReadOnlyMemory<T> data) where T : unmanaged;

        void Release(in MemoryAllocation allocation);
        void ReleaseAll();
    }

    public abstract class MemoryAllocatorBase : IMemoryAllocator
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Constructors

        ~MemoryAllocatorBase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                ReleaseAll();
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation Allocate(uint size) => Allocate((int)size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation Allocate(long size) => Allocate((int)size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation Allocate(ulong size) => Allocate((int)size);
        public abstract MemoryAllocation Allocate(int size);

        public unsafe virtual MemoryAllocation AllocateThenCopy(IntPtr ptr, int size)
        {
            MemoryAllocation allocation = Allocate(size);
            Buffer.MemoryCopy(ptr.ToPointer(), allocation.address.ToPointer(), allocation.size, allocation.size);
            return allocation;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation AllocateThenCopy(IntPtr ptr, uint size) => AllocateThenCopy(ptr, (int)size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation AllocateThenCopy(IntPtr ptr, long size) => AllocateThenCopy(ptr, (int)size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryAllocation AllocateThenCopy(IntPtr ptr, ulong size) => AllocateThenCopy(ptr, (int)size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe virtual MemoryAllocation AllocateThenCopy<T>(in Span<T> data) where T : unmanaged
        {
            MemoryAllocation allocation = Allocate(Marshal.SizeOf<T>() * data.Length);
            fixed (T* ptr = data)
                Buffer.MemoryCopy(ptr, allocation.address.ToPointer(), allocation.size, allocation.size);
            return allocation;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe virtual MemoryAllocation AllocateThenCopy<T>(in ReadOnlySpan<T> data) where T : unmanaged
        {
            MemoryAllocation allocation = Allocate(Marshal.SizeOf<T>() * data.Length);
            fixed (T* ptr = data)
                Buffer.MemoryCopy(ptr, allocation.address.ToPointer(), allocation.size, allocation.size);
            return allocation;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe virtual MemoryAllocation AllocateThenCopy<T>(in Memory<T> data) where T : unmanaged
        {
            MemoryAllocation allocation = Allocate(Marshal.SizeOf<T>() * data.Length);
            using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
            Buffer.MemoryCopy(pinnedData.RawPointer, allocation.address.ToPointer(), allocation.size, allocation.size);
            return allocation;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe virtual MemoryAllocation AllocateThenCopy<T>(in ReadOnlyMemory<T> data) where T : unmanaged
        {
            MemoryAllocation allocation = Allocate(Marshal.SizeOf<T>() * data.Length);
            using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
            Buffer.MemoryCopy(pinnedData.RawPointer, allocation.address.ToPointer(), allocation.size, allocation.size);
            return allocation;
        }

        public abstract void Release(in MemoryAllocation allocation);
        public abstract void ReleaseAll();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

}
