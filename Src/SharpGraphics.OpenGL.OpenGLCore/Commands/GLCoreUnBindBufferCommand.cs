using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreUnBindBufferCommand : IGLCommand
    {

        private readonly BufferTarget _target;

        internal GLCoreUnBindBufferCommand(BufferTarget target) => _target = target;

        public void Execute() => GL.BindBuffer(_target, 0);

        public override string ToString() => $"UnBind {_target}";

    }
}
