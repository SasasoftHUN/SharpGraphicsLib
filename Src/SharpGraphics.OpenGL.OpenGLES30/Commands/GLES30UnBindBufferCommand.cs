using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30UnBindBufferCommand : IGLCommand
    {

        private readonly BufferTarget _target;

        internal GLES30UnBindBufferCommand(BufferTarget target) => _target = target;

        public void Execute() => GL.BindBuffer(_target, 0);

        public override string ToString() => $"UnBind {_target}";

    }
}
