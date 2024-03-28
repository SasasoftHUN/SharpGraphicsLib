using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreFinishCommand : IGLCommand
    {

        internal GLCoreFinishCommand() { }

        public void Execute() => GL.Finish();

        public override string ToString() => "Finish";

    }
}
