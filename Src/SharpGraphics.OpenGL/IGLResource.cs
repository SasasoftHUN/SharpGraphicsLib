using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public interface IGLResource
    {
        void GLInitialize();
        void GLFree();
    }
}
