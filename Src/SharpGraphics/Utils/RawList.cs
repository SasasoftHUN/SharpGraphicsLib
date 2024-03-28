using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public sealed class RawList<T> : IList<T>, IDisposable where T : unmanaged
    {

        #region Fields

        public const int DEFAULT_CAPACITY = 4;
        private const float GROWTH_FACTOR = 2f;
        private static readonly int _elementSize;

        private IntPtr _pointer;
        private uint _capacity;
        private uint _count = 0u;

        #endregion

        #region Properties

        public IntPtr Pointer => _pointer;
        public uint Capacity => _capacity;
        public uint Count => _count;
        int ICollection<T>.Count => (int)_count;

        public unsafe ref T this[int index] => ref Unsafe.AsRef<T>((_pointer + index * _elementSize).ToPointer());
        public unsafe ref T this[uint index] => ref Unsafe.AsRef<T>((_pointer + (int)index * _elementSize).ToPointer());
        T IList<T>.this[int index] { get => Marshal.PtrToStructure<T>(_pointer + index * _elementSize); set => Marshal.StructureToPtr(value, _pointer + index * _elementSize, false); }

        public unsafe Span<T> Span => new Span<T>(_pointer.ToPointer(), (int)_count);
        public unsafe ReadOnlySpan<T> ReadOnlySpan => new ReadOnlySpan<T>(_pointer.ToPointer(), (int)_count);

        public bool IsReadOnly => false;
        public bool IsDisposed => _pointer == IntPtr.Zero;

        #endregion

        #region Constructors

        static RawList() => _elementSize = Marshal.SizeOf<T>();

        public RawList() : this(DEFAULT_CAPACITY) { }
        public RawList(int capacity) => Allocate((uint)capacity);
        public RawList(uint capacity) => Allocate(capacity);

        ~RawList() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private void Allocate(uint capacity)
        {
            _pointer = Marshal.AllocHGlobal(new IntPtr(capacity * _elementSize));
            _capacity = capacity;
        }
        private void ReAllocate(uint capacity)
        {
            _pointer = Marshal.ReAllocHGlobal(_pointer, new IntPtr(capacity * _elementSize));
            _capacity = capacity;
        }

        private void Dispose(bool disposing)
        {
            if (_pointer != IntPtr.Zero)
            {
                /*if (disposing)
                {
                    //dispose managed state (managed objects)
                }*/

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                Marshal.FreeHGlobal(_pointer);
                _pointer = IntPtr.Zero;
            }
        }

        #endregion

        #region Public Methods

        public void Add(T item)
        {
            if (_count == _capacity)
                ReAllocate((uint)(_capacity * GROWTH_FACTOR));

            Marshal.StructureToPtr(item, _pointer + (int)_count++ * _elementSize, false);
        }
        public void Add(in T item)
        {
            if (_count == _capacity)
                ReAllocate((uint)(_capacity * GROWTH_FACTOR));

            Marshal.StructureToPtr(item, _pointer + (int)_count++ * _elementSize, false);
        }
        public void Insert(int index, T item)
        {
            if (index == _count)
                Add(item);
            else
            {
                if (_count == _capacity)
                    ReAllocate((uint)(_capacity * GROWTH_FACTOR));

                IntPtr ptr = _pointer + index * _elementSize;
                long moveSize = (_count - index) * _elementSize;
                unsafe
                {
                    Buffer.MemoryCopy(ptr.ToPointer(), (ptr + _elementSize).ToPointer(), moveSize, moveSize);
                }
                Marshal.StructureToPtr(item, ptr, false);
                _count++;
            }
        }

        public bool Contains(T item)
        {
            foreach (T data in ReadOnlySpan)
                if (data.Equals(item))
                    return true;
            return false;
        }
        public int IndexOf(T item)
        {
            ReadOnlySpan<T> span = ReadOnlySpan;
            for (int i = 0; i < span.Length; i++)
                if (span[i].Equals(item))
                    return i;
            return -1;
        }
        public IntPtr PointerOf(T item)
        {
            int index = IndexOf(item);
            return index >= 0 ? _pointer + index * _elementSize : IntPtr.Zero;
        }
        public IntPtr PointerOfElement(int index) => _pointer + index * _elementSize;
        public IntPtr PointerOfElement(uint index) => _pointer + (int)index * _elementSize;


        public IEnumerator<T> GetEnumerator() => new Enumerator(_pointer, _count);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_pointer, _count);


        public void CopyTo(T[] array, int arrayIndex)
        {
            ReadOnlySpan<T> span = ReadOnlySpan;
            for (int i = 0; i < span.Length; i++)
                array[arrayIndex + i] = span[i];
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else return false;
        }
        public void RemoveAt(int index)
        {
            if (index < _count - 1u)
            {
                IntPtr ptr = _pointer + index * _elementSize;
                long moveSize = (_count - index) * _elementSize;
                unsafe
                {
                    Buffer.MemoryCopy((ptr + _elementSize).ToPointer(), ptr.ToPointer(), moveSize, moveSize);
                }
            }
            _count--;
        }

        public void Clear() => _count = 0u;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public struct Enumerator : IEnumerator<T>
        {
            
            private IntPtr _pointer;
            private uint _count;
            private uint _currentIndex;
            private T _current;

            public Enumerator(IntPtr pointer, uint count)
            {
                _pointer = pointer;
                _count = count;
                _currentIndex = 0u;
                _current = default(T);
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _count)
                {
                    _current = Marshal.PtrToStructure<T>(_pointer + (int)_currentIndex++ * _elementSize);
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _current = default(T);
                _currentIndex = 0u;
            }

            public void Dispose() { }
        }

    }
}
