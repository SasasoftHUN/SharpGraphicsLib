using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.GraphicsViews;

namespace SharpGraphics.OpenGL.Contexts
{

    public abstract class LinuxContextFactory
    {

        #region Protected Methods

        protected abstract X11Context CreateX11Context();
        protected abstract X11Context CreateX11Context(LinuxX11SpecificViewInfo x11ViewInfo, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext);

        #endregion

        #region Public Methods

        public IGLContext CreateContext()
        {
            if (X11Context.IsAvailable())
                return CreateX11Context();
            else throw new LinuxGraphicsDeviceCreationException("No supported Window System has been found!"); //TODO: Support Wayland and Mir
        }

        public IGLContext CreateContext(IGraphicsView graphicsView, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext)
        {
            if (X11Context.IsAvailable() && graphicsView.PlatformSpecificViewInfo is LinuxX11SpecificViewInfo x11ViewInfo)
                return CreateX11Context(x11ViewInfo, contextVersionRequests, debugContext);
            else throw new LinuxGraphicsDeviceCreationException("No supported Window System has been found!"); //TODO: Support Wayland and Mir
        }

        //Wayland Env Variable: WAYLAND_DISPLAY
        //Mir Env Variable: MIR_SOCKET
        //Others: DirectFB, Weston, KWin, Mutter, Openbox

        #endregion

    }

    public class LinuxGraphicsDeviceCreationException : GLGraphicsDeviceCreationException
    {

        protected internal LinuxGraphicsDeviceCreationException(string creationStepName) : base(creationStepName) { }

    }

}
