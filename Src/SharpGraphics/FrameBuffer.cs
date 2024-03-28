using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{

    public interface IFrameBuffer : IDisposable
    {

        bool IsDisposed { get; }

        Vector2UInt Resolution { get; }
        uint Layers { get; }

        ITexture GetAttachment(int index);
        ITexture GetAttachment(uint index);

    }

    public interface IFrameBuffer<T> : IFrameBuffer where T : ITexture
    {

        T this[int index] { get; }
        T this[uint index] { get; }

        T GetTexture(int index);
        T GetTexture(uint index);

    }

    public abstract class FrameBuffer<T> : IFrameBuffer<T> where T : ITexture
    {

        #region Fields

        private bool _isDisposed;
        private readonly bool _ownImages;

        protected readonly Vector2UInt _resolution;
        protected readonly uint _layers;

        protected readonly T[] _images;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        public Vector2UInt Resolution => _resolution;
        public uint Layers => _layers;

        public T this[int index] => _images[index];
        public T this[uint index] => _images[index];

        #endregion

        #region Constructors

        protected FrameBuffer(in ReadOnlySpan<T> images, Vector2UInt resolution, uint layers, bool ownImages)
        {
            _images = images.ToArray(); //Copy for safety
            _resolution = resolution;
            _layers = layers;
            _ownImages = ownImages;
        }

        ~FrameBuffer() //Needed in base class: Cleans Images of FrameBuffer
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
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_ownImages && _images != null)
                    foreach (T image in _images)
                        if (image != null)
                            image.Dispose();

                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public ITexture GetAttachment(int index) => _images[index];
        public ITexture GetAttachment(uint index) => _images[index];
        public T GetTexture(int index) => _images[index];
        public T GetTexture(uint index) => _images[index];

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
