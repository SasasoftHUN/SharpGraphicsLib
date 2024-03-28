using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL
{
    public abstract class GLGraphicsPipeline : GLPipeline, IGraphicsPipeline
    {

        #region Fields

        private readonly IGLShaderProgram[] _shaders;
        private readonly GLPipelineResourceLayout[]? _resourceLayouts; //Null is valid for optimization (not allocating empty array)
        private readonly int[]? _resourceLayoutStartingTextureUnits;
        private readonly ColorAttachmentUsage[]? _colorAttachmentUsages;

        protected int _programID;
        protected SortedDictionary<uint, int>? _samplerLocations;

        protected readonly RenderPassStep _renderPassStep;
        protected readonly int _fallbackColorAttachmentUsageIndex;

        protected readonly VertexInputs? _vertexInputs;
        protected readonly RasterizationOptions? _rasterisation;
        protected readonly DepthUsage _depthUsage;
        protected readonly bool _isBlend;

        #endregion

        #region Properties

        protected ReadOnlySpan<IGLShaderProgram> Shaders => _shaders;
        protected ReadOnlySpan<GLPipelineResourceLayout> ResourceLayouts => _resourceLayouts;
        protected ReadOnlySpan<ColorAttachmentUsage> ColorAttachmentUsages => _colorAttachmentUsages;

        public int ProgramID => _programID;

        public bool AreSamplerLocationsExplicit => _samplerLocations == null;
        public ReadOnlySpan<int> ResourceLayoutStartingTextureUnits => _resourceLayoutStartingTextureUnits;

        #endregion

        #region Constructors

        protected internal GLGraphicsPipeline(GLGraphicsDevice device, in GraphicsPipelineConstuctionParameters constuction): base(device)
        {
            ReadOnlySpan<IGraphicsShaderProgram> shaders = constuction.Shaders;
            _shaders = new IGLShaderProgram[shaders.Length];
            for (int i = 0; i < _shaders.Length; i++)
                _shaders[i] = Unsafe.As<IGLShaderProgram>(shaders[i]);

            _vertexInputs = constuction.vertexInputs;
            _rasterisation = constuction.rasterization;
            _depthUsage = constuction.depthUsage;

            ReadOnlySpan<PipelineResourceLayout> resourceLayouts = constuction.ResourceLayouts;
            if (resourceLayouts.Length == 0)
            {
                _resourceLayouts = null;
                _resourceLayoutStartingTextureUnits = null;
            }
            else
            {
                _resourceLayouts = new GLPipelineResourceLayout[resourceLayouts.Length];
                _resourceLayoutStartingTextureUnits = new int[resourceLayouts.Length];
                int usedTextureUnits = 0;

                for (int i = 0; i < _resourceLayouts.Length; i++)
                {
                    _resourceLayouts[i] = Unsafe.As<GLPipelineResourceLayout>(resourceLayouts[i]);
                    _resourceLayoutStartingTextureUnits[i] = usedTextureUnits;
                    usedTextureUnits += _resourceLayouts[i].UsedTextureUnits;
                }
            }

            ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = constuction.ColorAttachmentUsages;
            _colorAttachmentUsages = colorAttachmentUsages.Length > 0 ? colorAttachmentUsages.ToArray() : null; //Copy for safety
            _isBlend = false;
            for (int i = 0; i < colorAttachmentUsages.Length; i++)
                if (colorAttachmentUsages[i].blend.HasValue)
                    _isBlend = true;

            _renderPassStep = constuction.renderPass.Steps[(int)constuction.renderPassStep];
            _fallbackColorAttachmentUsageIndex = (int)constuction.fallbackColorAttachmentUsageIndex;
        }

        #endregion

        #region Public Methods

        public int GetVertexArrayStride(int binding) => _vertexInputs.HasValue ? (int)_vertexInputs.Value.Bindings[binding].stride : 0;
        public int GetVertexArrayStride(uint binding) => _vertexInputs.HasValue ? (int)_vertexInputs.Value.Bindings[(int)binding].stride : 0;

        public int GetSamplerLocation(uint binding) => _samplerLocations != null && _samplerLocations.TryGetValue(binding, out int location) ? location : -1;

        #endregion

    }
}
