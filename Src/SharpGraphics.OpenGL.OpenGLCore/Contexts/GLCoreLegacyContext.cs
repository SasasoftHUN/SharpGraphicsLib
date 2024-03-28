#if !OPENTK4
using OpenTK.Graphics;
using OpenTK.Platform;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore.Contexts
{
    internal class GLCoreLegacyContext : IGLContext
    {

        #region Fields

        private bool _isDisposed;

        private readonly IWindowInfo _windowInfo;
        private readonly IGraphicsContext _context;

        #endregion

        #region Properties

        public int SwapInterval { set => _context.SwapInterval = value; }

        #endregion

        #region Constructors

        internal GLCoreLegacyContext(IGraphicsView graphicsView, OperatingSystem operatingSystem)
        {
            SwapChainConstruction constructionRequest = graphicsView.SwapChainConstructionRequest;
            constructionRequest.mode.ToSwapInterval(out int swapInterval, out int backBufferCount);
            constructionRequest.depthStencilFormat.ToDepthStencilFormat(out int depth, out int stencil);

            //GraphicsContextFlags contextFlags = GraphicsContextFlags.ForwardCompatible; //Core Profile (Debug flag gets "discarded" with this, need Default for that), but RenderDoc says it's isn't???
            GraphicsContextFlags contextFlags = GraphicsContextFlags.Default;
#if DEBUG
            //TODO: What's wrong with GL Debugging?
            /*if (_management.DebugLevel != DebugLevel.None)
                contextFlags |= GraphicsContextFlags.Debug;*/
#endif
            GraphicsMode mode = new GraphicsMode(constructionRequest.colorFormat.ToColorFormat(), depth, stencil, 0, ColorFormat.Empty, backBufferCount, false);
            switch (operatingSystem)
            {
                case OperatingSystem.Windows: //Probably is never used, custom WGL integration handles Windows
                    _windowInfo = Utilities.CreateWindowsWindowInfo(graphicsView.ViewHandle);
                    break;

                case OperatingSystem.Linux:
                    switch (graphicsView.PlatformSpecificViewInfo)
                    {
                        case LinuxX11SpecificViewInfo linuxX11Info:
                                _windowInfo = Utilities.CreateX11WindowInfo(linuxX11Info.X11Display, linuxX11Info.X11Screen, graphicsView.ViewHandle, linuxX11Info.X11WindowHandle, IntPtr.Zero);
                            break;

                        default: throw new ArgumentException("PlatformSpecificViewInfo is not for the specified Linux Platform.");
                    }
                    break;

                /*case OperatingSystem.Android:
                case OperatingSystem.MacOS:
                case OperatingSystem.UWP:*/
                default: throw new PlatformNotSupportedException(operatingSystem.ToString());
            }
            _context = new GraphicsContext(mode, _windowInfo, 0, 0, contextFlags);
            _context.MakeCurrent(_windowInfo);
            _context.LoadAll();
            _context.SwapInterval = swapInterval;
        }

        ~GLCoreLegacyContext() => Dispose(disposing: false);

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
                _context?.Dispose();
                _windowInfo?.Dispose();

                // set large fields to null

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Bind() => _context.MakeCurrent(_windowInfo);
        public void UnBind() => _context.MakeCurrent(null);

        public void SwapBuffers() => _context.SwapBuffers();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
#endif