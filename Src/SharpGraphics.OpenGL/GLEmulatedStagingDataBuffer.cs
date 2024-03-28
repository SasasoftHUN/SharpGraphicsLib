using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL
{

    internal interface IGLEmulatedStagingDataBuffer : IStagingDataBuffer
    {
        IntPtr Pointer { get; }
    }
    internal interface IGLEmulatedStagingDataBuffer<T> : IGLEmulatedStagingDataBuffer, IStagingDataBuffer<T> where T : unmanaged { }

    internal sealed class GLEmulatedStagingDataBuffer<T> : DataBuffer<T>, IGLEmulatedStagingDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private IntPtr _pointer;
        private IntPtr _mappedPointer;

        #endregion

        #region Properties

        public IntPtr Pointer => _pointer;

        public bool IsMapped => _mappedPointer != IntPtr.Zero;
        public IntPtr MappedPointer => _mappedPointer;
        public unsafe void* MappedRawPointer => _mappedPointer.ToPointer();

        #endregion

        #region Constructors

        public GLEmulatedStagingDataBuffer(GraphicsDevice device, uint dataCapacity, DataBufferType alignmentType) :
            base(device, dataCapacity, DataBufferType.CopySource, alignmentType)
        {
            if (dataCapacity > 0u)
                _pointer = Marshal.AllocHGlobal((int)_size);
        }

        #endregion

        #region Private Methods



        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            if (_pointer != IntPtr.Zero)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing GLStagingDataBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                Marshal.FreeHGlobal(_pointer);
                _pointer = IntPtr.Zero;

                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public unsafe override void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul)
            => Buffer.MemoryCopy(data.ToPointer(), (_pointer + (int)offset).ToPointer(), size, size);
        public override void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u)
            => Marshal.StructureToPtr(data, _pointer + (int)(elementIndexOffset * _elementOffset), false);
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u)
        {
            if (_dataTypeSize == _elementOffset)
            {
                ulong sizeToCopy = _dataTypeSize * (ulong)data.Length;
                unsafe
                {
                    fixed (T* ptr = data)
                        Buffer.MemoryCopy(ptr, (_pointer + (int)(elementIndexOffset * _dataTypeSize)).ToPointer(), _size, sizeToCopy);
                }
            }
            else (_pointer + (int)(elementIndexOffset * _elementOffset)).CopyWithAlignment(data, (int)_elementOffset);
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u)
        {
            if (_dataTypeSize == _elementOffset)
            {
                ulong sizeToCopy = _dataTypeSize * (ulong)data.Length;
                unsafe
                {
                    using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
                    Buffer.MemoryCopy(pinnedData.RawPointer, (_pointer + (int)(elementIndexOffset * _dataTypeSize)).ToPointer(), _size, sizeToCopy);
                }
            }
            else (_pointer + (int)(elementIndexOffset * _elementOffset)).CopyWithAlignment(data, (int)_elementOffset);
        }

        public override void Resize(ulong size)
        {
            base.Resize(size);

            _pointer = _pointer != IntPtr.Zero ?
                Marshal.ReAllocHGlobal(_pointer, (IntPtr)_size) :
                Marshal.AllocHGlobal((int)_size);
        }
        public override void ResizeCapacity(uint dataCapacity)
        {
            base.ResizeCapacity(dataCapacity);

            _pointer = _pointer != IntPtr.Zero ?
                Marshal.ReAllocHGlobal(_pointer, (IntPtr)_size) :
                Marshal.AllocHGlobal((int)_size);
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, ulong size, ulong sourceOffset = 0ul, ulong destinationOffset = 0ul)
        {
            AssertCopyTo(destination, sourceOffset, destinationOffset);

            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            if (destination is GLEmulatedStagingDataBuffer<T> stagingDestination)
                glCommandBuffer.AddDataTransferCommand(new GLCopyEmulatedStagingBufferCommand(_pointer + (int)sourceOffset, stagingDestination._pointer + (int)destinationOffset, size));
            else glCommandBuffer.AddDataTransferCommand(new GLBufferDataFromEmulatedStagingBufferCommand(Unsafe.As<IGLDataBuffer>(destination), new IntPtr((int)destinationOffset), this, (int)sourceOffset, (int)size));
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in CopyBufferTextureRange range)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            glCommandBuffer.AddDataTransferCommand(new GLTexSubImageFromEmulatedStagingBufferCommand(Unsafe.As<IGLTexture>(destination), this, range));
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            glCommandBuffer.AddDataTransferCommand(new GLTexSubImageMultipleRangesFromEmulatedStagingBufferCommand(Unsafe.As<IGLTexture>(destination), glCommandBuffer.MemoryAllocator, this, ranges));
        }

        public IntPtr MapMemory()
        {
            _mappedPointer = _pointer;
            return _mappedPointer;
        }
        public IntPtr MapMemory(ulong offset, ulong size)
        {
            _mappedPointer = _pointer + (int)offset;
            return _mappedPointer;
        }
        public IntPtr MapMemoryElements(uint elementIndexOffset, uint elementCount)
        {
            _mappedPointer = _pointer + (int)(elementIndexOffset * _elementOffset);
            return _mappedPointer;
        }

        public void FlushMappedSystemMemory() { }
        public void FlushMappedDeviceMemory() { }
        public void FlushMappedSystemMemory(ulong offset, ulong size) { }
        public void FlushMappedDeviceMemory(ulong offset, ulong size) { }
        public void FlushMappedSystemMemoryElements(uint elementIndexOffset, uint elementCount) { }
        public void FlushMappedDeviceMemoryElements(uint elementIndexOffset, uint elementCount) { }

        public T[] GatherMappedDataElements()
        {
#if DEBUG
            Debug.Assert(_mappedPointer != IntPtr.Zero, $"Trying to Gather from VulkanMappableDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif
            return _mappedPointer.GatherAlignedElements<T>((int)_capacity, (int)_elementOffset, (int)_dataTypeSize);
        }

        public void UnMapMemory() => _mappedPointer = IntPtr.Zero;

        #endregion

    }
}
