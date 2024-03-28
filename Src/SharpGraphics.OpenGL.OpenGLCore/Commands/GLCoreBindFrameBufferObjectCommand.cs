using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreBindFrameBufferObjectCommand : IGLCommand
    {

        private readonly int _id;

        internal GLCoreBindFrameBufferObjectCommand(int id) => _id = id;

        public void Execute() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);

        public override string ToString() => $"Bind FBO (ID: {_id})";

    }
}
