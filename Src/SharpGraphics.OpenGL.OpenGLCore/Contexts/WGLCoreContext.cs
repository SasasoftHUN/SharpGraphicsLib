using OpenTK;
#if OPENTK4
using OpenTK.Windowing.GraphicsLibraryFramework;
#endif
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static SharpGraphics.OpenGL.Contexts.WGLContext;

namespace SharpGraphics.OpenGL.OpenGLCore.Contexts
{
    internal class WGLCoreContext : WGLContext
#if OPENTK4
        , IBindingsContext
#endif
    {

        #region Properties

        public override int SwapInterval
        {
            set
            {
#if OPENTK4
                OpenTK.Graphics.Wgl.Wgl.Ext.SwapInterval(value);
#else
                //TODO: Set SwapInterval pre OpenTK4
#endif
            }
        }

        #endregion

        #region Constructors

        internal WGLCoreContext() { }
        internal WGLCoreContext(IGraphicsView graphicsView, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext, WGLContextCreationRequest? contextCreationRequest) : base(graphicsView, contextVersionRequests, debugContext, contextCreationRequest)
        {
            graphicsView.SwapChainConstructionRequest.mode.ToSwapInterval(out int swapInterval, out _);
#if OPENTK4
            OpenTK.Graphics.Wgl.Wgl.Ext.SwapInterval(swapInterval);
#else
            //TODO: Set SwapInterval pre OpenTK4
#endif
        }

        #endregion

        #region Protected Methods

        protected override void LoadBindings()
        {
#if OPENTK4
            OpenTK.Graphics.ES11.GL.LoadBindings(this);
            OpenTK.Graphics.ES20.GL.LoadBindings(this);
            OpenTK.Graphics.ES30.GL.LoadBindings(this);
            OpenTK.Graphics.OpenGL.GL.LoadBindings(this);
            OpenTK.Graphics.OpenGL4.GL.LoadBindings(this);
            OpenTK.Graphics.Wgl.Wgl.LoadBindings(this);
#else
            //TODO: Load Bindings pre OpenTK4
#endif
        }

        #endregion

    }
}
