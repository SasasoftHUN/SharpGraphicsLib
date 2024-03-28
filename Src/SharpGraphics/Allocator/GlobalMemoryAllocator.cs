using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Allocator
{
    public sealed class GlobalMemoryAllocator : MemoryAllocatorBase
    {

        #region Fields

        private List<IntPtr> _allocations = new List<IntPtr>();

        #endregion

        #region Constructors

        ~GlobalMemoryAllocator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Public Methods

        public override MemoryAllocation Allocate(int size)
        {
            /*if (_isDisposed)
                throw new ObjectDisposedException("MemoryAllocator");*/

            IntPtr address = Marshal.AllocHGlobal(size);
            _allocations.Add(address);
            return new MemoryAllocation(this, 0, address, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(in MemoryAllocation allocation)
        {
            Marshal.FreeHGlobal(allocation.address);
            _allocations.Remove(allocation.address);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReleaseAll()
        {
            foreach (IntPtr allocation in _allocations)
                Marshal.FreeHGlobal(allocation);
            _allocations.Clear();
        }

        #endregion

    }
}
