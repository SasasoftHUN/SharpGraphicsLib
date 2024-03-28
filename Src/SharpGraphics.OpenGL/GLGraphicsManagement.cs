using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLGraphicsManagement : GraphicsManagement
    {

        #region Constructors

        protected GLGraphicsManagement(OperatingSystem operatingSystem, DebugLevel debugLevel) : base(operatingSystem, debugLevel) { }

        #endregion

        #region Protected Methods

        protected abstract IGraphicsDevice CreateGLGraphicsDevice(IGraphicsView graphicsView, IGLContextCreationRequest? contextCreationRequest = null);

        #endregion

        #region Public Methods

        public override GraphicsDeviceRequest GetAutoGraphicsDeviceRequest(IGraphicsView view)
             => new GraphicsDeviceRequest(0u, view, new PresentCommandProcessorRequest(0u));
        public override IGraphicsDevice CreateGraphicsDeviceAuto(IGraphicsView view) => CreateGLGraphicsDevice(view);
        public override IGraphicsDevice CreateGraphicsDevice(in GraphicsDeviceRequest deviceRequest)
        {
            if (deviceRequest.deviceIndex != 0u)
                throw new ArgumentOutOfRangeException("OpenGL supports only a single Device!");

            if (deviceRequest.CommandProcessorGroupRequests.Length != 1 || deviceRequest.CommandProcessorGroupRequests[0].groupIndex != 0u || deviceRequest.CommandProcessorGroupRequests[0].Count != 1u)
                throw new ArgumentOutOfRangeException("OpenGL supports only a single CommandProcessorGroup with a single CommandProcessor in it!");

            if (deviceRequest.CommandProcessorGroupRequests[0].CommandProcessorRequests[0].priority != 1f)
                throw new ArgumentOutOfRangeException("OpenGL supports only 1.0f as priority for CommandProcessors!");

            if (deviceRequest.graphicsView == null)
                throw new ArgumentOutOfRangeException("OpenGL Device can be created only with a GraphicsView!");

            return CreateGLGraphicsDevice(deviceRequest.graphicsView);
        }

        #endregion

    }
}
