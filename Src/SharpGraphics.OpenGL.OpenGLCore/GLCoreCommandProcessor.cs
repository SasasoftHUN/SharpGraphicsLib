using SharpGraphics.OpenGL.OpenGLCore.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreCommandProcessor : GLCommandProcessor
    {

        #region Constructors

        internal GLCoreCommandProcessor(GLCoreGraphicsDevice device) : base(device) { }

        #endregion

        #region Public Methods

        public override CommandBufferFactory CreateCommandBufferFactory(CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default) => new GLCoreCommandBufferFactory(this);
        public override CommandBufferFactory[] CreateCommandBufferFactories(uint count, CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default)
        {
            GLCoreCommandBufferFactory[] factories = new GLCoreCommandBufferFactory[count];
            for (int i = 0; i < count; i++)
                factories[i] = new GLCoreCommandBufferFactory(this);
            return factories;
        }

        public override void WaitForIdle() => Device.WaitForIdle();

        #endregion

    }
}
