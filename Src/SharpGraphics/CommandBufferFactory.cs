using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{

    [Flags]
    public enum CommandBufferFactoryProperties : uint
    {
        Nothing = 0u,
        ResettableBuffers = 1u,
        ShortLivedBuffers = 2u,

        Default = ResettableBuffers | ShortLivedBuffers,
    }

    public abstract class CommandBufferFactory : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CommandBufferFactory()
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

        public abstract GraphicsCommandBuffer CreateCommandBuffer(CommandBufferLevel level = CommandBufferLevel.Primary);

        /// <summary>
        /// Calls <see cref="CreateCommandBuffers(uint)"/> by casting <paramref name="count"/> to <see cref="uint"/>.
        /// </summary>
        /// <param name="count">Number of <see cref="GraphicsCommandBuffer"/>s to create.</param>
        /// <returns></returns>
        public GraphicsCommandBuffer[] CreateCommandBuffers(int count, CommandBufferLevel level = CommandBufferLevel.Primary) => CreateCommandBuffers((uint)count, level);
        public abstract GraphicsCommandBuffer[] CreateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
