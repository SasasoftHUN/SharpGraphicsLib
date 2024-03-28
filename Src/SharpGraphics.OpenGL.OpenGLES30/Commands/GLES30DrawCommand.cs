using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30DrawCommand : IGLCommand
    {

#if ANDROID
        private readonly BeginMode _geometryType;
#else
        private readonly PrimitiveType _geometryType;
#endif
        private readonly int _first;
        private readonly int _count;

        internal GLES30DrawCommand(
#if ANDROID
                BeginMode geometryType,
#else
                PrimitiveType geometryType,
#endif
                int first, int count)
        {
            _geometryType = geometryType;
            _first = first;
            _count = count;
        }

        public void Execute() => GL.DrawArrays(_geometryType, _first, _count);

        public override string ToString() => $"Draw Arrays ({_geometryType}, First: {_first}, Count: {_count})";

    }
}
