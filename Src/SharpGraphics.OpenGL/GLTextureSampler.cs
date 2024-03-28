using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLTextureSampler : TextureSampler, IGLResource
    {

        #region Fields

        private bool _isDisposed = false;

        protected int _id;

        protected readonly GLGraphicsDevice _device;
        protected readonly TextureSamplerConstruction _construction;

        #endregion

        #region Prpoerties

        public int ID => _id;

        #endregion

        #region Constructors

        protected GLTextureSampler(GLGraphicsDevice device, in TextureSamplerConstruction construction)
        {
            _device = device;
            _construction = construction;
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
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL TextureSampler from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine("Warning: GLTextureSampler cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public abstract void GLInitialize();
        public abstract void GLFree();

        public abstract void GLBind(int textureUnit);
        public abstract void GLUnBind(int textureUnit);

        #endregion

    }
}
