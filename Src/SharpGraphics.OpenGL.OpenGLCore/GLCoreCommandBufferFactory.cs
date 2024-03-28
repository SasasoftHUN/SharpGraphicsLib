using SharpGraphics.OpenGL.OpenGLCore.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreCommandBufferFactory : GLCommandBufferFactory
    {

        #region Constructors

        internal GLCoreCommandBufferFactory(GLCoreCommandProcessor commandProcessor) : base(commandProcessor) { }

        #endregion

        #region Public Methods

        public override GraphicsCommandBuffer CreateCommandBuffer(CommandBufferLevel level = CommandBufferLevel.Primary) => new GLCoreCommandBufferList(_commandProcessor);
        public override GraphicsCommandBuffer[] CreateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary)
        {
            GLCoreCommandBufferList[] buffers = new GLCoreCommandBufferList[count];
            for (int i = 0; i < count; i++)
                buffers[i] = new GLCoreCommandBufferList(_commandProcessor);
            return buffers;
        }

        #endregion

    }
}
