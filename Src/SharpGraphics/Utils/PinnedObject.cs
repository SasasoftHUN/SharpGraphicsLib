using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly ref struct PinnedObject<T> where T : unmanaged
    {

        private readonly T _obj;
        private readonly GCHandle _handle;

        public readonly IntPtr pointer;

        public unsafe void* RawPointer => pointer.ToPointer();

        public PinnedObject(in T obj)
        {
            _obj = obj;
            _handle = GCHandle.Alloc(_obj, GCHandleType.Pinned);
            pointer = _handle.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
                _handle.Free();
        }

    }

    public readonly ref struct PinnedObjectReference<T> where T : unmanaged
    {

        private readonly MemoryHandle _handle;

        public readonly IntPtr pointer;

        public unsafe void* RawPointer => pointer.ToPointer();

        public PinnedObjectReference(ref T obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            pointer = handle.AddrOfPinnedObject();
            unsafe { _handle = new MemoryHandle(pointer.ToPointer(), handle); }
        }
        public PinnedObjectReference(T[] obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            pointer = handle.AddrOfPinnedObject();
            unsafe { _handle = new MemoryHandle(pointer.ToPointer(), handle); }
        }
        public PinnedObjectReference(in Memory<T> obj)
        {
            _handle = obj.Pin();
            unsafe { pointer = new IntPtr(_handle.Pointer); }
        }
        public PinnedObjectReference(in ReadOnlyMemory<T> obj)
        {
            _handle = obj.Pin();
            unsafe { pointer = new IntPtr(_handle.Pointer); }
        }

        public void Dispose() => _handle.Dispose();

    }

    public readonly struct PinnedObjectHandle<T> where T : unmanaged
    {

        private readonly MemoryHandle _handle;

        public readonly IntPtr pointer;

        public unsafe void* RawPointer => pointer.ToPointer();

        public PinnedObjectHandle(ref T obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            pointer = handle.AddrOfPinnedObject();
            unsafe { _handle = new MemoryHandle(pointer.ToPointer(), handle); }
        }
        public PinnedObjectHandle(T[] obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            pointer = handle.AddrOfPinnedObject();
            unsafe { _handle = new MemoryHandle(pointer.ToPointer(), handle); }
        }
        public PinnedObjectHandle(in Memory<T> obj)
        {
            _handle = obj.Pin();
            unsafe { pointer = new IntPtr(_handle.Pointer); }
        }
        public PinnedObjectHandle(in ReadOnlyMemory<T> obj)
        {
            _handle = obj.Pin();
            unsafe { pointer = new IntPtr(_handle.Pointer); }
        }

        public void Dispose() => _handle.Dispose();

    }
}
