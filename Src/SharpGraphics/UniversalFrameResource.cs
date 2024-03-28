using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public class UniversalFrameResource<T> : FrameResource<T>
    {

        #region Fields

        private readonly Func<T> _constructor;

        #endregion

        #region Constructors

        public UniversalFrameResource(GraphicsSwapChain swapChain, Func<T> constructor): base(swapChain)
        {
            _constructor = constructor;

            if (swapChain.FrameCount > 0)
                InitializeResources(swapChain.FrameCount); //Need to call in subclass, because _constructor is not yet initialized in base
        }

        //~UniversalFrameResource() => Dispose(disposing: false); //Base calls it

        #endregion

        #region Protected Methods

        protected override T[] CreateResources(uint count)
        {
            T[] resources = new T[count];

            for (uint i = 0; i < count; i++)
                resources[i] = _constructor();

            return resources;
        }

        #endregion

    }
}
