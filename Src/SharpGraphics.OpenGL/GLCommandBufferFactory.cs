using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLCommandBufferFactory : CommandBufferFactory
    {

        #region Fields

        protected readonly GLCommandProcessor _commandProcessor;

        #endregion

        #region Constructors

        protected GLCommandBufferFactory(GLCommandProcessor commandProcessor) => _commandProcessor = commandProcessor;

        #endregion

    }
}
