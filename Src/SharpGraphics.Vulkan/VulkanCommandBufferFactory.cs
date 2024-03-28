using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanCommandBufferFactory : CommandBufferFactory
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanCommandProcessor _commandProcessor;

        private Vk.VkCommandPool _commandPool;

        #endregion

        #region Properties

        internal VulkanCommandProcessor CommandProcessor => _commandProcessor;
        internal Vk.VkCommandPool CommandPool => _commandPool;

        #endregion

        #region Constructors

        internal VulkanCommandBufferFactory(VulkanCommandProcessor commandProcessor, Vk.VkCommandPoolCreateFlags createFlags)
        {
            _isDisposed = false;

            _commandProcessor = commandProcessor;

            Vk.VkCommandPoolCreateInfo commandPoolCreateInfo = new Vk.VkCommandPoolCreateInfo()
            {
                sType = Vk.VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = commandProcessor.FamilyIndex,
                flags = createFlags,
            };

            Vk.VkResult result = VK.vkCreateCommandPool(_commandProcessor.Device.Device, ref commandPoolCreateInfo, IntPtr.Zero, out _commandPool);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Error creating VkCommandPool: {result}!");
        }

        ~VulkanCommandBufferFactory() => Dispose(false);

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
                    Debug.WriteLine($"Disposing Vulkan Command Buffer Factory from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_commandPool != Vk.VkCommandPool.Null)
                {
                    if (_commandProcessor != null && !_commandProcessor.IsDisposed && _commandProcessor.Device != null && !_commandProcessor.Device.IsDisposed)
                    {
                        VK.vkDestroyCommandPool(_commandProcessor.Device.Device, _commandPool, IntPtr.Zero);
                        _commandPool = Vk.VkCommandPool.Null;
                    }
                    else Debug.WriteLine("Warning: VulkanCommandBufferFactory cannot be disposed properly because parent CommandProcessor (VkQueue) or GraphicsDevice is already Disposed!");
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        internal static void DeleteCommandBuffersOnSamePool(in ReadOnlySpan<VulkanCommandBuffer> commandBuffers)
        {
            VulkanCommandBufferFactory? factory = null;
            Span<Vk.VkCommandBuffer> vkCommandBuffers = stackalloc Vk.VkCommandBuffer[commandBuffers.Length];
            Span<Vk.VkFence> vkFences = stackalloc Vk.VkFence[commandBuffers.Length];
            int bufferCount = 0;
            int fenceCount = 0;

            foreach (VulkanCommandBuffer commandBuffer in commandBuffers)
                if (commandBuffer != null && !commandBuffer.IsDisposed)
                {
                    if (commandBuffer.CommandBuffer != Vk.VkCommandBuffer.Null)
                        vkCommandBuffers[bufferCount++] = commandBuffer.CommandBuffer;
                    if (commandBuffer.Fence != Vk.VkFence.Null)
                        vkFences[fenceCount++] = commandBuffer.Fence;
                    if (factory == null && commandBuffer.CommandBufferFactory != null && !commandBuffer.CommandBufferFactory.IsDisposed)
                        factory = commandBuffer.CommandBufferFactory;
                    
                    commandBuffer.SetToDisposed();
                }

            if (factory != null)
            {
                if (bufferCount > 0)
                    factory.DeleteVKCommandBuffers(vkCommandBuffers.Slice(0, bufferCount));
                if (fenceCount > 0 && factory.CommandProcessor != null && !factory.CommandProcessor.IsDisposed && factory.CommandProcessor.Device != null && !factory.CommandProcessor.Device.IsDisposed)
                    factory.CommandProcessor.Device.DestroyFences(vkFences.Slice(0, fenceCount));
            }
        }
        internal static void DeleteCommandBuffersOnDifferentPools(IEnumerable<VulkanCommandBuffer> commandBuffers)
        {
            foreach (IGrouping<VulkanCommandBufferFactory, VulkanCommandBuffer> commandBuffersOnPool in commandBuffers.Where(c => !c.IsDisposed && c.CommandBufferFactory != null && !c.CommandBufferFactory.IsDisposed).GroupBy(c => c.CommandBufferFactory))
            {
                Span<Vk.VkCommandBuffer> vkCommandBuffers = stackalloc Vk.VkCommandBuffer[commandBuffersOnPool.Count()];
                Span<Vk.VkFence> vkFences = stackalloc Vk.VkFence[vkCommandBuffers.Length];
                int bufferCount = 0;
                int fenceCount = 0;

                foreach (VulkanCommandBuffer commandBuffer in commandBuffersOnPool)
                {
                    if (commandBuffer.CommandBuffer != Vk.VkCommandBuffer.Null)
                        vkCommandBuffers[bufferCount++] = commandBuffer.CommandBuffer;
                    if (commandBuffer.Fence != Vk.VkFence.Null)
                        vkFences[fenceCount++] = commandBuffer.Fence;

                    commandBuffer.SetToDisposed();
                }

                if (bufferCount > 0)
                    commandBuffersOnPool.Key.DeleteVKCommandBuffers(vkCommandBuffers.Slice(0, bufferCount));
                if (fenceCount > 0)
                    commandBuffersOnPool.Key.CommandProcessor.Device.DestroyFences(vkFences.Slice(0, fenceCount));
            }
        }

        internal void DeleteVKCommandBuffer(Vk.VkCommandBuffer commandBuffer)
            => VK.vkFreeCommandBuffers(_commandProcessor.Device.Device, _commandPool, 1u, ref commandBuffer);
        internal void DeleteVKCommandBuffers(in Span<Vk.VkCommandBuffer> commandBuffers)
            => VK.vkFreeCommandBuffers(_commandProcessor.Device.Device, _commandPool, (uint)commandBuffers.Length, ref commandBuffers[0]);

        #endregion

        #region Public Methods

        public override GraphicsCommandBuffer CreateCommandBuffer(CommandBufferLevel level = CommandBufferLevel.Primary)
        {
            Vk.VkCommandBufferAllocateInfo commandBufferAllocateInfo = new Vk.VkCommandBufferAllocateInfo()
            {
                sType = Vk.VkStructureType.CommandBufferAllocateInfo,
                commandPool = _commandPool,
                level = level.ToVkCommandBufferLevel(),
                commandBufferCount = 1,
            };

            Vk.VkResult result = VK.vkAllocateCommandBuffers(_commandProcessor.Device.Device, ref commandBufferAllocateInfo, out Vk.VkCommandBuffer commandBuffer);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkCommandBuffer allocation error: {result}!");

            return new VulkanCommandBuffer(this, commandBuffer);
        }

        public override GraphicsCommandBuffer[] CreateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary)
        {
            Span<Vk.VkCommandBuffer> commandBuffers = stackalloc Vk.VkCommandBuffer[(int)count];

            Vk.VkCommandBufferAllocateInfo commandBufferAllocateInfo = new Vk.VkCommandBufferAllocateInfo()
            {
                sType = Vk.VkStructureType.CommandBufferAllocateInfo,
                commandPool = _commandPool,
                level = level.ToVkCommandBufferLevel(),
                commandBufferCount = count,
            };

            Vk.VkResult result = VK.vkAllocateCommandBuffers(_commandProcessor.Device.Device, ref commandBufferAllocateInfo, out commandBuffers[0]);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkCommandBuffer allocation error: {result}!");

            VulkanCommandBuffer[] results = new VulkanCommandBuffer[count];
            for (int i = 0; i < count; i++)
                results[i] = new VulkanCommandBuffer(this, commandBuffers[i]);

            return results;
        }

        #endregion


    }
}
