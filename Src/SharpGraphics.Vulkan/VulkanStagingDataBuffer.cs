using System;
using System.Collections.Generic;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{

    internal interface IVulkanStagingDataBuffer : IVulkanMappableDataBuffer, IStagingDataBuffer { }
    internal interface IVulkanStagingDataBuffer<T> : IVulkanMappableDataBuffer<T>, IStagingDataBuffer<T>, IVulkanStagingDataBuffer where T : unmanaged { }

    internal sealed class VulkanStagingDataBuffer<T> : VulkanMappableDataBuffer<T>, IVulkanStagingDataBuffer<T> where T : unmanaged
    {

        #region Constructors

        internal VulkanStagingDataBuffer(VulkanGraphicsDevice device, DataBufferType alignmentType, uint dataCapacity) :
            base(device, dataCapacity, DataBufferType.CopySource | DataBufferType.CopyDestination | DataBufferType.Store, alignmentType, Vk.VkMemoryPropertyFlags.HostVisible)
        {
        }

        #endregion

    }
}
