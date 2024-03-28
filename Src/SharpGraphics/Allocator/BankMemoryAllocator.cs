using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Allocator
{
    public sealed class BankMemoryAllocator : MemoryAllocatorBase
    {

        private class IntPtrComparer : IComparer<IntPtr>
        {
            public int Compare(IntPtr x, IntPtr y) => (int)x - (int)y;
        }

        private struct MemoryBank
        {

            private IntPtr _allocator;
            private int _sizeLeft;

            public readonly IntPtr address;
            public readonly int size;

            public MemoryBank(IntPtr address, int size)
            {
                _allocator = address;
                _sizeLeft = size;

                this.address = address;
                this.size = size;
            }

            public IntPtr Get(int size)
            {
                IntPtr result = _allocator;
                _allocator += size;
                _sizeLeft -= size;
                return result;
            }
            public bool TryGet(int size, out IntPtr result)
            {
                if (_sizeLeft >= size)
                {
                    result = _allocator;
                    _allocator += size;
                    _sizeLeft -= size;
                    return true;
                }
                else
                {
                    result = IntPtr.Zero;
                    return false;
                }
            }

            public void ReleaseAll()
            {
                _allocator = address;
                _sizeLeft = size;
            }

        }

        #region Fields

        private bool _isDisposed;
        private readonly int _bankSizeAllocationMultiplier = 16;
        private readonly int _bankSizeAllocationMultiplierLimit = 16777216;

        private MemoryBank[] _banks;
        private int _nextAllocationBankIndex = 0;

        #endregion

        #region Constructors

        public BankMemoryAllocator()
        {
            _banks = new MemoryBank[16];
        }
        public BankMemoryAllocator(int defaultBankCount, int bankSizeAllocationMultiplier)
        {
            _banks = new MemoryBank[defaultBankCount];
            _bankSizeAllocationMultiplier = bankSizeAllocationMultiplier;
        }

        ~BankMemoryAllocator()
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
            for (int i = _nextAllocationBankIndex; i < _banks.Length; i++)
                if (_banks[i].TryGet(size, out IntPtr address))
                {
                    _nextAllocationBankIndex = i >= _banks.Length - 1 ? 0 : i + 1;
                    return new MemoryAllocation(this, i, address, size);
                }
            for (int i = 0; i < _nextAllocationBankIndex; i++)
                if (_banks[i].TryGet(size, out IntPtr address))
                {
                    _nextAllocationBankIndex = i >= _banks.Length - 1 ? 0 : i + 1;
                    return new MemoryAllocation(this, i, address, size);
                }

            {
                int newBankIndex = AllocateInBanks(size <= _bankSizeAllocationMultiplierLimit ? size * _bankSizeAllocationMultiplier : size);
                _nextAllocationBankIndex = newBankIndex >= _banks.Length - 1 ? 0 : newBankIndex + 1;
                return new MemoryAllocation(this, newBankIndex, _banks[newBankIndex].Get(size), size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(in MemoryAllocation allocation) { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReleaseAll()
        {
            for (int i = 0; i < _banks.Length; i++)
                if (_banks[i].address != IntPtr.Zero)
                    _banks[i].ReleaseAll();
            _nextAllocationBankIndex = 0;
        }

        #endregion

    }
}
