#if !OPENTK4 && !ANDROID
using OpenTK.Graphics;
using OpenTK.Platform;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Contexts
{
    internal class GLES30LegacyDesktopContext : GLES30LegacyContextBase
    {

        #region Properties

        public override int SwapInterval { set => _context.SwapInterval = value; }

        #endregion

        #region Constructors

        internal GLES30LegacyDesktopContext(IGraphicsView graphicsView, OperatingSystem operatingSystem, DebugLevel debugLevel): base(graphicsView, operatingSystem, debugLevel) { }

        ~GLES30LegacyDesktopContext() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected override void CreateContext(IGraphicsView graphicsView, OperatingSystem operatingSystem, DebugLevel debugLevel, out IWindowInfo windowInfo, out IGraphicsContext context)
        {
            SwapChainConstruction constructionRequest = graphicsView.SwapChainConstructionRequest;
            constructionRequest.mode.ToSwapInterval(out _, out int backBufferCount);
            constructionRequest.depthStencilFormat.ToDepthStencilFormat(out int depth, out int stencil);

            GraphicsContextFlags contextFlags = GraphicsContextFlags.Embedded;
#if DEBUG
            if (debugLevel != DebugLevel.None)
                contextFlags |= GraphicsContextFlags.Debug;
#endif

            GraphicsMode mode = new GraphicsMode(constructionRequest.colorFormat.ToColorFormat(), depth, stencil, 0, ColorFormat.Empty, backBufferCount, false);
            switch (operatingSystem)
            {
                case OperatingSystem.Windows: //Probably is never used, custom WGL integration handles Windows
                    windowInfo = Utilities.CreateWindowsWindowInfo(graphicsView.ViewHandle);
                    break;

                case OperatingSystem.Linux:
                    switch (graphicsView.PlatformSpecificViewInfo)
                    {
                        case LinuxX11SpecificViewInfo linuxX11Info:
                            windowInfo = Utilities.CreateX11WindowInfo(linuxX11Info.X11Display, linuxX11Info.X11Screen, graphicsView.ViewHandle, linuxX11Info.X11WindowHandle, IntPtr.Zero);
                            break;

                        default: throw new ArgumentException("PlatformSpecificViewInfo is not for the specified Linux Platform.");
                    }
                    break;

                /*case OperatingSystem.Android:
                case OperatingSystem.MacOS:
                case OperatingSystem.UWP:*/
                default: throw new PlatformNotSupportedException(operatingSystem.ToString());
            }
            context = new GraphicsContext(mode, _windowInfo, 0, 0, contextFlags);
        }

        #endregion

    }
}
#endif