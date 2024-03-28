using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGraphics.Vulkan
{
    internal class VulkanGraphicsShaderProgram : VulkanShaderProgram, IGraphicsShaderProgram
    {

        #region Fields

        private readonly GraphicsShaderStages _stage;

        #endregion

        #region Properties

        public GraphicsShaderStages Stage => _stage;

        #endregion

        #region Constructors

        internal VulkanGraphicsShaderProgram(VulkanGraphicsDevice device, ShaderSourceBinary shaderSource, GraphicsShaderStages stage) : base(device, shaderSource, stage.ToVkShaderStageFlags())
            => _stage = stage;

        #endregion

    }
}
