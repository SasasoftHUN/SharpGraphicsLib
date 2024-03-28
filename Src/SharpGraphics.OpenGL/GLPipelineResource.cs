using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public class GLPipelineResource : PipelineResource
    {

        public readonly struct UniformBuffer
        {
            public readonly uint binding;
            public readonly IGLDataBuffer buffer;

            public UniformBuffer(uint binding, IGLDataBuffer buffer)
            {
                this.binding = binding;
                this.buffer = buffer;
            }
        }
        public readonly struct UniformDynamicBuffer
        {
            public readonly UniformBuffer buffer;
            public readonly uint elementOffset;

            public UniformDynamicBuffer(in UniformBuffer buffer, uint elementOffset)
            {
                this.buffer = buffer;
                this.elementOffset = elementOffset;
            }
        }
        public readonly struct CombinedTextureSampler
        {
            public readonly uint binding;
            public readonly GLTextureSampler sampler;
            public readonly IGLTexture texture;

            public CombinedTextureSampler(uint binding, GLTextureSampler sampler, IGLTexture texture)
            {
                this.binding = binding;
                this.sampler = sampler;
                this.texture = texture;
            }
        }
        public readonly struct InputAttachment
        {
            public readonly uint binding;
            public readonly IGLTexture attachment;

            public InputAttachment(uint binding, IGLTexture attachment)
            {
                this.binding = binding;
                this.attachment = attachment;
            }
        }

        #region Fields

        private readonly IReadOnlyDictionary<uint, uint> _uniqueBindings;

        protected readonly SortedDictionary<uint, int> _bindingIndices;

        protected readonly UniformBuffer[]? _uniformBuffers;
        protected readonly UniformDynamicBuffer[]? _uniformDynamicBuffers;
        protected readonly CombinedTextureSampler[]? _combinedTextureSamplers;
        protected readonly InputAttachment[]? _inputAttachments;

        #endregion

        #region Constructors

        protected internal GLPipelineResource(GLPipelineResourceLayout layout)
        {
            _uniqueBindings = layout.UniqueBindings;
            _bindingIndices = new SortedDictionary<uint, int>();
            _uniformBuffers = CreateBindingArray<UniformBuffer>(layout.UniformBindings, _bindingIndices);
            _uniformDynamicBuffers = CreateBindingArray<UniformDynamicBuffer>(layout.DynamicUniformBindings, _bindingIndices);
            _combinedTextureSamplers = CreateBindingArray<CombinedTextureSampler>(layout.CombinedTextureSamplerBindings, _bindingIndices);
            _inputAttachments = CreateBindingArray<InputAttachment>(layout.InputAttachmentBindings, _bindingIndices);
        }

        #endregion

        #region Private Methods

        private static T[]? CreateBindingArray<T>(IEnumerable<uint> bindings, SortedDictionary<uint, int> bindingIndices)
        {
            if (bindings.Any())
            {
                int i = 0;
                foreach (uint binding in bindings)
                    bindingIndices[binding] = i++;
                return new T[bindings.Count()];
            }
            else return null;
        }

        #endregion

        #region Protected Methods

        protected override void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer)
            => BindResource(pipeline, set, commandBuffer, 0);
        protected override void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer, int dataIndex)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (_uniformBuffers != null)
                for (int i = 0; i < _uniformBuffers.Length; i++)
                    glCommandBuffer.AddCommand(new GLBindUniformBufferCommand(_uniformBuffers[i].binding, _uniformBuffers[i].buffer));
            if (_uniformDynamicBuffers != null)
                for (int i = 0; i < _uniformDynamicBuffers.Length; i++)
                    glCommandBuffer.AddCommand(new GLBindUniformDynamicBufferCommand(_uniformDynamicBuffers[i].buffer.binding, _uniformDynamicBuffers[i].buffer.buffer, _uniformDynamicBuffers[i].elementOffset, dataIndex));

            BindResourceTextures(pipeline, set, glCommandBuffer);
        }

        protected virtual void BindResourceTextures(IPipeline pipeline, uint set, GLCommandBufferList glCommandBuffer)
        {
            GLGraphicsPipeline glPipeline = Unsafe.As<GLGraphicsPipeline>(pipeline);

            int textureUnit = glPipeline.ResourceLayoutStartingTextureUnits[(int)set];
            if (_combinedTextureSamplers != null)
                for (int i = 0; i < _combinedTextureSamplers.Length; i++)
                {
                    glCommandBuffer.AddCommand(new GLBindCombinedTextureSamplerCommand(
                        glPipeline.AreSamplerLocationsExplicit ? (int)_combinedTextureSamplers[i].binding : glPipeline.GetSamplerLocation(_combinedTextureSamplers[i].binding),
                        textureUnit, _combinedTextureSamplers[i].sampler, _combinedTextureSamplers[i].texture));
                    glCommandBuffer.AddResourceStateRestoreCommand(set, new GLUnBindCombinedTextureSamplerCommand(textureUnit++, _combinedTextureSamplers[i].sampler, _combinedTextureSamplers[i].texture));
                }
            if (_inputAttachments != null)
                for (int i = 0; i < _inputAttachments.Length; i++)
                {
                    glCommandBuffer.AddCommand(new GLBindTextureCommand(
                        glPipeline.AreSamplerLocationsExplicit ? (int)_inputAttachments[i].binding : glPipeline.GetSamplerLocation(_inputAttachments[i].binding),
                        textureUnit, _inputAttachments[i].attachment));
                    glCommandBuffer.AddResourceStateRestoreCommand(set, new GLUnBindTextureCommand(_inputAttachments[i].attachment, textureUnit++));
                }
        }

        #endregion

        #region Public Methods

        public override void BindUniformBuffer(uint binding, IDataBuffer buffer)
        {
            if (_uniformBuffers == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Uniform Buffer bindings!");
            Debug.Assert(_bindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain binding {binding}!");
            _uniformBuffers[_bindingIndices[binding]] = new UniformBuffer(_uniqueBindings[binding], Unsafe.As<IGLDataBuffer>(buffer));
        }
        public override void BindUniformBufferDynamic(uint binding, IDataBuffer buffer, uint elementOffset)
        {
            if (_uniformDynamicBuffers == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Dynamic Uniform Buffer bindings!");
            Debug.Assert(_bindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain binding {binding}!");
            _uniformDynamicBuffers[_bindingIndices[binding]] = new UniformDynamicBuffer(new UniformBuffer(_uniqueBindings[binding], Unsafe.As<IGLDataBuffer>(buffer)), elementOffset);
        }

        public override void BindTexture(uint binding, TextureSampler sampler, ITexture texture)
        {
            if (_combinedTextureSamplers == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Uniform Buffer bindings!");
            Debug.Assert(_bindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain binding {binding}!");
            _combinedTextureSamplers[_bindingIndices[binding]] = new CombinedTextureSampler(_uniqueBindings[binding], Unsafe.As<GLTextureSampler>(sampler), Unsafe.As<IGLTexture>(texture));
        }

        public override void BindInputAttachments(uint binding, ITexture attachment)
        {
            if (_inputAttachments == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Input Attachment bindings!");
            Debug.Assert(_bindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain binding {binding}!");
            _inputAttachments[_bindingIndices[binding]] = new InputAttachment(_uniqueBindings[binding], Unsafe.As<IGLTexture>(attachment));
        }
        public override void BindInputAttachments(in RenderPassStep step, IFrameBuffer frameBuffer)
        {
            if (_inputAttachments == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Input Attachment bindings!");

            ReadOnlySpan<uint> inputAttachmentIndices = step.InputAttachmentIndices;
            if (inputAttachmentIndices.Length > 0)
            {
                for (int i = 0; i < inputAttachmentIndices.Length; i++)
                {
                    uint binding = (uint)i;
                    Debug.Assert(_bindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain binding {binding}!");
                    _inputAttachments[_bindingIndices[binding]] = new InputAttachment(_uniqueBindings[binding], Unsafe.As<IGLTexture>(frameBuffer.GetAttachment(inputAttachmentIndices[i])));
                }
            }
        }

        #endregion

    }
}
