using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30DrawIndexedCommand : IGLCommand
    {

#if ANDROID
        private readonly BeginMode _geometryType;
#else
        private readonly PrimitiveType _geometryType;
#endif
        private readonly int _count;
        private readonly DrawElementsType _elementsType;
        private readonly IntPtr _first;

        internal GLES30DrawIndexedCommand(
#if ANDROID
                BeginMode geometryType,
#else
                PrimitiveType geometryType,
#endif
               int count, DrawElementsType elementsType, IntPtr first)
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
