using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanPipelineResource : PipelineResource
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;
        private readonly VulkanPipelineResourceLayout _layout;

        private Vk.VkDescriptorSet _set;
        private readonly PinnedObjectHandle<Vk.VkDescriptorSet> _pinnedSet;
        private readonly int _poolIndex;

        private readonly RawList<Vk.VkDescriptorImageInfo> _imageUpdates;
        private readonly RawList<Vk.VkDescriptorBufferInfo> _bufferUpdates;
        private readonly RawList<Vk.VkWriteDescriptorSet> _updates;
        private bool _isUpdateNeeded = false;

        private readonly SortedDictionary<uint, int>? _dynamicBindingIndices;
        private readonly uint[]? _dynamicSizes;

        #endregion

        #region Properties

        internal ref Vk.VkDescriptorSet Set => ref _set;
        internal IntPtr SetPtr => _pinnedSet.pointer;
        internal int PoolIndex => _poolIndex;

        #endregion

        #region Constructors

        internal VulkanPipelineResource(VulkanGraphicsDevice device, VulkanPipelineResourceLayout layout, Vk.VkDescriptorSet set, int poolIndex)
        {
            _device = device;
            _layout = layout;
            _set = set;
            _pinnedSet = new PinnedObjectHandle<Vk.VkDescriptorSet>(ref _set);
            _poolIndex = poolIndex;

            _imageUpdates = new RawList<Vk.VkDescriptorImageInfo>();
            _bufferUpdates = new RawList<Vk.VkDescriptorBufferInfo>();
            _updates = new RawList<Vk.VkWriteDescriptorSet>();

            if (layout.HasDynamicBindings)
            {
                _dynamicBindingIndices = new SortedDictionary<uint, int>();
                int i = 0;
                foreach (uint binding in layout.DynamicBindings)
                    _dynamicBindingIndices[binding] = i++;
                _dynamicSizes = new uint[_dynamicBindingIndices.Count];
            }
            else
            {
                _dynamicBindingIndices = null;
                _dynamicSizes = null;
            }
        }

        ~VulkanPipelineResource() => Dispose(false);

        #endregion

        #region Private Methods
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBindings()
        {
            if (_isUpdateNeeded)
            {
                VK.vkUpdateDescriptorSets(_device.Device, _updates.Count, _updates.Pointer, 0u, IntPtr.Zero);
                _updates.Clear();
                _imageUpdates.Clear();
                _bufferUpdates.Clear();
                _isUpdateNeeded = false;
            }
        }

        #endregion

        #region Protected Methods

        protected override void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer)
        {
            VulkanPipeline vkPipeline = Unsafe.As<VulkanPipeline>(pipeline);
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);

            UpdateBindings();

            if (_dynamicSizes == null)
                VK.vkCmdBindDescriptorSets(vkCommandBuffer.CommandBuffer, vkPipeline.BindPoint, vkPipeline.Layout, set, 1u, _pinnedSet.pointer, 0u, IntPtr.Zero);
            else VK.vkCmdBindDescriptorSets(vkCommandBuffer.CommandBuffer, vkPipeline.BindPoint, vkPipeline.Layout, set, 1u, _pinnedSet.pointer, (uint)_dynamicSizes.Length, ref _dynamicSizes[0]);
        }
        protected override void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer, int dataIndex)
        {
            VulkanPipeline vkPipeline = Unsafe.As<VulkanPipeline>(pipeline);
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);

            UpdateBindings();

            if (_dynamicSizes == null)
                VK.vkCmdBindDescriptorSets(vkCommandBuffer.CommandBuffer, vkPipeline.BindPoint, vkPipeline.Layout, set, 1u, _pinnedSet.pointer, 0u, IntPtr.Zero);
            else
            {
                Span<uint> offsets = stackalloc uint[_dynamicSizes.Length];
                for (int i = 0; i < offsets.Length; i++)
                    offsets[i] = (uint)(_dynamicSizes[i] * dataIndex);
                VK.vkCmdBindDescriptorSets(vkCommandBuffer.CommandBuffer, vkPipeline.BindPoint, vkPipeline.Layout, set, 1u, _pinnedSet.pointer, (uint)offsets.Length, ref offsets[0]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects).
                }*/

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                _pinnedSet.Dispose();
                if (_set != Vk.VkDescriptorSet.Null)
                {
                    if (_layout != null && !_layout.IsDisposed)
                    {
                        _layout.DisposePipelineResouce(this);
                        _set = Vk.VkDescriptorSet.Null;
                    }
                    else Debug.WriteLine("Warning: VulkanPipelineResource cannot be disposed properly because parent Layout is already Disposed!");
                }

                if (_updates != null && !_updates.IsDisposed)
                    _updates.Dispose();
                if (_bufferUpdates != null && !_bufferUpdates.IsDisposed)
                    _bufferUpdates.Dispose();
                if (_imageUpdates != null && !_imageUpdates.IsDisposed)
                    _imageUpdates.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void BindUniformBuffer(uint binding, IDataBuffer buffer)
        {
            IVulkanDataBuffer vkDataBuffer = Unsafe.As<IVulkanDataBuffer>(buffer);
            _bufferUpdates.Add(new Vk.VkDescriptorBufferInfo()
            {
                buffer = vkDataBuffer.Buffer,
                offset = 0u,
                range = vkDataBuffer.Size,
            });

            _updates.Add(new Vk.VkWriteDescriptorSet()
            {
                sType = Vk.VkStructureType.WriteDescriptorSet,
                dstSet = _set,
                dstBinding = binding,
                dstArrayElement = 0u,
                descriptorCount = 1u,
                descriptorType = Vk.VkDescriptorType.UniformBuffer,

                pBufferInfo = _bufferUpdates.PointerOfElement(_bufferUpdates.Count - 1u),
            });
            _isUpdateNeeded = true;
        }
        public override void BindUniformBufferDynamic(uint binding, IDataBuffer buffer, uint elementOffset)
        {
            if (_dynamicBindingIndices == null || _dynamicSizes == null)
                throw new ArgumentOutOfRangeException($"PipelineResourceLayout does not contain Dynamic Uniform Buffer bindings!");
            Debug.Assert(_dynamicBindingIndices.ContainsKey(binding), $"PipelineResourceLayout does not contain a Dynamic Uniform Buffer binding for binding {binding}!");

            _dynamicSizes[_dynamicBindingIndices[binding]] = elementOffset;

            IVulkanDataBuffer vkDataBuffer = Unsafe.As<IVulkanDataBuffer>(buffer);
            _bufferUpdates.Add(new Vk.VkDescriptorBufferInfo()
            {
                buffer = vkDataBuffer.Buffer,
                offset = 0u,
                range = elementOffset,
            });

            _updates.Add(new Vk.VkWriteDescriptorSet()
            {
                sType = Vk.VkStructureType.WriteDescriptorSet,
                dstSet = _set,
                dstBinding = binding,
                dstArrayElement = 0u,
                descriptorCount = 1u,
                descriptorType = Vk.VkDescriptorType.UniformBufferDynamic,

                pBufferInfo = _bufferUpdates.PointerOfElement(_bufferUpdates.Count - 1u),
            });
            _isUpdateNeeded = true;
        }

        public override void BindTexture(uint binding, TextureSampler sampler, ITexture texture)
        {
            VulkanTextureSampler vkTextureSampler = Unsafe.As<VulkanTextureSampler>(sampler);
            VulkanTexture vkTexture = Unsafe.As<VulkanTexture>(texture);

            _imageUpdates.Add(new Vk.VkDescriptorImageInfo()
            {
                sampler = vkTextureSampler.Sampler,
                imageView = vkTexture.VkImageView,
                imageLayout = vkTexture.VkLayout,
            });
            _updates.Add(new Vk.VkWriteDescriptorSet()
            {
                sType = Vk.VkStructureType.WriteDescriptorSet,
                dstSet = _set,
                dstBinding = binding,
                dstArrayElement = 0u,
                descriptorCount = 1u,
                descriptorType = Vk.VkDescriptorType.CombinedImageSampler,

                pImageInfo = _imageUpdates.PointerOfElement(_imageUpdates.Count - 1u),
            });
            _isUpdateNeeded = true;
        }

        public override void BindInputAttachments(uint binding, ITexture attachment)
        {
            VulkanTexture vkTexture = Unsafe.As<VulkanTexture>(attachment);

            _imageUpdates.Add(new Vk.VkDescriptorImageInfo()
            {
                sampler = Vk.VkSampler.Null,
                imageView = vkTexture.VkImageView,
                imageLayout = Vk.VkImageLayout.ShaderReadOnlyOptimal,
            });
            _updates.Add(new Vk.VkWriteDescriptorSet()
            {
                sType = Vk.VkStructureType.WriteDescriptorSet,
                dstSet = _set,
                dstBinding = binding,
                dstArrayElement = 0u,
                descriptorCount = 1u,
                descriptorType = Vk.VkDescriptorType.InputAttachment,

                pImageInfo = _imageUpdates.PointerOfElement(_imageUpdates.Count - 1u),
            });
            _isUpdateNeeded = true;
        }
        public override void BindInputAttachments(in RenderPassStep step, IFrameBuffer frameBuffer)
        {
            ReadOnlySpan<uint> inputAttachmentIndices = step.InputAttachmentIndices;
            if (inputAttachmentIndices.Length > 0)
            {
                IVulkanFrameBuffer vkFrameBuffer = Unsafe.As<IVulkanFrameBuffer>(frameBuffer);

                for (int i = 0; i < inputAttachmentIndices.Length; i++)
                {
                    _imageUpdates.Add(new Vk.VkDescriptorImageInfo()
                    {
                        sampler = Vk.VkSampler.Null,
                        imageView = vkFrameBuffer.GetAttachmentView(inputAttachmentIndices[i]),
                        imageLayout = Vk.VkImageLayout.ShaderReadOnlyOptimal,
                    });
                    _updates.Add(new Vk.VkWriteDescriptorSet()
                    {
                        sType = Vk.VkStructureType.WriteDescriptorSet,
                        dstSet = _set,
                        dstBinding = (uint)i,
                        dstArrayElement = 0u,
                        descriptorCount = 1u,
                        descriptorType = Vk.VkDescriptorType.InputAttachment,

                        pImageInfo = _imageUpdates.PointerOfElement(_imageUpdates.Count - 1u),
                    });
                    _isUpdateNeeded = true;
                }
            }
        }

        #endregion

    }
}
