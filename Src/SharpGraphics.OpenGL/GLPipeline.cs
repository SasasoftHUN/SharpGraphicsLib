using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLPipeline : IPipeline, IGLResource
    {

        #region Fields

        private bool _isDisposed;

        protected readonly GLGraphicsDevice _device;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        protected internal GLPipeline(GLGraphicsDevice device) => _device = device;

        ~GLPipeline() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                // Dispose managed state (managed objects)

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL Pipeline from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine("Warning: GLPipeline cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public abstract void GLInitialize();
        public abstract void GLFree();

        public abstract void GLBind();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
