using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public class GLPipelineResourceLayout : PipelineResourceLayout
    {

        #region Fields

        protected readonly SortedDictionary<uint, uint> _uniqueBindings = new SortedDictionary<uint, uint>();
        protected readonly SortedSet<uint> _uniformBindings = new SortedSet<uint>();
        protected readonly SortedSet<uint> _dynamicUniformBindings = new SortedSet<uint>();
        protected readonly SortedSet<uint> _combinedTextureSamplerBindings = new SortedSet<uint>();
        protected readonly SortedSet<uint> _inputAttachmentBindings = new SortedSet<uint>();

        protected readonly int _usedTextureUnits = 0;

        #endregion

        #region Properties

        public IReadOnlyDictionary<uint, uint> UniqueBindings => _uniqueBindings;
        public IEnumerable<uint> UniformBindings => _uniformBindings;
        public IEnumerable<uint> DynamicUniformBindings => _dynamicUniformBindings;
        public IEnumerable<uint> CombinedTextureSamplerBindings => _combinedTextureSamplerBindings;
        public IEnumerable<uint> InputAttachmentBindings => _inputAttachmentBindings;
        public int UsedTextureUnits => _usedTextureUnits;

        #endregion

        #region Constructors

        protected internal GLPipelineResourceLayout(in PipelineResourceProperties resourceProperties)
        {
            AssertLayout(resourceProperties);
            
            ReadOnlySpan<PipelineResourceProperty> properties = resourceProperties.Properties;
            for (int i = 0; i < properties.Length; i++)
            {
                _uniqueBindings[properties[i].binding] = properties[i].uniqueBinding;
                switch (properties[i].type)
                {
                    case PipelineResourceType.UniformBuffer: _uniformBindings.Add(properties[i].binding); break;
                    case PipelineResourceType.UniformBufferDynamic: _dynamicUniformBindings.Add(properties[i].binding); break;

                    case PipelineResourceType.CombinedTextureSampler: _combinedTextureSamplerBindings.Add(properties[i].binding); ++_usedTextureUnits; break;
                    case PipelineResourceType.InputAttachment: _inputAttachmentBindings.Add(properties[i].binding); ++_usedTextureUnits; break;
                }
            }
        }

        #endregion

        #region Public Methods

        public override PipelineResource CreateResource() => new GLPipelineResource(this);
        public override PipelineResource[] CreateResources(uint count)
        {
            PipelineResource[] result = new PipelineResource[count];
            for (int i = 0; i < count; i++)
                result[i] = new GLPipelineResource(this);
            return result;
        }

        public uint GetUniqueBinding(uint binding) => _uniqueBindings[binding];

        #endregion

    }
}
