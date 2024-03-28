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
    internal sealed class VulkanCommandProcessor : GraphicsCommandProcessor, IEquatable<VulkanCommandProcessor>, IDisposable
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;

        private readonly uint _familyIndex;
        private readonly uint _queueIndex;
        private readonly Vk.VkQueue _queue;

        private readonly CommandBufferFactory _commandBufferFactory;

        #endregion

        #region Properties

        internal VulkanGraphicsDevice Device => _device;

        internal uint FamilyIndex => _familyIndex;
        internal Vk.VkQueue Queue => _queue;

        public override CommandBufferFactory CommandBufferFactory => _commandBufferFactory;

        #endregion

        #region Constructors

        internal VulkanCommandProcessor(VulkanGraphicsDevice device, uint familyIndex, uint queueIndex, float priority): base(device.DeviceInfo.CommandProcessorGroups[(int)familyIndex].Type, priority)
        {
            _isDisposed = false;
            _device = device;

            _familyIndex = familyIndex;
            _queueIndex = queueIndex;
            VK.vkGetDeviceQueue(device.Device, familyIndex, queueIndex, out _queue);

            _commandBufferFactory = CreateCommandBufferFactory();
        }

        ~VulkanCommandProcessor() => Dispose(false);

        #endregion

        #region Private Methods



        #endregion

        #region Protected Methods

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
                    Debug.WriteLine($"Disposing Vulkan Command Processor ({_familyIndex}-{_queueIndex}) from {(disposing ? "Dispose()" : "Finalizer")}...");

                _commandBufferFactory.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        internal void Submit(Vk.VkCommandBuffer commandBuffer, Vk.VkFence fence)
        {
            VK.vkResetFences(_device.Device, 1u, ref fence);
            
            Vk.VkSubmitInfo submitInfo = new Vk.VkSubmitInfo()
            {
                sType = Vk.VkStructureType.SubmitInfo,
                commandBufferCount = 1u,
                pCommandBuffers = UnsafeExtension.AsIntPtr(ref commandBuffer),
            };

            if (VK.vkQueueSubmit(_queue, 1u, ref submitInfo, fence) != Vk.VkResult.Success)
                return;
        }
        internal void Submit(Vk.VkCommandBuffer commandBuffer, Vk.VkSemaphore waitSemaphore, Vk.VkPipelineStageFlags waitStage, Vk.VkSemaphore signalSemaphore, Vk.VkFence fence)
        {
            VK.vkResetFences(_device.Device, 1u, ref fence);

            Vk.VkSubmitInfo submitInfo = new Vk.VkSubmitInfo()
            {
                sType = Vk.VkStructureType.SubmitInfo,

                waitSemaphoreCount = 1u,
                pWaitSemaphores = UnsafeExtension.AsIntPtr(ref waitSemaphore),
                pWaitDstStageMask = UnsafeExtension.AsIntPtr(ref waitStage),

                commandBufferCount = 1u,
                pCommandBuffers = UnsafeExtension.AsIntPtr(ref commandBuffer),

                signalSemaphoreCount = 1u,
                pSignalSemaphores = UnsafeExtension.AsIntPtr(ref signalSemaphore),
            };

            if (VK.vkQueueSubmit(_queue, 1u, ref submitInfo, fence) != Vk.VkResult.Success)
                return;
        }

        #endregion

        #region Public Methods

        public override CommandBufferFactory CreateCommandBufferFactory(CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default)
            => new VulkanCommandBufferFactory(this, properties.ToVkCommandPoolCreateFlags());
        public override CommandBufferFactory[] CreateCommandBufferFactories(uint count, CommandBufferFactoryProperties properties = CommandBufferFactoryProperties.Default)
        {
            Vk.VkCommandPoolCreateFlags poolCreateFlags = properties.ToVkCommandPoolCreateFlags();

            CommandBufferFactory[] factories = new CommandBufferFactory[count];
            for (int i = 0; i < factories.Length; i++)
                factories[i] = new VulkanCommandBufferFactory(this, poolCreateFlags);

            return factories;
        }

        public override void Submit(GraphicsCommandBuffer commandBuffer)
        {
            VulkanCommandBuffer vkCommandBuffer = Unsafe.As< VulkanCommandBuffer>(commandBuffer);
            Submit(vkCommandBuffer.CommandBuffer, vkCommandBuffer.Fence);
        }

        public override void WaitForIdle() => VK.vkQueueWaitIdle(_queue);

        public override bool Equals(object? obj) => obj is VulkanCommandProcessor vulkanCommandProcessor && Equals(vulkanCommandProcessor);
        public bool Equals(VulkanCommandProcessor? other) => other is not null && _familyIndex == other._familyIndex && _queueIndex == other._queueIndex;
        public override int GetHashCode()
        {
            var hashCode = 910069165;
            hashCode = hashCode * -1521134295 + _familyIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + _queueIndex.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(VulkanCommandProcessor? left, VulkanCommandProcessor? right)
            => left is not null && right is not null && left.Equals(right);
        public static bool operator !=(VulkanCommandProcessor? left, VulkanCommandProcessor? right)
            => !(left == right);

        #endregion

    }
}
