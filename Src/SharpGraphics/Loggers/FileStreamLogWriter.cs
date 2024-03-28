using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGraphics.Loggers
{
    public class FileStreamLogWriter : StreamLogWriter
    {

        #region Fields

        private FileStream? _fileStream;

        private bool _isDisposed = false;

        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects)
                }*/

                if (_fileStream != null)
                    _fileStream.Dispose();

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public override void Initialize(string name)
        {
            _fileStream = File.OpenWrite(name);
            Initialize(_fileStream);
        }

        #endregion

    }
}
