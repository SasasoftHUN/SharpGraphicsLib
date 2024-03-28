using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Apps.NormalsThreads
{
    public class SecondaryCommandBuffers : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        private CommandBufferFactory[] _factories;
        private GraphicsCommandBuffer[] _commandBuffers;

        #endregion

        #region Properties

        public CommandBufferFactory[] Factories => _factories;
        public GraphicsCommandBuffer[] CommandBuffers => _commandBuffers;

        #endregion

        #region Constructors

        public SecondaryCommandBuffers(GraphicsCommandProcessor commandProcessor, uint bufferCount)
        {
            _factories = commandProcessor.CreateCommandBufferFactories(bufferCount);
            _commandBuffers = new GraphicsCommandBuffer[bufferCount];
            for (int i = 0; i < _factories.Length; i++)
                _commandBuffers[i] = _factories[i].CreateCommandBuffer(CommandBufferLevel.Secondary);
        }

        ~SecondaryCommandBuffers()
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
                    // dispose managed state (managed objects)
                }*/

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                if (_commandBuffers != null)
                    foreach (GraphicsCommandBuffer commandBuffer in _commandBuffers)
                        if (!commandBuffer.IsDisposed)
                            commandBuffer.Dispose();

                if (_factories != null)
                    foreach (CommandBufferFactory factory in _factories)
                        if (!factory.IsDisposed)
                            factory.Dispose();

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
