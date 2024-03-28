using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.GraphicsViews;

namespace SharpGraphics.OpenGL.Contexts
{

    public abstract class X11Context : LinuxContext
    {
        //https://www.khronos.org/opengl/wiki/Programming_OpenGL_in_Linux:_GLX_and_Xlib

        //https://xwindow.angelfire.com/page28.html
        [DllImport("libdl.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr XOpenDisplay(string display_name); //https://www.x.org/releases/X11R7.7/doc/man/man3/XOpenDisplay.3.xhtml

        [DllImport("libGL.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr glXChooseVisual(IntPtr dpy, int screen, int[] attributes); 
        [DllImport("libGL.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr glXCreateContext(IntPtr dpy, IntPtr vis, IntPtr shareList, bool direct); 

        [DllImport("libGL.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr glXMakeCurrent(IntPtr dpy, IntPtr drawable, IntPtr ctx);
        [DllImport("libGL.so", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr glXSwapBuffers(IntPtr dpy, IntPtr drawable);

        #region Fields

        private bool _isDisposed;

        private IntPtr _x11Display;
        private IntPtr _x11Window;

        private IntPtr _context;

        #endregion

        #region Properties

        public override int SwapInterval { set => throw new NotImplementedException(); }

        #endregion

        #region Constructors

        protected X11Context()
        {
            throw new NotImplementedException();
        }
        protected X11Context(LinuxX11SpecificViewInfo x11ViewInfo, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext)
        {
            _x11Display = x11ViewInfo.X11Display;
            _x11Window = x11ViewInfo.X11WindowHandle;

            IntPtr visualInfo = glXChooseVisual(x11ViewInfo.X11Display, x11ViewInfo.X11Screen, new int[] { 0 });
            if (visualInfo == IntPtr.Zero)
            {
                throw new Exception("X11 GL Context Creation: No Appropriate Visual found!");
            }

            _context = glXCreateContext(x11ViewInfo.X11Display, visualInfo, IntPtr.Zero, true);
            if (_context == IntPtr.Zero)
            {
                
            }

            Bind();
            LoadBindings();
        }

        ~X11Context() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected abstract void LoadBindings();

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        public static bool IsAvailable() => Environment.GetEnvironmentVariable("DISPLAY") != null;

        #endregion

        #region Public Methods

        public override void Bind() => glXMakeCurrent(_x11Display, _x11Window, _context);
        public override void UnBind() => glXMakeCurrent(_x11Display, IntPtr.Zero, IntPtr.Zero);

        public override void SwapBuffers() => glXSwapBuffers(_x11Display, _x11Window);

        public override IntPtr GetProcAddress(string name) => base.GetProcAddress(name);

        #endregion

    }
}
