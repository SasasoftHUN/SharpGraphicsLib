using SharpGraphics.OpenGL.OpenGLES30.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30CommandProcessor : GLCommandProcessor
    {

        #region Fields

        private readonly GLES30GraphicsDevice _gles30Device;

        #endregion

        #region Properties

        internal GLES30GraphicsDevice GLES30Device => _gles30Device;

        #endregion

        #region Constructors

        internal GLES30CommandProcessor(GLES30GraphicsDevice device) : base(device)
            => _gles30Device = device;

        #endregion

        #region Public Methods

        public override CommandBufferFactory CreateCommandBufferFactory(CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default) => new GLES30CommandBufferFactory(this);
        public override CommandBufferFactory[] CreateCommandBufferFactories(uint count, CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default)
        {
            GLES30CommandBufferFactory[] factories = new GLES30CommandBufferFactory[count];
            for (int i = 0; i < count; i++)
                factories[i] = new GLES30CommandBufferFactory(this);
            return factories;
        }

        public override void WaitForIdle() => Device.WaitForIdle();

        #endregion

    }
}
