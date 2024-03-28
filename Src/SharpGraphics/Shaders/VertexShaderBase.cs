using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Shaders
{
    public abstract class VertexShaderBase : ShaderBase
    {
#if NETUNIFIED
        [RequiresUnreferencedCode("SharpGraphics.GraphicsDevice.CreateShaderSource will call this through Generics")]
        protected internal VertexShaderBase() : base() { }
#endif
    }
}
