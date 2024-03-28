using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public sealed class RawString : IDisposable
    {

        #region Fields

        private IntPtr _pointer;
        private string? _string;
        private bool _ownMemory;

        #endregion

        #region Properties

        public IntPtr Pointer => _pointer;
        public unsafe char* RawPointer => (char*)_pointer.ToPointer();
        public string? String => _string;

        #endregion

        #region Constructors

        public RawString(IntPtr pointer)
        {
            _pointer = pointer;
            _string = Marshal.PtrToStringAnsi(pointer);
            _ownMemory = false;
        }
        public RawString(string str)
        {
            _pointer = Marshal.StringToHGlobalAnsi(str);
            _string = str;
            _ownMemory = true;
        }

        ~RawString() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private void Dispose(bool disposing)
        {
            if (_pointer != IntPtr.Zero)
            {
                /*if (disposing)
                {
                    // dispose managed state (managed objects)
                }*/

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                if (_ownMemory)
                    Marshal.FreeHGlobal(_pointer);
                _pointer = IntPtr.Zero;
                _string = null;
                _ownMemory = false;
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Operators

        public static unsafe implicit operator char*(RawString rawString) => rawString.RawPointer;
        public static implicit operator IntPtr(RawString rawString) => rawString.Pointer;
        public static implicit operator string?(RawString rawString) => rawString.String;

        public static unsafe implicit operator RawString(void* pointer) => new RawString(new IntPtr(pointer));
        public static implicit operator RawString(IntPtr pointer) => new RawString(pointer);
        public static implicit operator RawString(string str) => new RawString(str);

        #endregion

    }
}
