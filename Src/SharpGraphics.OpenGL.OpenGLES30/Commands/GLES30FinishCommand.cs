using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30FinishCommand : IGLCommand
    {

        internal GLES30FinishCommand() { }

        public void Execute() => GL.Finish();

        public override string ToString() => "Finish";

    }
}
