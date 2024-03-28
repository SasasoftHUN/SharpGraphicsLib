using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreDrawIndexedCommand : IGLCommand
    {

        private readonly PrimitiveType _geometryType; //BeginMode in OpenGL ES
        private readonly int _count;
        private readonly DrawElementsType _elementsType;
        private readonly int _first;

        internal GLCoreDrawIndexedCommand(PrimitiveType geometryType, int count, DrawElementsType elementsType, int first)
        {
            _geometryType = geometryType;
            _count = count;
            _elementsType = elementsType;
            _first = first;
        }

        public void Execute() => GL.DrawElements(_geometryType, _count, _elementsType, _first);

        public override string ToString() => $"Draw Elements ({_geometryType}, {_elementsType}, First: {_first}, Count: {_count})";

    }
}
