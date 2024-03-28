using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{
    internal sealed class GLES30RestoreDefaultPipelineStateCommand : IGLCommand
    {

        private readonly bool _disableScissor;

        internal GLES30RestoreDefaultPipelineStateCommand(bool disableScissor)
        {
            _disableScissor = disableScissor;
        }

        public void Execute()
        {
            GL.UseProgram(0);

            GL.BindVertexArray(0);

            //No need to restore cull options because it will automatically set these on the next Pipeline Bind.
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);
            //GL.FrontFace(FrontFaceDirection.Ccw);
            //GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            //No need to restore depth options because it will automatically set these on the next Pipeline Bind.
            //GL.Enable(EnableCap.DepthTest);
            //GL.DepthFunc(_depthUsage.comparison.ToDepthFunction());
            //GL.DepthMask(_depthUsage.write);

            if (_disableScissor)
                GL.Disable(EnableCap.ScissorTest);
        }

        public override string ToString() => "Restore Default Pipeline State";

    }
}
