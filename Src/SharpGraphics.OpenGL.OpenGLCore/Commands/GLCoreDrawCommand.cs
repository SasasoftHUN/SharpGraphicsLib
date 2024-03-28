using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreDrawCommand : IGLCommand
    {

        private readonly PrimitiveType _geometryType; //BeginMode in OpenGL ES
        private readonly int _first;
        private readonly int _count;

        internal GLCoreDrawCommand(PrimitiveType geometryType, int first, int count)
        {
            _geometryType = geometryType;
            _first = first;
            _count = count;
        }

        public void Execute() => GL.DrawArrays(_geometryType, _first, _count);

        public override string ToString() => $"Draw Arrays ({_geometryType}, First: {_first}, Count: {_count})";

    }
}
