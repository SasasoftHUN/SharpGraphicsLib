using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Shaders
{
    public abstract class ShaderBase
    {
#if NETUNIFIED
        [RequiresUnreferencedCode("SharpGraphics.GraphicsDevice.CreateShaderSource will call this through Generics")]
        protected internal ShaderBase() { }
#endif

        public abstract bool TryGetSourceText(string backendName, [NotNullWhen(returnValue: true)] out string? sourceText);
        public abstract bool TryGetSourceBinary(string backendName, out ReadOnlyMemory<byte> sourceBytes);

        public abstract void Main();

    }
}
