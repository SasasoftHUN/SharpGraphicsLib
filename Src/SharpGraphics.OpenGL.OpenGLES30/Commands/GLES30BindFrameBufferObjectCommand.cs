using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30BindFrameBufferObjectCommand : IGLCommand
    {

        private readonly int _id;

        internal GLES30BindFrameBufferObjectCommand(int id) => _id = id;

        public void Execute() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);

        public override string ToString() => $"Bind FBO (ID: {_id})";

    }
}
