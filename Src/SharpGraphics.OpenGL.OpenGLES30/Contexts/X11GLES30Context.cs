using SharpGraphics.OpenGL.Contexts;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
#if OPENTK4
using OpenTK.Windowing.GraphicsLibraryFramework;
#endif

namespace SharpGraphics.OpenGL.OpenGLES30.Contexts
{
    internal class X11GLES30Context : X11Context
#if OPENTK4
        , IBindingsContext
#endif
    {

        #region Constructors

        internal X11GLES30Context() { }
        internal X11GLES30Context(LinuxX11SpecificViewInfo x11ViewInfo, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext) : base(x11ViewInfo, contextVersionRequests, debugContext) { }

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
