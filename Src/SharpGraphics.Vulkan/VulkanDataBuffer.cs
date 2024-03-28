using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{

    internal interface IVulkanDataBuffer : IDataBuffer
    {
        internal ulong Offset { get; }

        internal Vk.VkBuffer Buffer { get; }
        internal Vk.VkBufferUsageFlags Usage { get; }
    }
    internal interface IVulkanDataBuffer<T> : IVulkanDataBuffer, IDataBuffer<T> where T : unmanaged
    {
    }

    internal abstract class VulkanDataBuffer<T> : DataBuffer<T>, IVulkanDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private readonly Vk.VkAccessFlags _accessFlags;
        private readonly Vk.VkAccessFlags _earliestUsageAccessFlag;
        private readonly Vk.VkBufferUsageFlags _usageFlags;
        private readonly Vk.VkPipelineStageFlags _earliestUsage;
        private readonly Vk.VkMemoryPropertyFlags _memoryProperty;

        private bool _isDisposed;


        protected readonly VulkanGraphicsDevice _device;

        protected ulong _offset;

        protected Vk.VkBuffer _buffer;
        protected Vk.VkMemoryRequirements _memoryRequirements;
        protected Vk.VkDeviceMemory _memory;

        #endregion

        #region Properties

        ulong IVulkanDataBuffer.Offset => _offset;

        Vk.VkBuffer IVulkanDataBuffer.Buffer => _buffer;
        Vk.VkBufferUsageFlags IVulkanDataBuffer.Usage => _usageFlags;

        #endregion

        #region Constructors

        protected VulkanDataBuffer(VulkanGraphicsDevice device, uint dataCount, DataBufferType type, DataBufferType alignmentType, Vk.VkBufferUsageFlags usageFlags, Vk.VkMemoryPropertyFlags memoryProperty) :
            base(device, dataCount, type, alignmentType)
        {
            _isDisposed = false;
            _device = device;

            _usageFlags = usageFlags;
            _earliestUsage = type.ToPipelineStageFlags();
            _earliestUsageAccessFlag = _earliestUsage.GetEarliestUsageAccessFlags();
            _memoryProperty = memoryProperty;

            _accessFlags = 0u;
            if (_usageFlags.HasFlag(Vk.VkBufferUsageFlags.IndirectBuffer)) _accessFlags |= Vk.VkAccessFlags.IndirectCommandRead;
            if (_usageFlags.HasFlag(Vk.VkBufferUsageFlags.IndexBuffer)) _accessFlags |= Vk.VkAccessFlags.IndexRead;
            if (_usageFlags.HasFlag(Vk.VkBufferUsageFlags.VertexBuffer)) _accessFlags |= Vk.VkAccessFlags.VertexAttributeRead;
            if (_usageFlags.HasFlag(Vk.VkBufferUsageFlags.UniformBuffer) || _usageFlags.HasFlag(Vk.VkBufferUsageFlags.UniformTexelBuffer)) _accessFlags |= Vk.VkAccessFlags.UniformRead;
            if (_usageFlags.HasFlag(Vk.VkBufferUsageFlags.StorageBuffer) || _usageFlags.HasFlag(Vk.VkBufferUsageFlags.StorageTexelBuffer)) _accessFlags |= _earliestUsageAccessFlag;

            if (_accessFlags == 0u)
                _accessFlags |= _earliestUsageAccessFlag;

            if (dataCount > 0u)
                Allocate();
        }

        ~VulkanDataBuffer() => Dispose(false);

        #endregion

        #region Protected Methods

        protected override void AssertCopyTo(IDataBuffer destination)
        {
            IVulkanDataBuffer vkDestination = Unsafe.As<IVulkanDataBuffer>(destination);

            Debug.Assert(_usageFlags.HasFlag(Vk.VkBufferUsageFlags.TransferSrc), $"DataBuffer<{typeof(T).FullName}> has no CopySource usage for CopyTo!");
            Debug.Assert(vkDestination.Usage.HasFlag(Vk.VkBufferUsageFlags.TransferDst), $"DataBuffer<{typeof(T).FullName}>'s destination Buffer has no CopyDestination usage for CopyTo!");
        }


        protected void Allocate()
        {
            //Buffer Creation
            Vk.VkBufferCreateInfo bufferCreateInfo = new Vk.VkBufferCreateInfo()
            {
                sType = Vk.VkStructureType.BufferCreateInfo,
                
                size = _size,
                usage = _usageFlags,

                //TODO: Implement support for using same Buffer from multiple Queues
                sharingMode = Vk.VkSharingMode.Exclusive,
            };

            Vk.VkResult result = VK.vkCreateBuffer(_device.Device, ref bufferCreateInfo, IntPtr.Zero, out _buffer);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkBuffer creation error: {result}!");

            //Memory Allocation and Bind
            //TODO: Use Vulkan GPU Memory Allocator
            VK.vkGetBufferMemoryRequirements(_device.Device, _buffer, out _memoryRequirements);
            _offset = 0ul;
            result = _device.AllocateMemory(_memoryRequirements, _memoryProperty, _memoryRequirements.size, out _memory);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkBuffer Memory Allocation failed: {result}!");

            result = VK.vkBindBufferMemory(_device.Device, _buffer, _memory, _offset);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkBuffer Memory Bind failed: {result}!");
        }
        protected void Free()
        {
            if (_buffer != Vk.VkBuffer.Null || _memory != Vk.VkDeviceMemory.Null)
            {
                if (_device != null && !_device.IsDisposed)
                {
                    if (_buffer != Vk.VkBuffer.Null)
                    {
                        VK.vkDestroyBuffer(_device.Device, _buffer, IntPtr.Zero);
                        _buffer = Vk.VkBuffer.Null;
                    }

                    //TODO: Use Vulkan GPU Memory Allocator
                    if (_memory != Vk.VkDeviceMemory.Null)
                    {
                        VK.vkFreeMemory(_device.Device, _memory, IntPtr.Zero);
                        _memory = Vk.VkDeviceMemory.Null;
                    }
                }
                else Debug.WriteLine($"Warning: VulkanDataBuffer<{typeof(T).FullName}> cannot be Freed properly because parent GraphicsDevice is already Disposed!");
            }
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing VulkanDataBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                Free();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void Resize(ulong size)
        {
            Free();
            base.Resize(size);
            Allocate();
        }
        public override void ResizeCapacity(uint dataCapacity)
        {
            Free();
            base.ResizeCapacity(dataCapacity);
            Allocate();
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, ulong size, ulong sourceOffset = 0ul, ulong destinationOffset = 0ul)
        {
            AssertCopyTo(destination, size + sourceOffset, size + destinationOffset);

            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            IVulkanDataBuffer vkDestination = Unsafe.As<IVulkanDataBuffer>(destination);

            ulong dstOffset = vkDestination.Offset + destinationOffset;
            Vk.VkBufferMemoryBarrier bufferMemoryBarrierBefore = new Vk.VkBufferMemoryBarrier()
            {
                sType = Vk.VkStructureType.BufferMemoryBarrier,
                srcAccessMask = _accessFlags,
                dstAccessMask = Vk.VkAccessFlags.MemoryWrite,

                srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                buffer = vkDestination.Buffer,
                offset = dstOffset,
                size = size,
            };
            vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.BottomOfPipe, Vk.VkPipelineStageFlags.Transfer, ref bufferMemoryBarrierBefore);

            Vk.VkBufferCopy bufferCopy = new Vk.VkBufferCopy()
            {
                srcOffset = _offset + sourceOffset,
                dstOffset = dstOffset,
                size = size
            };

            vkCommandBuffer.CopyBuffer(_buffer, vkDestination.Buffer, ref bufferCopy);

            Vk.VkBufferMemoryBarrier bufferMemoryBarrierAfter = new Vk.VkBufferMemoryBarrier()
            {
                sType = Vk.VkStructureType.BufferMemoryBarrier,
                srcAccessMask = Vk.VkAccessFlags.MemoryWrite,
                dstAccessMask = _accessFlags,

                srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                buffer = vkDestination.Buffer,
                offset = dstOffset,
                size = size,
            };
            vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.Transfer, _earliestUsage, ref bufferMemoryBarrierAfter);
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in CopyBufferTextureRange range)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            VulkanTexture vkDestination = Unsafe.As<VulkanTexture>(destination);

            Vk.VkBufferImageCopy bufferImageCopy = vkDestination.ToVkBufferImageCopy(range);
            vkCommandBuffer.CopyBufferToImage(_buffer, vkDestination.VkImage, Vk.VkImageLayout.TransferDstOptimal, ref bufferImageCopy);
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            VulkanTexture vkDestination = Unsafe.As<VulkanTexture>(destination);

            Span<Vk.VkBufferImageCopy> bufferImageCopies = stackalloc Vk.VkBufferImageCopy[ranges.Length];

            for (int i = 0; i < bufferImageCopies.Length; i++)
                bufferImageCopies[i] = vkDestination.ToVkBufferImageCopy(ranges[i]);
                
            vkCommandBuffer.CopyBufferToImage(_buffer, vkDestination.VkImage, Vk.VkImageLayout.TransferDstOptimal, bufferImageCopies);
        }

        #endregion

    }
}
