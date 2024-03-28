#if !OPENTK4
using OpenTK.Graphics;
using OpenTK.Platform;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Contexts
{
    internal abstract class GLES30LegacyContextBase : IGLContext
    {

        #region Fields

        private bool _isDisposed;

        protected readonly IWindowInfo _windowInfo;
        protected readonly IGraphicsContext _context;

        #endregion

        #region Properties

        public abstract int SwapInterval { set; }

        #endregion

        #region Constructors

        protected GLES30LegacyContextBase(IGraphicsView graphicsView, OperatingSystem operatingSystem, DebugLevel debugLevel)
        {
            CreateContext(graphicsView, operatingSystem, debugLevel, out _windowInfo, out _context);

            Bind();
            _context.LoadAll();

            graphicsView.SwapChainConstructionRequest.mode.ToSwapInterval(out int swapInterval, out _);
            SwapInterval = swapInterval;
        }

        ~GLES30LegacyContextBase() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected abstract void CreateContext(IGraphicsView graphicsView, OperatingSystem operatingSystem, DebugLevel debugLevel, out IWindowInfo windowInfo, out IGraphicsContext context);

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