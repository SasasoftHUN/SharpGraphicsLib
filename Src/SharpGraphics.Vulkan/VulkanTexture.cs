using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal abstract class VulkanTexture : Texture
    {

        #region Fields

        private bool _isDisposed;

        private readonly bool _ownImage;
        private readonly bool _ownMemory;

        private readonly Vk.VkMemoryRequirements _memoryRequirements;
        private Vk.VkDeviceMemory _memory;
        private readonly ulong _offsetInMemory;

        private Vk.VkImageLayout[,] _layouts;

        protected readonly VulkanGraphicsDevice _device;

        protected Vk.VkImage _image;
        protected readonly Vk.VkImageUsageFlags _usage;
        protected Vk.VkImageView _imageView;

        protected readonly Vk.VkPipelineStageFlags _earliestUsage; //Used for BufferData barriers

        #endregion

        #region Properties

        internal Vk.VkExtent3D VkExtent => Extent.ToVkExtent3D();
        internal Vk.VkImageView VkImageView => _imageView;
        internal Vk.VkImage VkImage => _image;
        internal Vk.VkImageUsageFlags VkUsage => _usage;
        internal Vk.VkImageLayout VkLayout => _layouts[_layerRange.start, _mipmapRange.start];

        #endregion

        #region Constructors

        //Create texture from an existing Vulkan Image (probably used from SwapChain)
        protected VulkanTexture(VulkanGraphicsDevice device, Vk.VkImage image, Vk.VkFormat format, Vk.VkImageLayout layout, TextureType type, Vk.VkPipelineStageFlags earliestUsage, in Vector3UInt resolution, uint mipLevels, uint layers)
            : base(resolution, layers, mipLevels, format.ToDataFormat(), type)
        {
            _isDisposed = false;

            _device = device;

            _ownMemory = false;
            _memoryRequirements = new Vk.VkMemoryRequirements();
            _memory = Vk.VkDeviceMemory.Null;
            _offsetInMemory = 0ul;
            _layouts = CreateLayouts(layout, (int)layers, (int)mipLevels);

            _ownImage = false;
            _image = image;
            _usage = type.ToVkImageUsageFlags(true);

            _earliestUsage = earliestUsage;

            //ImageView
            Vk.VkImageViewCreateInfo imageViewInfo = GetBaseImageViewCreateInfo();
            Vk.VkResult result = VK.vkCreateImageView(device.Device, ref imageViewInfo, IntPtr.Zero, out _imageView);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImageView creation error: {result}!");
        }
        //Create texture with memory allocation
        protected VulkanTexture(VulkanGraphicsDevice device, Vk.VkImageCreateInfo imageCreateInfo, TextureType type, Vk.VkMemoryPropertyFlags memoryProperty, Vk.VkPipelineStageFlags earliestUsage)
            : base(imageCreateInfo.extent.ToVector3UInt(), imageCreateInfo.arrayLayers, imageCreateInfo.mipLevels, imageCreateInfo.format.ToDataFormat(), type)
        {
            _isDisposed = false;

            _device = device;

            _layouts = CreateLayouts(imageCreateInfo.initialLayout, (int)imageCreateInfo.arrayLayers, (int)imageCreateInfo.mipLevels);
            _usage = imageCreateInfo.usage;

            Vk.VkResult result = VK.vkCreateImage(_device.Device, ref imageCreateInfo, IntPtr.Zero, out _image);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImage creation error: {result}!");
            _ownImage = true;

            //Memory Allocation and Bind
            //TODO: Use Vulkan GPU Memory Allocator
            VK.vkGetImageMemoryRequirements(_device.Device, _image, out _memoryRequirements);
            _offsetInMemory = 0ul;
            result = _device.AllocateMemory(_memoryRequirements, memoryProperty, _memoryRequirements.size, out _memory);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImage Memory Allocation failed: {result}!");

            result = VK.vkBindImageMemory(_device.Device, _image, _memory, _offsetInMemory);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImage Memory Bind failed: {result}!");
            _ownMemory = true;

            _earliestUsage = earliestUsage;

            //ImageView
            Vk.VkImageViewCreateInfo imageViewInfo = GetBaseImageViewCreateInfo();
            result = VK.vkCreateImageView(device.Device, ref imageViewInfo, IntPtr.Zero, out _imageView);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImageView creation error: {result}!");
        }
        //Create just View
        protected VulkanTexture(VulkanGraphicsDevice device, Vk.VkImageViewCreateInfo viewCreateInfo, VulkanTexture referenceTexture)
            : base(referenceTexture,
                  viewCreateInfo.components.ToTextureSwizzle(),
                  new TextureRange(viewCreateInfo.subresourceRange.baseMipLevel, viewCreateInfo.subresourceRange.levelCount),
                  new TextureRange(viewCreateInfo.subresourceRange.baseArrayLayer, viewCreateInfo.subresourceRange.layerCount),
                  viewCreateInfo.format.ToDataFormat())
        {
            _isDisposed = false;

            _device = device;

            _layouts = referenceTexture._layouts;
            _usage = referenceTexture._usage;

            _image = viewCreateInfo.image;
            _ownImage = false;

            _memoryRequirements = new Vk.VkMemoryRequirements();
            _memory = Vk.VkDeviceMemory.Null;
            _offsetInMemory = 0ul;
            _ownMemory = false;

            _earliestUsage = referenceTexture._earliestUsage;

            //ImageView
            Vk.VkResult result = VK.vkCreateImageView(device.Device, ref viewCreateInfo, IntPtr.Zero, out _imageView);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkImageView creation error: {result}!");
        }

        ~VulkanTexture() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private static Vk.VkImageLayout[,] CreateLayouts(Vk.VkImageLayout layout, int layers, int levels)
        {
            Vk.VkImageLayout[,] layouts = new Vk.VkImageLayout[layers, levels];
            for (int i = 0; i < layers; i++)
                for (int j = 0; j < levels; j++)
                    layouts[i, j] = layout;
            return layouts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareStoreData<T>(VulkanCommandBuffer vkCommandBuffer, in TextureRange mipLevels, in TextureRange layers,
            out IVulkanStagingDataBuffer<T> stagingBuffer, out uint stagingBufferElementIndexOffset, out TextureRange actualMipLevels, out TextureRange actualLayers, out Vk.VkImageSubresourceRange imageSubresourceRange) where T : unmanaged
        {
            //Get or Create Staging Buffer for Layer-Mip
            if (OwnStagingBuffers) //Manage Staging Buffers internally
            {
                uint totalPixelCount = GraphicsUtils.CalculateMipLevelsTotalPixelCount(mipLevels, Extent, Layers);
                if (!TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? textureStagingBuffer, out stagingBufferElementIndexOffset) || textureStagingBuffer.Capacity != totalPixelCount)
                {
                    RemoveStagingBuffer(layers, mipLevels);
                    stagingBuffer = new VulkanStagingDataBuffer<T>(_device, DataBufferType.CopySource, totalPixelCount);
                    AddStagingBuffer(new TextureStagingBuffer(stagingBuffer, layers, mipLevels));
                }
                else stagingBuffer = Unsafe.As<IVulkanStagingDataBuffer<T>>(textureStagingBuffer);
            }
            else if (TryGetStagingBuffer(layers, mipLevels, out IStagingDataBuffer<T>? textureStagingBuffer, out stagingBufferElementIndexOffset)) //Custom Staging Buffers
                stagingBuffer = Unsafe.As<IVulkanStagingDataBuffer<T>>(textureStagingBuffer);
            else throw new Exception("Suitable staging buffer not found for VulkanTexture StoreData");

            //Consider if this is just a TextureView operating on some other texture's memory
            actualMipLevels = _mipmapRange.Combine(mipLevels); //TODO: Check max level count!
            actualLayers = _layerRange.Combine(layers); //TODO: Check max layer count!

            //State to TransferDST
            imageSubresourceRange = new Vk.VkImageSubresourceRange()
            {
                aspectMask = _usage.ToVkImageAspectFlags(),
                baseMipLevel = actualMipLevels.start,
                levelCount = actualMipLevels.count,
                baseArrayLayer = actualLayers.start,
                layerCount = actualLayers.count,
            };
            Vk.VkImageMemoryBarrier imageMemoryBarrier_FromLayout_ToTransferDST = new Vk.VkImageMemoryBarrier()
            {
                sType = Vk.VkStructureType.ImageMemoryBarrier,
                srcAccessMask = 0u,
                dstAccessMask = Vk.VkAccessFlags.TransferWrite,

                oldLayout = _layouts[actualLayers.start, actualMipLevels.start], //All layers and mip levels must have the same layout
                newLayout = Vk.VkImageLayout.TransferDstOptimal,

                srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                image = _image,
                subresourceRange = imageSubresourceRange,
            };
            vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.TopOfPipe, Vk.VkPipelineStageFlags.Transfer, ref imageMemoryBarrier_FromLayout_ToTransferDST);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeStoreData(VulkanCommandBuffer vkCommandBuffer, TextureLayout layout, in TextureRange actualMipLevels, in TextureRange actualLayers, in Vk.VkImageSubresourceRange imageSubresourceRange)
        {
            Vk.VkImageLayout vkLayout = layout.ToVkImageLayout();
            for (int i = 0; i < actualLayers.count; i++)
                for (int j = 0; j < actualMipLevels.count; j++)
                    _layouts[actualLayers.start + i, actualMipLevels.start + j] = vkLayout;
            Vk.VkImageMemoryBarrier imageMemoryBarrier_FromTransferDST_ToLayout = new Vk.VkImageMemoryBarrier()
            {
                sType = Vk.VkStructureType.ImageMemoryBarrier,
                srcAccessMask = Vk.VkAccessFlags.TransferWrite,
                dstAccessMask = _usage.ToVkAccessFlags(_earliestUsage),

                oldLayout = Vk.VkImageLayout.TransferDstOptimal,
                newLayout = _layouts[actualLayers.start, actualMipLevels.start],

                srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                image = _image,
                subresourceRange = imageSubresourceRange,
            };
            vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.Transfer, _earliestUsage, ref imageMemoryBarrier_FromTransferDST_ToLayout);
        }

        #endregion

        #region Protected Methods

        protected abstract Vk.VkImageViewCreateInfo GetBaseImageViewCreateInfo();

        protected abstract Vk.VkOffset3D GetOffsetForMipMapBlit(int level);

        protected void StoreDataInternal<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels, in TextureRange layers) where T : unmanaged
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            PrepareStoreData(vkCommandBuffer, mipLevels, layers,
                out IVulkanStagingDataBuffer<T> stagingBuffer, out uint stagingBufferElementIndexOffset, out TextureRange actualMipLevels, out TextureRange actualLayers, out Vk.VkImageSubresourceRange imageSubresourceRange);

            stagingBuffer.BufferData(data, stagingBufferElementIndexOffset);
            stagingBuffer.CopyTo(vkCommandBuffer, this, actualMipLevels, actualLayers, stagingBufferElementIndexOffset * (ulong)DataBuffer<T>.DataTypeSize);

            FinalizeStoreData(vkCommandBuffer, layout, actualMipLevels, actualLayers, imageSubresourceRange);
        }
        protected void StoreDataInternal<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels, in TextureRange layers) where T : unmanaged
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            PrepareStoreData(vkCommandBuffer, mipLevels, layers,
                out IVulkanStagingDataBuffer<T> stagingBuffer, out uint stagingBufferElementIndexOffset, out TextureRange actualMipLevels, out TextureRange actualLayers, out Vk.VkImageSubresourceRange imageSubresourceRange);

            stagingBuffer.BufferData(data, stagingBufferElementIndexOffset);
            stagingBuffer.CopyTo(vkCommandBuffer, this, actualMipLevels, actualLayers, stagingBufferElementIndexOffset * (ulong)DataBuffer<T>.DataTypeSize);

            FinalizeStoreData(vkCommandBuffer, layout, actualMipLevels, actualLayers, imageSubresourceRange);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects)
                }*/

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (!disposing)
                    Debug.WriteLine($"Disposing VulkanTexture from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_imageView != Vk.VkImageView.Null ||
                    (_ownImage && (_image != Vk.VkImage.Null || (_ownMemory && _memory != Vk.VkDeviceMemory.Null))))
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        if (_imageView != Vk.VkImageView.Null)
                        {
                            VK.vkDestroyImageView(_device.Device, _imageView, IntPtr.Zero);
                            _imageView = Vk.VkImageView.Null;
                        }

                        if (_ownImage)
                        {
                            if (_image != Vk.VkImage.Null)
                            {
                                VK.vkDestroyImage(_device.Device, _image, IntPtr.Zero);
                                _image = Vk.VkImage.Null;
                            }

                            //TODO: Use Vulkan GPU Memory Allocator
                            if (_ownMemory && _memory != Vk.VkDeviceMemory.Null)
                            {
                                VK.vkFreeMemory(_device.Device, _memory, IntPtr.Zero);
                                _memory = Vk.VkDeviceMemory.Null;
                            }
                        }
                    }
                    else Debug.WriteLine("Warning: VulkanTexture cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                // Set large fields to null
                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void GenerateMipmaps(GraphicsCommandBuffer commandBuffer)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
#if DEBUG
            if (!_usage.HasFlag(Vk.VkImageUsageFlags.TransferDst))
                throw new Exception("CopyDestination usage is needed for MipMap Generation!");
            if (!_usage.HasFlag(Vk.VkImageUsageFlags.TransferSrc))
                throw new Exception("CopySource usage is needed for MipMap Generation!");
#endif


            // Transition first mip level into a TRANSFER_SRC_OPTIMAL layout.
            // We need to wait for first CopyBuffer to complete before we can transition away from TRANSFER_DST_OPTIMAL,
            // so use VK_PIPELINE_STAGE_TRANSFER_BIT as the srcStageMask.
            Vk.VkAccessFlags access = _usage.ToVkAccessFlags(_earliestUsage);
            Vk.VkImageAspectFlags aspectMask = _usage.ToVkImageAspectFlags();
            for (uint i = 0u; i < Layers; i++)
            {
                uint layer = i + _layerRange.start;
                Vk.VkImageLayout mipTargetLayout = _layouts[layer, _mipmapRange.start];
                if (mipTargetLayout == Vk.VkImageLayout.Undefined)
                    mipTargetLayout = VkLayout;

                Vk.VkImageMemoryBarrier imageMemoryBarrier_FromLayout_ToTransferDST = new Vk.VkImageMemoryBarrier()
                {
                    sType = Vk.VkStructureType.ImageMemoryBarrier,
                    srcAccessMask = access,
                    dstAccessMask = Vk.VkAccessFlags.TransferRead,

                    oldLayout = _layouts[layer, _mipmapRange.start],
                    newLayout = Vk.VkImageLayout.TransferSrcOptimal,

                    srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                    dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                    image = _image,
                    subresourceRange = new Vk.VkImageSubresourceRange()
                    {
                        aspectMask = aspectMask,
                        baseMipLevel = _mipmapRange.start,
                        levelCount = 1u,
                        baseArrayLayer = layer,
                        layerCount = 1u,
                    },
                };
                vkCommandBuffer.PipelineBarrier(_earliestUsage, Vk.VkPipelineStageFlags.Transfer, ref imageMemoryBarrier_FromLayout_ToTransferDST);

                for (uint j = 1u; j < MipLevels; j++)
                {
                    uint level = j + _mipmapRange.start;

                    // Transition the curremt mip level into transfer dst.
                    Vk.VkImageMemoryBarrier imageMemoryBarrier_CurMip_FromLayout_ToTransferDST = new Vk.VkImageMemoryBarrier()
                    {
                        sType = Vk.VkStructureType.ImageMemoryBarrier,
                        srcAccessMask = access,
                        dstAccessMask = Vk.VkAccessFlags.TransferWrite,

                        oldLayout = _layouts[layer, level],
                        newLayout = Vk.VkImageLayout.TransferDstOptimal,

                        srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                        dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                        image = _image,
                        subresourceRange = new Vk.VkImageSubresourceRange()
                        {
                            aspectMask = aspectMask,
                            baseMipLevel = level,
                            levelCount = 1u,
                            baseArrayLayer = layer,
                            layerCount = 1u,
                        },
                    };
                    vkCommandBuffer.PipelineBarrier(_earliestUsage, Vk.VkPipelineStageFlags.Transfer, ref imageMemoryBarrier_CurMip_FromLayout_ToTransferDST);

                    uint previousLevel = level - 1u;
                    Vk.VkImageBlit blit = new Vk.VkImageBlit()
                    {
                        srcSubresource = new Vk.VkImageSubresourceLayers()
                        {
                            aspectMask = aspectMask,
                            baseArrayLayer = layer,
                            layerCount = 1u,
                            mipLevel = previousLevel,
                        },
                        srcOffsets_1 = GetOffsetForMipMapBlit((int)previousLevel),
                        dstSubresource = new Vk.VkImageSubresourceLayers()
                        {
                            aspectMask = aspectMask,
                            baseArrayLayer = layer,
                            layerCount = 1u,
                            mipLevel = level,
                        },
                        dstOffsets_1 = GetOffsetForMipMapBlit((int)level),
                    };
                    // Generate a mip level by copying and scaling the previous one.
                    vkCommandBuffer.BlitImage(_image, ref blit, Vk.VkFilter.Linear);

                    // Transition the previous mip level into it's own layout.
                    Vk.VkImageMemoryBarrier imageMemoryBarrier_PrevMip_FromTransferSRC_ToLayout = new Vk.VkImageMemoryBarrier()
                    {
                        sType = Vk.VkStructureType.ImageMemoryBarrier,
                        srcAccessMask = Vk.VkAccessFlags.TransferRead,
                        dstAccessMask = access,

                        oldLayout = Vk.VkImageLayout.TransferSrcOptimal,
                        newLayout = _layouts[layer, previousLevel],

                        srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                        dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                        image = _image,
                        subresourceRange = new Vk.VkImageSubresourceRange()
                        {
                            aspectMask = aspectMask,
                            baseMipLevel = previousLevel,
                            levelCount = 1u,
                            baseArrayLayer = layer,
                            layerCount = 1u,
                        },
                    };
                    vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.Transfer, _earliestUsage, ref imageMemoryBarrier_PrevMip_FromTransferSRC_ToLayout);

                    // Transition the current mip level
                    _layouts[layer, level] = mipTargetLayout;
                    if (j + 1u < MipLevels)
                    {
                        // Transition the current mip level into a TRANSFER_SRC_OPTIMAL layout, to be used as the source for the next one.
                        Vk.VkImageMemoryBarrier imageMemoryBarrier_CurMip_FromTransferSRC_ToTransferDST = new Vk.VkImageMemoryBarrier()
                        {
                            sType = Vk.VkStructureType.ImageMemoryBarrier,
                            srcAccessMask = Vk.VkAccessFlags.TransferWrite,
                            dstAccessMask = Vk.VkAccessFlags.TransferRead,

                            oldLayout = Vk.VkImageLayout.TransferDstOptimal,
                            newLayout = Vk.VkImageLayout.TransferSrcOptimal,

                            srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                            dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                            image = _image,
                            subresourceRange = new Vk.VkImageSubresourceRange()
                            {
                                aspectMask = aspectMask,
                                baseMipLevel = level,
                                levelCount = 1u,
                                baseArrayLayer = layer,
                                layerCount = 1u,
                            },
                        };
                        vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.Transfer, Vk.VkPipelineStageFlags.Transfer, ref imageMemoryBarrier_CurMip_FromTransferSRC_ToTransferDST);
                    }
                    else
                    {
                        // If this is the last iteration of the loop, transition the mip level directly to used layout.
                        Vk.VkImageMemoryBarrier imageMemoryBarrier_CurMip_FromTransferSRC_ToLayout = new Vk.VkImageMemoryBarrier()
                        {
                            sType = Vk.VkStructureType.ImageMemoryBarrier,
                            srcAccessMask = Vk.VkAccessFlags.TransferWrite,
                            dstAccessMask = access,

                            oldLayout = Vk.VkImageLayout.TransferDstOptimal,
                            newLayout = _layouts[layer, level],

                            srcQueueFamilyIndex = VK.QueueFamilyIgnored,
                            dstQueueFamilyIndex = VK.QueueFamilyIgnored,

                            image = _image,
                            subresourceRange = new Vk.VkImageSubresourceRange()
                            {
                                aspectMask = aspectMask,
                                baseMipLevel = level,
                                levelCount = 1u,
                                baseArrayLayer = layer,
                                layerCount = 1u,
                            },
                        };
                        vkCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.Transfer, _earliestUsage, ref imageMemoryBarrier_CurMip_FromTransferSRC_ToLayout);
                    }
                }
            }
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in CopyBufferTextureRange range)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            IVulkanDataBuffer vkDestination = Unsafe.As<IVulkanDataBuffer>(destination);

            Vk.VkBufferImageCopy bufferImageCopy = this.ToVkBufferImageCopy(range);
            vkCommandBuffer.CopyImageToBuffer(_image, Vk.VkImageLayout.TransferSrcOptimal, vkDestination.Buffer, ref bufferImageCopy);
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As<VulkanCommandBuffer>(commandBuffer);
            IVulkanDataBuffer vkDestination = Unsafe.As<IVulkanDataBuffer>(destination);

            Span<Vk.VkBufferImageCopy> bufferImageCopies = stackalloc Vk.VkBufferImageCopy[ranges.Length];

            for (int i = 0; i < bufferImageCopies.Length; i++)
                bufferImageCopies[i] = this.ToVkBufferImageCopy(ranges[i]);

            vkCommandBuffer.CopyImageToBuffer(_image, Vk.VkImageLayout.TransferSrcOptimal, vkDestination.Buffer, bufferImageCopies);
        }

        #endregion

    }
}
