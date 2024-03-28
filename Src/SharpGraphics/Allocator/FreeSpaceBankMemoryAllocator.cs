using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Allocator
{
    public sealed class FreeSpaceBankMemoryAllocator : MemoryAllocatorBase
    {

        private class IntPtrComparer : IComparer<IntPtr>
        {
            public int Compare(IntPtr x, IntPtr y) => (int)x - (int)y;
        }

        private struct MemoryBank
        {
            private readonly SortedDictionary<IntPtr, int> _freeSpaces;
            //private int _largestFreeSpace;

            public readonly IntPtr address;
            public readonly int size;

            //public int LargestFreeSpace => _largestFreeSpace;

            public MemoryBank(IntPtr address, int size)
            {
#if CSLANG_9_0
                _freeSpaces = new SortedDictionary<IntPtr, int>()
#else
                _freeSpaces = new SortedDictionary<IntPtr, int>(new IntPtrComparer())
#endif
                {
                    { address, size },
                };
                //_largestFreeSpace = size;
                this.address = address;
                this.size = size;
            }

            public IntPtr Get(int size)
            {
                if (_freeSpaces != null)
                {
                    IntPtr result = IntPtr.Zero;
                    int resultSize = int.MaxValue;
                    foreach (KeyValuePair<IntPtr, int> freeSpace in _freeSpaces)
                        if (freeSpace.Value >= size && freeSpace.Value < resultSize)
                        {
                            result = freeSpace.Key;
                            resultSize = freeSpace.Value;
                        }

                    if (result != IntPtr.Zero)
                    {
                        _freeSpaces.Remove(result);
                        _freeSpaces.Add(result + size, resultSize - size);
                    }

                    return result;
                }
                else return IntPtr.Zero;
            }
            public void Release(IntPtr address, int size)
            {
                IntPtr end = address + size;

                IntPtr combineBefore = IntPtr.Zero;
                int combineBeforeSize = 0;
                IntPtr combineAfter = IntPtr.Zero;
                int combineAfterSize = 0;

                foreach (KeyValuePair<IntPtr, int> freeSpace in _freeSpaces)
                {
                    if (combineBefore == IntPtr.Zero && freeSpace.Key + freeSpace.Value == address)
                    {
                        combineBefore = freeSpace.Key;
                        combineBeforeSize = freeSpace.Value;
                        if (combineAfter != IntPtr.Zero)
                            break;
                    }
                    if (combineAfter == IntPtr.Zero && freeSpace.Key == end)
                    {
                        combineAfter = freeSpace.Key;
                        combineAfterSize = freeSpace.Value;
                        if (combineBefore != IntPtr.Zero)
                            break;
                    }
                }

                if (combineBefore != IntPtr.Zero)
                {
                    if (combineAfter != IntPtr.Zero)
                    {
                        _freeSpaces[combineBefore] = combineBeforeSize + size + combineAfterSize;
                        _freeSpaces.Remove(combineAfter);
                    }
                    else _freeSpaces[combineBefore] = combineBeforeSize + size;
                }
                else
                {
                    if (combineAfter != IntPtr.Zero)
                    {
                        _freeSpaces[address] = size + combineAfterSize;
                        _freeSpaces.Remove(combineAfter);
                    }
                    else _freeSpaces[address] = size;
                }
            }

            public void ReleaseAll()
            {
                _freeSpaces.Clear();
                _freeSpaces[address] = size;
            }

        }

        #region Fields

        private bool _isDisposed;
        private readonly int _bankSizeAllocationMultiplier = 16;
        private readonly int _bankSizeAllocationMultiplierLimit = 16777216;

        private MemoryBank[] _banks;

        #endregion

        #region Constructors

        public FreeSpaceBankMemoryAllocator()
        {
            _banks = new MemoryBank[16];
        }
        public FreeSpaceBankMemoryAllocator(int defaultBankCount, int bankSizeAllocationMultiplier)
        {
            _banks = new MemoryBank[defaultBankCount];
            _bankSizeAllocationMultiplier = bankSizeAllocationMultiplier;
        }

        ~FreeSpaceBankMemoryAllocator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNewBankIndex()
        {
            for (int i = 0; i < _banks.Length; i++)
                if (_banks[i].address == IntPtr.Zero)
                    return i;

            MemoryBank[] newBanks = new MemoryBank[_banks.Length * 2];
            for (int i = 0; i < _banks.Length; i++)
                newBanks[i] = _banks[i];
            int index = _banks.Length;
            _banks = newBanks;
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AllocateInBanks(int size)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MemoryAllocator");

            IntPtr address = Marshal.AllocHGlobal(size);
            int bankIndex = GetNewBankIndex();
            _banks[bankIndex] = new MemoryBank(address, size);
            return bankIndex;
        }

        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                // Dispose managed state (managed objects).

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.

                // Call the base class implementation.
                base.Dispose(disposing);

                for (int i = 0; i < _banks.Length; i++)
                    if (_banks[i].address != IntPtr.Zero)
                        Marshal.FreeHGlobal(_banks[i].address);
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public override MemoryAllocation Allocate(int size)
        {
            for (int i = 0; i < _banks.Length; i++)
            {
                IntPtr address = _banks[i].Get(size);
                if (address != IntPtr.Zero)
                    return new MemoryAllocation(this, i, address, size);
            }

            {
                int newBankIndex = AllocateInBanks(size <= _bankSizeAllocationMultiplierLimit ? size * _bankSizeAllocationMultiplier : size);
                return new MemoryAllocation(this, newBankIndex, _banks[newBankIndex].Get(size), size);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(in MemoryAllocation allocation)
            => _banks[allocation.bank].Release(allocation.address, allocation.size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReleaseAll()
        {
            for (int i = 0; i < _banks.Length; i++)
                if (_banks[i].address != IntPtr.Zero)
                    _banks[i].ReleaseAll();
        }

        #endregion

    }
}
