using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Shaders
{
    public abstract class FragmentShaderBase : ShaderBase
    {

#if NETUNIFIED
        [RequiresUnreferencedCode("SharpGraphics.GraphicsDevice.CreateShaderSource will call this through Generics")]
        protected internal FragmentShaderBase(): base() { }
#endif

        protected void Discard() { }

    }
}
