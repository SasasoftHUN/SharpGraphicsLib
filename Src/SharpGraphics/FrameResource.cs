using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public abstract class FrameResource<T> : IDisposable
    {

        #region Fields

        private readonly GraphicsSwapChain _swapChain;

        private T[] _resources;

        private bool _isDisposed;

        #endregion

        #region Properties

        public ref T Resource => ref _resources[_swapChain.CurrentFrameIndex];

        public ref T this[int i]
        {
            get
            {
                if (i == 0)
                    return ref _resources[_swapChain.CurrentFrameIndex];
                else
                {
                    int index = ((int)_swapChain.CurrentFrameIndex + i) % _resources.Length;
                    return ref _resources[index < 0 ? (index + _resources.Length) : index];
                }
            }
        }

        #endregion

        #region Constructors

        protected FrameResource(GraphicsSwapChain swapChain)
        {
            _swapChain = swapChain;
            _resources = new T[0];

            swapChain.FramesRecreated += SwapChain_FramesRecreated;
        }

        ~FrameResource() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private void SwapChain_FramesRecreated(object? sender, GraphicsSwapChain.SwapChainInfoEventArgs e) => InitializeResources(e.FrameCount);

        #endregion

        #region Protected Methods

        protected void InitializeResources(uint count)
        {
            if (_resources.Length != count)
            {
                DisposeResources();
                _resources = CreateResources(count);
            }
        }

        protected abstract T[] CreateResources(uint count);
        protected virtual void DisposeResources()
        {
            for (int i = 0; i < _resources.Length; i++)
                if (_resources[i] != null && _resources[i] is IDisposable disposable)
                    disposable.Dispose(); //TODO: Does not dispose Array of T or any Collection<T>
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // dispose managed state (managed objects)
                }*/

                if (_swapChain != null)
                    _swapChain.FramesRecreated -= SwapChain_FramesRecreated;

                DisposeResources();

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public static implicit operator T(FrameResource<T> r) => r._resources[r._swapChain.CurrentFrameIndex];

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
