using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using SharpGraphics.Loggers;

namespace SharpGraphics
{
    public abstract class GraphicsApplicationBase : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        private readonly GraphicsManagement _graphicsManagement;

        private Thread _updateThread;
        private CancellationTokenSource _updateThreadCancellationTokenSource;

        protected Timers _timers;
        protected IGraphicsDevice? _graphicsDevice;
        protected readonly IGraphicsView _view;
        protected volatile bool _isRunning = false;
        protected volatile bool _reinitializeViewOnDispose = true;

        #endregion

        #region Properties

        public IFrameLogger? Logger { get; set; }

        #endregion

        #region Constructors

        protected GraphicsApplicationBase(GraphicsManagement graphicsManagement, IGraphicsView view)
        {
            DebugUtils.ThrowIfNull(graphicsManagement, "graphicsManagement");
            _graphicsManagement = graphicsManagement;

            DebugUtils.ThrowIfNull(view, "view");
            _view = view;
            _view.ViewDestroyed += _view_ViewDestroyed;
            if (_view.UserInputSource != null)
                _view.UserInputSource.UserInput += _view_UserInput;

            _updateThreadCancellationTokenSource = new CancellationTokenSource();
            _updateThread = new Thread(UpdateThreadJob);
            _timers = new Timers();
        }

        ~GraphicsApplicationBase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            BeginDispose(disposing: false); //Render Thread and Device must be stopped before doing any Dispose in the subclasses
        }

        #endregion

        #region Private Methods

        private void UpdateThreadJob()
        {
            Initialize();

            while (_isRunning && !_updateThreadCancellationTokenSource.IsCancellationRequested)
            {
                _timers.BeforeUpdate();
                if (_view.UserInputSource != null && !_view.UserInputSource.ImmediateInputEvents)
                    _view.UserInputSource.QueryInputEvents();
                Update();
                _timers.AfterUpdate();

                _timers.BeforeRender();
                Render();
                _timers.AfterRender();

                Logger?.Log(_timers);
            }
        }

        private void BeginDispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (_updateThreadCancellationTokenSource != null)
                    _updateThreadCancellationTokenSource.Cancel();

                if (_updateThread != null && _isRunning)
                    if (!_updateThread.Join(1000))
                        _updateThread.Interrupt();
                _isRunning = false;

                if (_graphicsDevice != null && !_graphicsDevice.IsDisposed)
                    _graphicsDevice.WaitForIdle();

                Dispose(disposing);

                _isDisposed = true;
            }
        }

        private void _view_ViewDestroyed(object? sender, EventArgs e)
        {
            _reinitializeViewOnDispose = false;
            if (_isRunning && !_isDisposed && _updateThreadCancellationTokenSource != null)
                _updateThreadCancellationTokenSource.Cancel();
        }
        private void _view_UserInput(object? sender, UserInputEventArgs e)
        {
            switch (e)
            {
                case UserInputCloseEventArgs close:
                    _reinitializeViewOnDispose = false;
                    if (_isRunning && !_isDisposed && _updateThreadCancellationTokenSource != null)
                        _updateThreadCancellationTokenSource.Cancel();
                    break;
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void Initialize()
        {
            _view.UserInputSource?.DiscardInputEvents();
            _timers.Start();
        }
        protected abstract void Update();
        protected abstract void Render();

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null

                if (!disposing)
                    Debug.WriteLine($"Disposing GraphicsApplicationBase from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_graphicsDevice != null && !_graphicsDevice.IsDisposed)
                {
                    _graphicsDevice.WaitForIdle();
                    _graphicsDevice.Dispose();
                }

                if (Logger != null && Logger is IDisposable logger)
                    logger.Dispose();

                if (_view != null)
                {
                    _view.ViewDestroyed -= _view_ViewDestroyed;
                    if (_view.UserInputSource != null)
                        _view.UserInputSource.UserInput -= _view_UserInput;

                    if (_reinitializeViewOnDispose)
                        _view.Reinitialize();
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void InitializeAndStart() => InitializeAndStart(_graphicsManagement.GetAutoGraphicsDeviceRequest(_view));
        public void InitializeAndStart(in GraphicsDeviceRequest graphicsDeviceRequest)
        {
            _graphicsDevice = _graphicsManagement.CreateGraphicsDevice(graphicsDeviceRequest);

            try
            {
                _updateThread.Start();
                _isRunning = true;
            }
            catch (ThreadAbortException) { Debug.WriteLine("Update Thread Aborted!"); }
        }
        public void WaitForEnd()
        {
            if (!_isRunning || _isDisposed || _updateThread == null)
                return;
            else _updateThread.Join();
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            BeginDispose(disposing: true); //Render Thread and Device must be stopped before doing any Dispose in the subclasses
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
