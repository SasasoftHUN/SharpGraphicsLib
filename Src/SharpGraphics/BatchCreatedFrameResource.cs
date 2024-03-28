using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public class BatchCreatedFrameResource<T> : FrameResource<T>
    {

        #region Fields

        private readonly Func<uint, T[]> _constructor;

        #endregion

        #region Constructors

        public BatchCreatedFrameResource(GraphicsSwapChain swapChain, Func<uint, T[]> constructor) : base(swapChain)
        {
            _constructor = constructor;

            if (swapChain.FrameCount > 0)
                InitializeResources(swapChain.FrameCount); //Need to call in subclass, because _constructor is not yet initialized in base
        }

        //~BatchCreatedFrameResource() => Dispose(disposing: false); //Base calls it

        #endregion

        #region Protected Methods

        protected override T[] CreateResources(uint count) => _constructor(count);

        #endregion

    }
}
