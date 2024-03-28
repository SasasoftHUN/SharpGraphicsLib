using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLCommandProcessor : GraphicsCommandProcessor
    {

        #region Fields

        private GLGraphicsDevice _device;

        protected readonly CommandBufferFactory _commandBufferFactory;

        #endregion

        #region Properties

        protected internal GLGraphicsDevice Device => _device;

        public override CommandBufferFactory CommandBufferFactory => _commandBufferFactory;

        #endregion

        #region Constructors

        protected internal GLCommandProcessor(GLGraphicsDevice device) : base(device.DeviceInfo.CommandProcessorGroups[0].Type, 1f)
        {
             _device = device;
            _commandBufferFactory = CreateCommandBufferFactory();
        }

        #endregion

        #region Public Methods

        public void Submit(IGLCommand command) => _device.SubmitCommand(command);
        public override void Submit(GraphicsCommandBuffer commandBuffer)
            => _device.SubmitCommand(Unsafe.As<IGLCommandBuffer>(commandBuffer));

        #endregion

    }
}
