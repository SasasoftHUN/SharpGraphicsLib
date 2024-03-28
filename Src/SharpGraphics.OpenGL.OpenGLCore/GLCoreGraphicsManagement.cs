using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;
using SharpGraphics.OpenGL.Contexts;
using SharpGraphics.OpenGL.OpenGLCore.Contexts;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    /// <summary>
    /// Acts as a Factory for <see cref="GLGraphicsDevice"/>s
    /// </summary>
    public sealed class GLCoreGraphicsManagement : GLGraphicsManagement
    {

        #region Fields

        private readonly GLCoreGraphicsDeviceInfo[] _deviceInfos = new GLCoreGraphicsDeviceInfo[] { new GLCoreGraphicsDeviceInfo() };

        #endregion

        #region Properties

        public override ReadOnlySpan<GraphicsDeviceInfo> AvailableDevices => _deviceInfos;

        #endregion

        #region Constructors

        public GLCoreGraphicsManagement(OperatingSystem operatingSystem, DebugLevel debugLevel): base(operatingSystem, debugLevel)
        {
            try
            {
                IGLContext? offscreenContext = null;
                switch (operatingSystem)
                {
                    case OperatingSystem.Windows:
                        offscreenContext = new WGLCoreContext();
                        break;

                    /*case OperatingSystem.UWP:
                    case OperatingSystem.Linux:
                    case OperatingSystem.Android:
                    case OperatingSystem.MacOS:
                    default:
                        break;*/
                }

                if (offscreenContext != null)
                    using (offscreenContext)
                    {
                        GLCoreGraphicsDeviceFeatures features = new GLCoreGraphicsDeviceFeatures();
                        _deviceInfos[0] = new GLCoreGraphicsDeviceInfo(features, new GLCoreGraphicsDeviceLimits(features));
                    }
            }
            catch { }
        }

        #endregion

        #region Protected Methods

        protected override IGraphicsDevice CreateGLGraphicsDevice(IGraphicsView view, IGLContextCreationRequest? contextCreationRequest = null) => new GLCoreGraphicsDevice(this, view, contextCreationRequest);

        #endregion

    }
}
