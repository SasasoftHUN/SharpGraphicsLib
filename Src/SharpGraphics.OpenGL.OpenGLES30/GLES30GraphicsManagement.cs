using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;
using SharpGraphics.OpenGL.Contexts;
using SharpGraphics.OpenGL.OpenGLES30.Contexts;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    /// <summary>
    /// Acts as a Factory for <see cref="GLGraphicsDevice"/>s
    /// </summary>
    public sealed class GLES30GraphicsManagement : GLGraphicsManagement
    {

        #region Fields

        private readonly GLES30GraphicsDeviceInfo[] _deviceInfos = new GLES30GraphicsDeviceInfo[] { new GLES30GraphicsDeviceInfo() };

        #endregion

        #region Properties

        public override ReadOnlySpan<GraphicsDeviceInfo> AvailableDevices => _deviceInfos;

        #endregion

        #region Constructors

        public GLES30GraphicsManagement(OperatingSystem operatingSystem, DebugLevel debugLevel): base(operatingSystem, debugLevel)
        {
            try
            {
                IGLContext? offscreenContext = null;
                switch (operatingSystem)
                {
                    case OperatingSystem.Windows:
                        offscreenContext = new WGLES30Context();
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
                        GLES30GraphicsDeviceFeatures features = new GLES30GraphicsDeviceFeatures();
                        _deviceInfos[0] = new GLES30GraphicsDeviceInfo(features, new GLES30GraphicsDeviceLimits(features));
                    }
            }
            catch { }
        }

        #endregion

        #region Protected Methods

        protected override IGraphicsDevice CreateGLGraphicsDevice(IGraphicsView graphicsView, IGLContextCreationRequest? contextCreationRequest = null) => new GLES30GraphicsDevice(this, graphicsView, contextCreationRequest);

        #endregion

    }
}
