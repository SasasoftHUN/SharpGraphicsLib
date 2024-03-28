using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGraphics.Loggers
{
    public class StreamLogWriter : ILogWriter
    {

        #region Fields

        private StreamWriter? _streamWriter;

        private bool _isInitialized = false;
        private bool _isDisposed = false;

        #endregion

        #region Constructors

        public StreamLogWriter() { }
        public StreamLogWriter(Stream stream)
        {
            _streamWriter = new StreamWriter(stream);
            _isInitialized = true;
        }
        ~StreamLogWriter()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects)
                }*/

                if (_streamWriter != null)
                    _streamWriter.Dispose();

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public virtual void Initialize(string name) { }
        public void Initialize(Stream stream)
        {
            if (_streamWriter != null)
                _streamWriter.Dispose();
            _streamWriter = new StreamWriter(stream);
            _isInitialized = true;
        }
        public void WriteLog(StringBuilder log)
        {
            if (_isInitialized && !_isDisposed && _streamWriter != null)
                _streamWriter.Write(log);
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
