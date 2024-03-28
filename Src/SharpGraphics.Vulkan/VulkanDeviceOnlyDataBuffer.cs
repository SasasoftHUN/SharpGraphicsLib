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

    internal interface IVulkanDeviceOnlyDataBuffer : IVulkanDataBuffer, IDeviceOnlyDataBuffer { }
    internal interface IVulkanDeviceOnlyDataBuffer<T> : IVulkanDataBuffer<T>, IDeviceOnlyDataBuffer<T>, IVulkanDeviceOnlyDataBuffer where T : unmanaged { }

    internal sealed class VulkanDeviceOnlyDataBuffer<T> : VulkanDataBuffer<T>, IVulkanDeviceOnlyDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private IVulkanStagingDataBuffer<T>? _stagingBuffer;
        private ulong _stagingBufferOffset = 0ul;
        private uint _stagingBufferElementIndexOffset = 0u;
        private bool _ownStagingBuffer = true;
        private DataBufferType _alignmentType;

        private bool _isDisposed;

        #endregion

        #region Properties



        #endregion

        #region Constructors

        internal VulkanDeviceOnlyDataBuffer(VulkanGraphicsDevice device, uint dataCapacity, DataBufferType type, DataBufferType alignmentType, Vk.VkMemoryPropertyFlags memoryProperty) :
            base(device, dataCapacity, type, alignmentType, type.ToVkBufferUsageFlags(true), memoryProperty)
        {
            _isDisposed = false;
            _alignmentType = alignmentType;
        }

        ~VulkanDeviceOnlyDataBuffer() => Dispose(false);

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertStoreData()
        {
            Debug.Assert(_stagingBuffer != null, $"Staging Buffer is null in VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> during StoreData!");
            if (_stagingBuffer != null)
                Debug.Assert(!_stagingBuffer.IsDisposed, $"Staging Buffer is Disposed in VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> during StoreData!");
        }

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void AssertStoreData(ulong sizeRequired)
        {
            AssertStoreData();
            base.AssertStoreData(sizeRequired);
            if (_stagingBuffer != null)
                Debug.Assert(_stagingBuffer.Size >= sizeRequired + _stagingBufferOffset, $"Staging Buffer has not enough allocated memory (Size: {_stagingBuffer.Size}) for {sizeRequired + _stagingBufferOffset} bytes in VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> during StoreData!");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void AssertStoreDataElements(uint dataCapacityRequired)
        {
            AssertStoreData();
            base.AssertStoreDataElements(dataCapacityRequired);
            if (_stagingBuffer != null)
                Debug.Assert(_stagingBuffer.Capacity >= dataCapacityRequired + _stagingBufferElementIndexOffset, $"Staging Buffer has not enough allocated memory (Data Capacity: {_stagingBuffer.Capacity}) for {dataCapacityRequired + _stagingBufferElementIndexOffset} elements in VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> during StoreData!");
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
                    Debug.WriteLine($"Disposing VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_stagingBuffer != null)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        _stagingBuffer.Dispose();
                        _stagingBuffer = null;
                    }
                    else Debug.WriteLine($"Warning: VulkanDeviceOnlyDataBuffer<{typeof(T).FullName}> cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul)
        {
            if (_stagingBuffer == null)
            {
                if (_ownStagingBuffer)
                    _stagingBuffer = new VulkanStagingDataBuffer<T>(_device, _alignmentType, Capacity);
                else throw new Exception("VulkanDeviceOnlyDataBuffer has no StagingBuffer for StoreData");
            }
            AssertStoreData(size + offset);

            ulong stagingBufferOffset = _stagingBufferOffset + offset;
            _stagingBuffer.BufferData(data, size, stagingBufferOffset);

            _stagingBuffer.CopyTo(commandBuffer, this, size, stagingBufferOffset, offset);
        }

        public override void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u)
        {
            if (_stagingBuffer == null)
            {
                if (_ownStagingBuffer)
                    _stagingBuffer = new VulkanStagingDataBuffer<T>(_device, _alignmentType, Capacity);
                else throw new Exception("VulkanDeviceOnlyDataBuffer has no StagingBuffer for StoreData");
            }
            AssertStoreDataElements(1u + elementIndexOffset);

            _stagingBuffer.BufferData(ref data, _stagingBufferElementIndexOffset + elementIndexOffset);

            ulong offset = elementIndexOffset * _elementOffset;
            _stagingBuffer.CopyTo(commandBuffer, this, _dataTypeSize, _stagingBufferOffset + offset, offset);
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u)
        {
            if (_stagingBuffer == null)
            {
                if (_ownStagingBuffer)
                    _stagingBuffer = new VulkanStagingDataBuffer<T>(_device, _alignmentType, Capacity);
                else throw new Exception("VulkanDeviceOnlyDataBuffer has no StagingBuffer for StoreData");
            }
            AssertStoreDataElements((uint)data.Length + elementIndexOffset);

            uint stagingBufferElementIndexOffset = _stagingBufferElementIndexOffset + elementIndexOffset;
            _stagingBuffer.BufferData(data, stagingBufferElementIndexOffset);

            _stagingBuffer.CopyElementsTo(commandBuffer, this, (uint)data.Length, stagingBufferElementIndexOffset, elementIndexOffset);
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u)
        {
            if (_stagingBuffer == null)
            {
                if (_ownStagingBuffer)
                    _stagingBuffer = new VulkanStagingDataBuffer<T>(_device, _alignmentType, Capacity);
                else throw new Exception("VulkanDeviceOnlyDataBuffer has no StagingBuffer for StoreData");
            }
            AssertStoreDataElements((uint)data.Length + elementIndexOffset);

            uint stagingBufferElementIndexOffset = _stagingBufferElementIndexOffset + elementIndexOffset;
            _stagingBuffer.BufferData(data, stagingBufferElementIndexOffset);

            _stagingBuffer.CopyElementsTo(commandBuffer, this, (uint)data.Length, stagingBufferElementIndexOffset, elementIndexOffset);
        }

        public void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer)
        {
            if (_ownStagingBuffer && _stagingBuffer != null)
                _stagingBuffer.Dispose();
            
            _stagingBuffer = Unsafe.As<IVulkanStagingDataBuffer<T>>(stagingBuffer);
            _stagingBufferOffset = 0ul;
            _stagingBufferElementIndexOffset = 0u;
            _ownStagingBuffer = false;
        }
        public void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer, uint elementIndexOffset)
        {
            _stagingBuffer = Unsafe.As<IVulkanStagingDataBuffer<T>>(stagingBuffer);
            _stagingBufferOffset = elementIndexOffset * _elementOffset;
            _stagingBufferElementIndexOffset = elementIndexOffset;
            _ownStagingBuffer = false;
        }
        public void ReleaseStagingBuffer()
        {
            if (_ownStagingBuffer && _stagingBuffer != null)
                _stagingBuffer.Dispose();

            _stagingBuffer = null;
            _stagingBufferOffset = 0ul;
            _stagingBufferElementIndexOffset = 0u;
            _ownStagingBuffer = true;
        }
        

        public override void Resize(ulong size)
        {
            base.Resize(size);
            if (_ownStagingBuffer && _stagingBuffer != null)
                _stagingBuffer.Resize(size);
        }
        public override void ResizeCapacity(uint dataCapacity)
        {
            base.ResizeCapacity(dataCapacity);
            if (_ownStagingBuffer && _stagingBuffer != null)
                _stagingBuffer.ResizeCapacity(dataCapacity);
        }

        #endregion

    }
}
