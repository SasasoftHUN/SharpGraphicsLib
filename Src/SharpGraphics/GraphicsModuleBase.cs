using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{

    public readonly struct GraphicsModuleContext
    {
        public readonly IGraphicsDevice device;
        public readonly IGraphicsView view;
        public readonly Timers timer;

        internal GraphicsModuleContext(IGraphicsDevice device, IGraphicsView view, Timers timer)
        {
            this.device = device;
            this.view = view;
            this.timer = timer;
        }
    }

    public abstract class GraphicsModuleBase : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        protected readonly IGraphicsDevice _device;
        protected readonly IGraphicsView _view;
        protected readonly Timers _timers;

        #endregion

        #region Constructors

        public GraphicsModuleBase(in GraphicsModuleContext context)
        {
            _device = context.device;
            _view = context.view;
            _timers = context.timer;
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GraphicsModuleBase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

        protected internal abstract void Update();
        protected internal abstract void Render();

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
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

        #endregion

    }
}
