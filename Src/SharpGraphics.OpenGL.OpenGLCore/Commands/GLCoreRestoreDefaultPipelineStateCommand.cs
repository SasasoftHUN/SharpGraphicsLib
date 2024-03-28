using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreRestoreDefaultPipelineStateCommand : IGLCommand
    {

        private readonly GLCoreGraphicsPipeline _pipeline;
        private readonly bool _disableScissor;

        internal GLCoreRestoreDefaultPipelineStateCommand(GLCoreGraphicsPipeline pipeline, bool disableScissor)
        {
            _pipeline = pipeline;
            _disableScissor = disableScissor;
        }

        public void Execute() //TODO: Get Pipeline and use it's properties to restore config
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
