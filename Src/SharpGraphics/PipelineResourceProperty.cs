using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public enum PipelineResourceType
    {
        //Sampler = 0,
        CombinedTextureSampler = 1,
        //SampledImage = 2,
        //StorageImage = 3,
        //UniformTexelBuffer = 4,
        //StorageTexelBuffer = 5,
        UniformBuffer = 6,
        //StorageBuffer = 7,
        UniformBufferDynamic = 8,
        //StorageBufferDynamic = 9,
        InputAttachment = 10,
        //InlineUniformBlockEXT,
        //AccelerationStructureNV
    }

    public readonly struct PipelineResourceProperty : IEquatable<PipelineResourceProperty>
    {

        #region Fields

        public readonly uint binding;
        public readonly uint uniqueBinding;
        public readonly PipelineResourceType type;
        public readonly GraphicsShaderStages stage;

        #endregion

        #region Constructors

        public PipelineResourceProperty(uint uniqueBinding, PipelineResourceType type, GraphicsShaderStages stage)
        {
            this.binding = uniqueBinding;
            this.uniqueBinding = uniqueBinding;
            this.type = type;
            this.stage = stage;
        }
        public PipelineResourceProperty(uint binding, uint uniqueBinding, PipelineResourceType type, GraphicsShaderStages stage)
        {
            this.binding = binding;
            this.uniqueBinding = uniqueBinding;
            this.type = type;
            this.stage = stage;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object? obj) => obj is PipelineResourceProperty property && Equals(property);
        public bool Equals(PipelineResourceProperty other)
            => binding == other.binding &&
                uniqueBinding == other.uniqueBinding &&
                type == other.type &&
                stage == other.stage;
        public bool Equals(in PipelineResourceProperty other)
            => binding == other.binding &&
                uniqueBinding == other.uniqueBinding &&
                type == other.type &&
                stage == other.stage;
        public override int GetHashCode() => HashCode.Combine(binding, uniqueBinding, type, stage);

        #endregion

        #region Operators

        public static bool operator ==(PipelineResourceProperty left, PipelineResourceProperty right) => left.Equals(right);
        public static bool operator ==(in PipelineResourceProperty left, in PipelineResourceProperty right) => left.Equals(right);
        public static bool operator !=(PipelineResourceProperty left, PipelineResourceProperty right) => !(left == right);
        public static bool operator !=(in PipelineResourceProperty left, in PipelineResourceProperty right) => !(left == right);

        #endregion

    }
}
