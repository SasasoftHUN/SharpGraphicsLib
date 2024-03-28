using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics
{
    public class GraphicsApplication<T> : GraphicsApplicationBase where T : GraphicsModuleBase
    {

        public delegate T GraphicsModuleFactory(in GraphicsModuleContext context);

        #region Fields

        private readonly GraphicsModuleFactory _moduleFactory;
        private T? _module;

        private bool _isDisposed;

        #endregion

        #region Properties

        public T? Module => _module;

        #endregion

        #region Constructors

        public GraphicsApplication(GraphicsManagement graphicsManagement, IGraphicsView view, GraphicsModuleFactory moduleFactory) : base(graphicsManagement, view)
        {
            _moduleFactory = moduleFactory;
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();
            if (_graphicsDevice != null)
                _module = _moduleFactory(new GraphicsModuleContext(_graphicsDevice, _view, _timers));
            else throw new NullReferenceException("Graphics Device has not been initialized!");
        }
        protected override void Update() => _module?.Update();
        protected override void Render() => _module?.Render();

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing GraphicsApplication<{typeof(T).Name}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_graphicsDevice != null && !_graphicsDevice.IsDisposed)
                {
                    _graphicsDevice.WaitForIdle();

                    if (_module != null)
                        _module.Dispose();
                }
                else Debug.WriteLine("Warning: GraphicsApplication cannot be disposed properly because GraphicsDevice is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
