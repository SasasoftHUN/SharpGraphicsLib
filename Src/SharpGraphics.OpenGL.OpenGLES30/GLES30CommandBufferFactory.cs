using SharpGraphics.OpenGL.OpenGLES30.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30CommandBufferFactory : GLCommandBufferFactory
    {

        #region Fields

        private readonly GLES30CommandProcessor _gles30CommandProcessor;

        #endregion

        #region Constructors

        internal GLES30CommandBufferFactory(GLES30CommandProcessor commandProcessor) : base(commandProcessor)
            => _gles30CommandProcessor = commandProcessor;

        #endregion

        #region Public Methods

        public override GraphicsCommandBuffer CreateCommandBuffer(CommandBufferLevel level = CommandBufferLevel.Primary) => new GLES30CommandBufferList(_gles30CommandProcessor);
        public override GraphicsCommandBuffer[] CreateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary)
        {
            GLES30CommandBufferList[] buffers = new GLES30CommandBufferList[count];
            for (int i = 0; i < count; i++)
                buffers[i] = new GLES30CommandBufferList(_gles30CommandProcessor);
            return buffers;
        }

        #endregion

    }
}
