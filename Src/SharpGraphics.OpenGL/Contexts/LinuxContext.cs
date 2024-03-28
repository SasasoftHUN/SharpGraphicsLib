using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.GraphicsViews;

namespace SharpGraphics.OpenGL.Contexts
{
    public abstract class LinuxContext : IGLContext
    {

        [Flags]
        private enum DLOPEN_FLAG_BITS : int //TODO: May differ between linux distros...
        {
            RTLD_LAZY = 1,
            //RTLD_NOW = 0x0002,
            //RTLD_GLOBAL = 0x0002,
            //RTLD_LOCAL = 0x0002,
            //RTLD_NODELETE = 0x0002,
            //RTLD_NOLOAD = 0x0002,
            //RTLD_DEEPBIND = 0x0002,
        }

        [DllImport("libdl.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr dlopen(string filename, DLOPEN_FLAG_BITS flags);

        [DllImport("libdl.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so", ExactSpelling = true, SetLastError = true)]
        private extern static int dlclose(IntPtr handle);

        #region Fields

        private bool _isDisposed;

        private readonly IntPtr _libGL;

        #endregion

        #region Properties

        public abstract int SwapInterval { set; }

        #endregion

        #region Constructors

        protected LinuxContext()
        {
            _libGL = dlopen("libGL.so", DLOPEN_FLAG_BITS.RTLD_LAZY);
            if (_libGL == IntPtr.Zero)
                throw new LinuxGraphicsDeviceCreationException("Failed to open libGL.so!");
        }

        ~LinuxContext() => Dispose(disposing: false);

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

                dlclose(_libGL);

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public abstract void Bind();
        public abstract void UnBind();
        public abstract void SwapBuffers();

        public virtual IntPtr GetProcAddress(string name) => dlsym(_libGL, name);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetDelegate<T>() where T : Delegate
            => GetDelegate<T>(typeof(T).Name);
        public T GetDelegate<T>(string name) where T : Delegate
        {
            IntPtr functionPtr = GetProcAddress(name);
            if (functionPtr != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
            else throw new EntryPointNotFoundException($"dlsym and other pointer getters have not found function {name}!");
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
