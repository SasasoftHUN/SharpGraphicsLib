using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public abstract class GraphicsCommandProcessor : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        public GraphicsCommandProcessorType Type { get; }
        public float Priority { get; }
        public bool IsDisposed => _isDisposed;

        public abstract CommandBufferFactory CommandBufferFactory { get; }

        #endregion

        #region Constructors

        protected GraphicsCommandProcessor(GraphicsCommandProcessorType type, float priority)
        {
            Type = type;
            Priority = priority;
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GraphicsCommandProcessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

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
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public abstract CommandBufferFactory CreateCommandBufferFactory(CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default);
        public CommandBufferFactory[] CreateCommandBufferFactories(int count, CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default) => CreateCommandBufferFactories((uint)count, properties);
        public abstract CommandBufferFactory[] CreateCommandBufferFactories(uint count, CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default);

        public abstract void Submit(GraphicsCommandBuffer commandBuffer);
        //public abstract void Submit(GraphicsCommandBuffer[] commandBuffers);

        public abstract void WaitForIdle();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
