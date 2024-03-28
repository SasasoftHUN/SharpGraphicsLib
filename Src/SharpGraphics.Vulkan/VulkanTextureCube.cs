using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanTextureCube : VulkanTexture, ITextureCube
    {

        #region Fields

        //private bool _isDisposed;

        #endregion

        #region Constructors

        //Create Cube texture from an existing Vulkan Image (probably used from SwapChain)
        internal VulkanTextureCube(VulkanGraphicsDevice device, Vk.VkImage image, Vk.VkFormat format, Vk.VkExtent2D size, Vk.VkImageLayout layout, TextureType type, Vk.VkPipelineStageFlags earliestUsage, uint mipLevels) :
            base(device, image, format, layout, type, earliestUsage, new Vector3UInt(size.width, size.height, 1u), mipLevels, 6u) { }
        //Create Cube texture with memory allocation
        internal VulkanTextureCube(VulkanGraphicsDevice device, Vk.VkFormat format, Vk.VkExtent2D size, Vk.VkImageLayout layout, TextureType type, Vk.VkMemoryPropertyFlags memoryProperty, Vk.VkPipelineStageFlags earliestUsage, uint mipLevels) :
            base(device,
                new Vk.VkImageCreateInfo()
                {
                    sType = Vk.VkStructureType.ImageCreateInfo,
                    flags = Vk.VkImageCreateFlags.CubeCompatible,
                    imageType = Vk.VkImageType.Image2D,
                    format = format,
                    extent = new Vk.VkExtent3D() { width = size.width, height = size.height, depth = 1u },
                    mipLevels = mipLevels,
                    arrayLayers = 6u,

                    samples = Vk.VkSampleCountFlags.SampleCount1,
                    tiling = memoryProperty.HasFlag(Vk.VkMemoryPropertyFlags.HostVisible) ? Vk.VkImageTiling.Linear : Vk.VkImageTiling.Optimal,
                    usage = type.ToVkImageUsageFlags(!memoryProperty.HasFlag(Vk.VkMemoryPropertyFlags.HostVisible)),

                    //TODO: Implement support for using same Texture from multiple Queues
                    sharingMode = Vk.VkSharingMode.Exclusive,

                    initialLayout = layout,
                },
                type, memoryProperty, earliestUsage) { }
        //Create Cube View
        internal VulkanTextureCube(VulkanGraphicsDevice device, Vk.VkImageViewCreateInfo imageViewCreateInfo, VulkanTexture referenceTexture) :
            base(device, imageViewCreateInfo, referenceTexture) { }

        //~VulkanTextureCube() => Dispose(false);

        #endregion

        #region Protected Methods

        protected override Vk.VkImageViewCreateInfo GetBaseImageViewCreateInfo()
            => new Vk.VkImageViewCreateInfo()
            {
                sType = Vk.VkStructureType.ImageViewCreateInfo,
                image = _image,
                viewType = Vk.VkImageViewType.Cube,
                format = DataFormat.ToVkFormat(),
                components = new Vk.VkComponentMapping()
                {
                    r = Vk.VkComponentSwizzle.Identity,
                    g = Vk.VkComponentSwizzle.Identity,
                    b = Vk.VkComponentSwizzle.Identity,
                    a = Vk.VkComponentSwizzle.Identity,
                },
                subresourceRange = new Vk.VkImageSubresourceRange()
                {
                    aspectMask = _usage.ToVkImageAspectFlags(),
                    baseMipLevel = 0u,
                    levelCount = MipLevels,
                    baseArrayLayer = 0u,
                    layerCount = 6u,
                }
            };

        protected override Vk.VkOffset3D GetOffsetForMipMapBlit(int level)
            => new Vk.VkOffset3D(
                    Math.Max((int)Width >> level, 1),
                    Math.Max((int)Height >> level, 1), 1);


        // Protected implementation of Dispose pattern.
        /*protected override void Dispose(bool disposing)
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
                    Debug.WriteLine($"Disposing Vulkan Texture 2D from {(disposing ? "Dispose()" : "Finalizer")}...");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }*/

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, new ReadOnlySpan<T>(data), layout, mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, data, layout, mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, data, layout, mipLevels, (uint)face);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, new ReadOnlySpan<T>(data), layout, mipLevels, new TextureRange(0u, 6u));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, data, layout, mipLevels, new TextureRange(0u, 6u));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => StoreDataInternal(commandBuffer, data, layout, mipLevels, new TextureRange(0u, 6u));


        public ITextureCube CreateView(DataFormat? dataFormat = default(DataFormat?), in TextureSwizzle? swizzle = default(TextureSwizzle?), in TextureRange? levels = default(TextureRange?))
            => new VulkanTextureCube(_device, new Vk.VkImageViewCreateInfo()
            {
                sType = Vk.VkStructureType.ImageViewCreateInfo,
                image = _image,
                viewType = Vk.VkImageViewType.Cube,
                format = (dataFormat ?? DataFormat).ToVkFormat(),
                components = _swizzle.Combine(swizzle).ToVkComponentMapping(),
                subresourceRange = new Vk.VkImageSubresourceRange()
                {
                    aspectMask = _usage.ToVkImageAspectFlags(),
                    baseMipLevel = (levels.HasValue ? levels.Value.start : 0u) + _mipmapRange.start,
                    levelCount = levels.HasValue ? levels.Value.count : _mipmapRange.count,
                    baseArrayLayer = _layerRange.start,
                    layerCount = 6u,
                }
            }, _referenceTexture != null ? Unsafe.As<VulkanTexture>(_referenceTexture) : this);
        public ITexture2D CreateView(CubeFace face, DataFormat? dataFormat = default(DataFormat?), in TextureSwizzle? swizzle = default(TextureSwizzle?), in TextureRange? levels = default(TextureRange?))
            => new VulkanTexture2D(_device, new Vk.VkImageViewCreateInfo()
            {
                sType = Vk.VkStructureType.ImageViewCreateInfo,
                image = _image,
                viewType = Vk.VkImageViewType.ImageView2D,
                format = (dataFormat ?? DataFormat).ToVkFormat(),
                components = _swizzle.Combine(swizzle).ToVkComponentMapping(),
                subresourceRange = new Vk.VkImageSubresourceRange()
                {
                    aspectMask = _usage.ToVkImageAspectFlags(),
                    baseMipLevel = (levels.HasValue ? levels.Value.start : 0u) + _mipmapRange.start,
                    levelCount = levels.HasValue ? levels.Value.count : _mipmapRange.count,
                    baseArrayLayer = _layerRange.start + (uint)face,
                    layerCount = 1u,
                }
            }, _referenceTexture != null ? Unsafe.As<VulkanTexture>(_referenceTexture) : this);

        public void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, CubeFace face, uint elementIndexOffset = 0u)
            => UseStagingBuffer(stagingBuffer, mipLevels, (uint)face, elementIndexOffset);

        #endregion

    }
}
