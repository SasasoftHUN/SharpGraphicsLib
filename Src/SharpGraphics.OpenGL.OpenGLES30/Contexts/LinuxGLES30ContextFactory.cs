using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Contexts;

namespace SharpGraphics.OpenGL.OpenGLES30.Contexts
{
    internal class LinuxGLES30ContextFactory : LinuxContextFactory
    {

        #region Constructors

        internal LinuxGLES30ContextFactory() { }

        #endregion

        #region Protected Methods

        protected override X11Context CreateX11Context() => new X11GLES30Context();
        protected override X11Context CreateX11Context(LinuxX11SpecificViewInfo x11ViewInfo, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext) => new X11GLES30Context(x11ViewInfo, contextVersionRequests, debugContext);

        #endregion

    }
}
