using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanPipelineResourceLayout : PipelineResourceLayout
    {

        private const uint POOLS_START_COUNT = 4u;
        private const uint POOL_START_SIZE = 128u;
        private const uint POOL_START_SIZE_MULTIPLIER = 8u;
        private const uint POOL_START_SIZE_MULTIPLICATION_LIMIT = POOL_START_SIZE * POOL_START_SIZE_MULTIPLIER;

        private struct DescriptorPool
        {
            public readonly Vk.VkDescriptorPool pool;
            public uint allocationLeft;

            public DescriptorPool(in Vk.VkDescriptorPool pool, uint capacity)
            {
                this.pool = pool;
                this.allocationLeft = capacity;
            }
        }

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;

        private Vk.VkDescriptorSetLayout _descriptorSetLayout;
        private readonly Vk.VkDescriptorType[] _descriptorPoolTypes;
        private readonly SortedSet<uint> _dynamicBindings;

        private DescriptorPool[] _descriptorPools = new DescriptorPool[POOLS_START_COUNT];

        #endregion

        #region Properties

        internal Vk.VkDescriptorSetLayout Layout => _descriptorSetLayout;
        internal IEnumerable<uint> DynamicBindings => _dynamicBindings;
        internal bool HasDynamicBindings { get; }

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        internal VulkanPipelineResourceLayout(VulkanGraphicsDevice device, in PipelineResourceProperties resourceProperties)
        {
            AssertLayout(resourceProperties);

            _device = device;

            //Prepare Descriptor Properties
            _dynamicBindings = new SortedSet<uint>();
            ReadOnlySpan<PipelineResourceProperty> properties = resourceProperties.Properties;
            Span<Vk.VkDescriptorSetLayoutBinding> descriptorSetLayoutBindings = stackalloc Vk.VkDescriptorSetLayoutBinding[properties.Length];
            _descriptorPoolTypes = new Vk.VkDescriptorType[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                switch (properties[i].type)
                {
                    case PipelineResourceType.UniformBufferDynamic: _dynamicBindings.Add((uint)i); break;
                }

                descriptorSetLayoutBindings[i] = new Vk.VkDescriptorSetLayoutBinding()
                {
                    binding = properties[i].binding,
                    descriptorType = properties[i].type.ToVkDescriptorType(),
                    descriptorCount = 1u, //Array of buffers: https://stackoverflow.com/questions/65772848/what-does-vkdescriptorsetlayoutbindingdescriptorcount-specify
                    stageFlags = properties[i].stage.ToVkShaderStageFlags(),
                };

                _descriptorPoolTypes[i] = descriptorSetLayoutBindings[i].descriptorType;
            }
            HasDynamicBindings = _dynamicBindings.Count > 0;

            //Create Descriptor Set Layout
            Vk.VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new Vk.VkDescriptorSetLayoutCreateInfo()
            {
                sType = Vk.VkStructureType.DescriptorSetLayoutCreateInfo,
                flags = 0u,
                bindingCount = (uint)descriptorSetLayoutBindings.Length,
                pBindings = UnsafeExtension.AsIntPtr(descriptorSetLayoutBindings),
            };
            Vk.VkResult result = VK.vkCreateDescriptorSetLayout(_device.Device, ref descriptorSetLayoutCreateInfo, IntPtr.Zero, out _descriptorSetLayout);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Descriptor Set Layout creation failed: {result}!");

            CreatePool(1u);
        }

        ~VulkanPipelineResourceLayout() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private int GetNewPoolIndex()
        {
            for (int i = 0; i < _descriptorPools.Length; i++)
                if (_descriptorPools[i].pool == Vk.VkDescriptorPool.Null)
                    return i;

            DescriptorPool[] newPools = new DescriptorPool[_descriptorPools.Length * 2];
            for (int i = 0; i < _descriptorPools.Length; i++)
                newPools[i] = _descriptorPools[i];
            int index = _descriptorPools.Length;
            _descriptorPools = newPools;
            return index;
        }
        private int CreatePool(uint capacityRequest)
        {
            uint poolCapacity = POOL_START_SIZE;
            if (capacityRequest > poolCapacity)
                poolCapacity = capacityRequest > POOL_START_SIZE_MULTIPLICATION_LIMIT ? capacityRequest : (capacityRequest * POOL_START_SIZE_MULTIPLIER);

            Span<Vk.VkDescriptorPoolSize> poolSizes = stackalloc Vk.VkDescriptorPoolSize[_descriptorPoolTypes.Length];
            for (int i = 0; i < poolSizes.Length; i++)
                poolSizes[i] = new Vk.VkDescriptorPoolSize(_descriptorPoolTypes[i], poolCapacity);

            //Create Descriptor Pool
            Vk.VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new Vk.VkDescriptorPoolCreateInfo()
            {
                sType = Vk.VkStructureType.DescriptorPoolCreateInfo,
                flags = Vk.VkDescriptorPoolCreateFlags.FreeDescriptorSet,
                maxSets = poolCapacity,
                poolSizeCount = (uint)poolSizes.Length,
                pPoolSizes = UnsafeExtension.AsIntPtr(poolSizes),
            };
            Vk.VkResult result = VK.vkCreateDescriptorPool(_device.Device, ref descriptorPoolCreateInfo, IntPtr.Zero, out Vk.VkDescriptorPool descriptorPool);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Descriptor Pool creation failed: {result}!");

            int poolIndex = GetNewPoolIndex();
            _descriptorPools[poolIndex] = new DescriptorPool(descriptorPool, poolCapacity);
            return poolIndex;
        }
        private int GetPoolIndexForAllocation(uint allocationCount)
        {
            for (int i = 0; i < _descriptorPools.Length; i++)
                if (_descriptorPools[i].allocationLeft >= allocationCount)
                    return i;
            return CreatePool(allocationCount);
        }

        #endregion

        #region Internal Methods

        internal void DisposePipelineResouce(VulkanPipelineResource resource)
        {
            if (!_device.IsDisposed)
            {
                VK.vkFreeDescriptorSets(_device.Device, _descriptorPools[resource.PoolIndex].pool, 1u, ref resource.Set);
                ++_descriptorPools[resource.PoolIndex].allocationLeft;
            }
            else Debug.WriteLine("Warning: VulkanPipelineResource cannot be disposed properly because parent GraphicsDevice is already Disposed!");
        }

        #endregion

        #region Protected Methods

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
                if (_descriptorSetLayout != Vk.VkDescriptorSetLayout.Null)
                {
                    if (!_device.IsDisposed)
                    {
                        for (int i = 0; i < _descriptorPools.Length; i++)
                            if (_descriptorPools[i].pool != Vk.VkDescriptorPool.Null)
                                VK.vkDestroyDescriptorPool(_device.Device, _descriptorPools[i].pool, IntPtr.Zero);

                        VK.vkDestroyDescriptorSetLayout(_device.Device, _descriptorSetLayout, IntPtr.Zero);
                    }
                    else Debug.WriteLine("Warning: VulkanPipelineResourceLayout cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }
                _descriptorSetLayout = Vk.VkDescriptorSetLayout.Null;

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override PipelineResource CreateResource()
        {
            int poolIndex = GetPoolIndexForAllocation(1u);
            --_descriptorPools[poolIndex].allocationLeft;
            using PinnedObjectReference<Vk.VkDescriptorSetLayout> pinnedDescriptorSetLayout = new PinnedObjectReference<Vk.VkDescriptorSetLayout>(ref _descriptorSetLayout);

            Vk.VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new Vk.VkDescriptorSetAllocateInfo()
            {
                sType = Vk.VkStructureType.DescriptorSetAllocateInfo,
                descriptorPool = _descriptorPools[poolIndex].pool,
                descriptorSetCount = 1u,
                pSetLayouts = pinnedDescriptorSetLayout.pointer,
            };

            Vk.VkResult allocationResult = VK.vkAllocateDescriptorSets(_device.Device, ref descriptorSetAllocateInfo, out Vk.VkDescriptorSet set);
            if (allocationResult != Vk.VkResult.Success)
                throw new Exception($"Descriptor Set allocation failed: {allocationResult}!");

            return new VulkanPipelineResource(_device, this, set, poolIndex);
        }

        public override PipelineResource[] CreateResources(uint count)
        {
            int poolIndex = GetPoolIndexForAllocation(count);
            _descriptorPools[poolIndex].allocationLeft -= count;
            Span<Vk.VkDescriptorSetLayout> layouts = stackalloc Vk.VkDescriptorSetLayout[(int)count];
            for (int i = 0; i < layouts.Length; i++)
                layouts[i] = _descriptorSetLayout;
            Vk.VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new Vk.VkDescriptorSetAllocateInfo()
            {
                sType = Vk.VkStructureType.DescriptorSetAllocateInfo,
                descriptorPool = _descriptorPools[poolIndex].pool,
                descriptorSetCount = count,
                pSetLayouts = UnsafeExtension.AsIntPtr(layouts),
            };

            Span<Vk.VkDescriptorSet> sets = stackalloc Vk.VkDescriptorSet[(int)count];
            Vk.VkResult allocationResult = VK.vkAllocateDescriptorSets(_device.Device, ref descriptorSetAllocateInfo, UnsafeExtension.AsIntPtr(sets));
            if (allocationResult != Vk.VkResult.Success)
                throw new Exception($"Descriptor Set allocation failed: {allocationResult}!");

            PipelineResource[] result = new PipelineResource[(int)count];
            for (int i = 0; i < sets.Length; i++)
                result[i] = new VulkanPipelineResource(_device, this, sets[i], poolIndex);
            return result;
        }

        #endregion

    }
}
