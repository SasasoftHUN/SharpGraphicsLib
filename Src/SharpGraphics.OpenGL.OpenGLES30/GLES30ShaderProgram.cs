using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Graphics.ES30;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal abstract class GLES30ShaderProgram : IGLShaderProgram
    {

        #region Fields

        private bool _isDisposed;

        protected int _id;
        protected readonly ShaderType _type;

        protected readonly GLES30GraphicsDevice _device;
        protected readonly string _shaderSource;

        #endregion

        #region Properties

        public int ID => _id;

        #endregion

        #region Constructors

        protected GLES30ShaderProgram(GLES30GraphicsDevice device, ShaderSourceText shaderSource, ShaderType type)
        {
            _device = device;
            _shaderSource = shaderSource.Source;
            _type = type;
        }

        ~GLES30ShaderProgram() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                //dispose managed state (managed objects)

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL ShaderProgram from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine("Warning: GLShaderProgram cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;
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

        public void GLInitialize()
        {
            _id = GL.CreateShader(_type);

            GL.ShaderSource(_id, _shaderSource);

            GL.CompileShader(_id);

            GL.GetShader(_id, ShaderParameter.CompileStatus, out int result);

            if (0 == result)
            {
                Debug.Fail(GL.GetShaderInfoLog(_id));
                GL.DeleteShader(_id);
                _id = 0;
            }
        }
        public void GLFree()
        {
            if (_id != 0)
            {
                GL.DeleteShader(_id);
                _id = 0;
            }
        }

        #endregion

    }
}
