#if !OPENTK4 && ANDROID
using Android.Views;
using OpenTK.Graphics;
using OpenTK.Platform;
using OpenTK.Platform.Android;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Contexts
{
    internal class GLES30LegacyAndroidContext : GLES30LegacyContextBase
    {

        #region Properties

        public override int SwapInterval { set { } } //Cannot set on Android, fail silently

        #endregion

        #region Constructors

        internal GLES30LegacyAndroidContext(IGraphicsView graphicsView, DebugLevel debugLevel) : base(graphicsView, OperatingSystem.Android, debugLevel) { }

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

            AndroidGraphicsMode mode = new AndroidGraphicsMode(constructionRequest.colorFormat.ToColorFormat(), depth, stencil, 0, backBufferCount, false);

            if (graphicsView.NativeView is SurfaceView androidSurfaceView && androidSurfaceView.Holder != null)
            {
                ISurfaceHolder holder = androidSurfaceView.Holder;
                AndroidWindow androidWindowInfo = new AndroidWindow(holder);
                androidWindowInfo.InitializeDisplay();
                mode.Initialize(androidWindowInfo.Display, (int)GLVersion.ES3);
                androidWindowInfo.CreateSurface(mode.Config);

                windowInfo = androidWindowInfo;
                context = new AndroidGraphicsContext(mode, windowInfo, null, GLVersion.ES3, contextFlags);
            }
            else throw new Exception("NativeView is not an initialized AndroidSurfaceView. OpenGLES30 Context cannot be initialized.");
        }

        #endregion

    }
}
#endif