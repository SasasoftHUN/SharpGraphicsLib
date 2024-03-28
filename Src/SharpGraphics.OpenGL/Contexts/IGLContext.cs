using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Contexts
{

    public readonly struct GLContextVersion
    {
        public readonly byte major;
        public readonly byte minor;
        public readonly bool forwardCompatible;
        public readonly bool coreProfile;

        public GLContextVersion(byte major, byte minor)
        {
            this.major = major;
            this.minor = minor;
            if (major >= 3)
            {
                forwardCompatible = true;
                if (major > 3 || minor >= 2)
                    coreProfile = true;
            }
        }
        public GLContextVersion(byte major, byte minor, bool forwardCompatible, bool coreProfile)
        {
            this.major = major;
            this.minor = minor;
            this.forwardCompatible = forwardCompatible;
            this.coreProfile = coreProfile;
        }
    }

    public interface IGLContextCreationRequest { }

    public interface IGLContext : IDisposable
    {

        int SwapInterval { set; }

        void Bind();
        void UnBind();
        void SwapBuffers();

    }
}
