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

namespace SharpGraphics.OpenGL.OpenGLCore.Contexts
{
    internal class LinuxCoreContextFactory : LinuxContextFactory
    {

        #region Constructors

        internal LinuxCoreContextFactory() { }

        #endregion

        #region Protected Methods

        protected override X11Context CreateX11Context() => new X11CoreContext();
        protected override X11Context CreateX11Context(LinuxX11SpecificViewInfo x11ViewInfo, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext) => new X11CoreContext(x11ViewInfo, contextVersionRequests, debugContext);

        #endregion

    }
}
