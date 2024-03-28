using OpenTK.Graphics.ES30;
using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30GraphicsPipeline : GLGraphicsPipeline
    {

        #region Fields

#if ANDROID
        private readonly BeginMode _primitiveType;
#else
        private readonly PrimitiveType _primitiveType;
#endif

        #endregion

        #region Properties

#if ANDROID
        internal BeginMode GeometryType => _primitiveType;
#else
        internal PrimitiveType GeometryType => _primitiveType;
#endif
        internal VertexInputs? VertexInputs => _vertexInputs;
        [MemberNotNullWhen(returnValue: true, "VertexInputs")] internal bool HasVertexInputs => _vertexInputs.HasValue;

        #endregion

        #region Constructors

        internal GLES30GraphicsPipeline(GLES30GraphicsDevice device, in GraphicsPipelineConstuctionParameters constuction) : base(device, constuction)
        {
            _primitiveType = constuction.geometryType.ToPrimitiveType();
        }

        #endregion

        #region Private Methods

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BindUniformBlockIndex(int programID, uint uniformBinding, StringBuilder uniformNameBuilder)
        {
            uniformNameBuilder.Append($"uniformblock_{uniformBinding}_uniform"); //But... why? Why does the API need it this way???
            int uniformIndex = GL.GetUniformBlockIndex(programID, uniformNameBuilder);
            GL.UniformBlockBinding(programID, uniformIndex, (int)uniformBinding);
            uniformNameBuilder.Clear();
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BindUniformBlockIndex(int programID, uint uniformBinding)
        {
            string uniformName = $"uniformblock_{uniformBinding}_uniform";
            int uniformIndex = GL.GetUniformBlockIndex(programID, uniformName);
            GL.UniformBlockBinding(programID, uniformIndex, (int)uniformBinding);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GLBindVertexAttributes(int programID, VertexInputs vertexInputs)
        {
            foreach (VertexInputAttribute attribute in vertexInputs.Attributes)
                GL.BindAttribLocation(programID, (int)attribute.location, $"vsin_{attribute.location}");
        }

#endregion

        #region Public Methods

        public override void GLInitialize()
        {
            _programID = GL.CreateProgram();

            foreach (IGLShaderProgram shader in Shaders)
                GL.AttachShader(_programID, shader.ID);

            if (_vertexInputs.HasValue)
                GLBindVertexAttributes(_programID, _vertexInputs.Value);

            //Fragment Data locations are "bound" in the shader source

            GL.LinkProgram(_programID);

#if ANDROID
            GL.GetProgram(_programID, ProgramParameter.LinkStatus, out int result);
#else
            GL.GetProgram(_programID, GetProgramParameterName.LinkStatus, out int result);
#endif
            if (0 == result)
            {
                Debug.Fail(GL.GetProgramInfoLog(_programID));
                GL.DeleteProgram(_programID);
                _programID = 0;
                return;
            }

            ReadOnlySpan<GLPipelineResourceLayout> resourceLayouts = ResourceLayouts;
            if (resourceLayouts.Length > 0)
            {
                for (int j = 0; j < resourceLayouts.Length; j++)
                {
                    //Uniform Block Bindings
#if ANDROID
                    StringBuilder uniformNameBuilder = new StringBuilder();
#endif
                    foreach (uint uniformBinding in resourceLayouts[j].UniformBindings)
                    {
                        uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(uniformBinding);
#if ANDROID
                        BindUniformBlockIndex(_programID, uniqueBinding, uniformNameBuilder);
#else
                        BindUniformBlockIndex(_programID, uniqueBinding);
#endif
                    }
                    foreach (uint uniformBinding in resourceLayouts[j].DynamicUniformBindings)
                    {
                        uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(uniformBinding);
#if ANDROID
                        BindUniformBlockIndex(_programID, uniqueBinding, uniformNameBuilder);
#else
                        BindUniformBlockIndex(_programID, uniqueBinding);
#endif
                    }


                    //Sampler Locations
                    if (resourceLayouts[j].CombinedTextureSamplerBindings.Any() || resourceLayouts[j].InputAttachmentBindings.Any())
                    {
                        _samplerLocations = new SortedDictionary<uint, int>();
                        foreach (uint samplerBinding in resourceLayouts[j].CombinedTextureSamplerBindings)
                        {
                            uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(samplerBinding);
                            _samplerLocations[uniqueBinding] = GL.GetUniformLocation(_programID, $"uniformsampler_{uniqueBinding}");
                        }
                        foreach (uint samplerBinding in resourceLayouts[j].InputAttachmentBindings)
                        {
                            uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(samplerBinding);
                            _samplerLocations[uniqueBinding] = GL.GetUniformLocation(_programID, $"uniformsampler_{uniqueBinding}");
                        }
                    }
                }
            }
        }
        public override void GLFree()
        {
            if (_programID > 0)
            {
                GL.DeleteProgram(_programID);
                _programID = 0;
            }
        }

        public override void GLBind()
        {
            GL.UseProgram(_programID);

            //TODO: Bind empty VAO if "there is no VAO"

            //Rasterization
            if (_rasterisation.HasValue)
            {
                GL.Disable(EnableCap.RasterizerDiscard);
                RasterizationOptions rasterization = _rasterisation.Value;

                //Culling and Rasterization
                if (rasterization.cullMode != CullMode.None)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(rasterization.cullMode.ToCullFaceModeFlags());
                    GL.FrontFace(rasterization.frontFace.ToFrontFaceDirection());
                    //GL.PolygonMode not supported on GLES30
                }
                else GL.Disable(EnableCap.CullFace);

                //Depth Test
                if (_depthUsage.testEnabled)
                {
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthFunc(_depthUsage.comparison.ToDepthFunction());
                }
                else GL.Disable(EnableCap.DepthTest);
                GL.DepthMask(_depthUsage.write);

                //Color Mask and Blend
                ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = ColorAttachmentUsages;
                if (colorAttachmentUsages.Length > 0)
                {
                    ColorAttachmentUsage colorAttachmentUsage = colorAttachmentUsages[_fallbackColorAttachmentUsageIndex];

                    //Color Mask
                    ColorComponents colorWriteMask = colorAttachmentUsage.colorWriteMask;
                    GL.ColorMask(colorWriteMask.HasFlag(ColorComponents.Red), colorWriteMask.HasFlag(ColorComponents.Green), colorWriteMask.HasFlag(ColorComponents.Blue), colorWriteMask.HasFlag(ColorComponents.Alpha));

                    //Blend
                    if (colorAttachmentUsage.blend.HasValue)
                    {
                        BlendAttachment blend = colorAttachmentUsage.blend.Value;

                        GL.Enable(EnableCap.Blend);
                        if (_device.GLFeatures.IsSeparateBufferBlendSupported)
                        {
                            GL.BlendFuncSeparate(blend.sourceColorBlendFactor.ToBlendingFactorSrc(), blend.destinationColorBlendFactor.ToBlendingFactorDest(), blend.sourceAlphaBlendFactor.ToBlendingFactorSrc(), blend.destinationAlphaBlendFactor.ToBlendingFactorDest());
                            GL.BlendEquationSeparate(blend.colorBlendOperation.ToBlendEquationMode(), blend.alphaBlendOperation.ToBlendEquationMode());
                        }
                        else
                        {
                            GL.BlendFunc(blend.sourceColorBlendFactor.ToBlendingFactorSrc(), blend.destinationColorBlendFactor.ToBlendingFactorDest());
                            GL.BlendEquation(blend.colorBlendOperation.ToBlendEquationMode());
                        }
                    }
                    else GL.Disable(EnableCap.Blend);
                }
                else
                {
                    GL.ColorMask(true, true, true, true);
                    GL.Disable(EnableCap.Blend);
                }
            }
            else GL.Enable(EnableCap.RasterizerDiscard);
        }

        #endregion

    }
}
