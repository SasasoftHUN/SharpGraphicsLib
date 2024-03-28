using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL
{
    public interface IGLDataBuffer : IGLResource
    {

        uint ID { get; }

        void GLBindIndexBuffer();
        void GLBindUniformBuffer(uint index);
        void GLBindUniformBuffer(uint index, IntPtr offset, IntPtr size);
        void GLBindPackBuffer();
        void GLBindUnPackBuffer();

        void GLBufferData(IntPtr offset, IntPtr size, IntPtr data);
        void GLReadData(IntPtr offset, IntPtr size, IntPtr data);

        void GLResize(int size);

        void GLCopyTo(IGLDataBuffer destination, IntPtr size, IntPtr sourceOffset, IntPtr destinationOffset);

        IntPtr GLMapMemory(IntPtr offset, IntPtr size);
        void GLFlushMappedSystemMemory(IntPtr offset, IntPtr size);
        void GLUnMapMemory();

    }
    public interface IGLDataBuffer<T> : IGLDataBuffer, IDeviceOnlyDataBuffer<T>, IMappableDataBuffer<T> where T : unmanaged
    {

        void GLBufferData(IntPtr offset, T data);
        void GLReadData(IntPtr offset, ref T data);

    }


    public abstract class GLDataBuffer<T> : DataBuffer<T>, IGLDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private bool _isDisposed;

        private IDataBuffer<T>? _stagingBuffer = null;
        private ulong _stagingBufferOffset = 0ul;
        private uint _stagingBufferElementIndexOffset = 0u;

        private ulong _mappedMemoryOffset;
        private ulong _mappedMemorySize;

#if DEBUG
        private bool _isDeviceOnly;
#endif

        protected readonly GLGraphicsDevice _device;

        protected uint _id;

        protected IntPtr _mappedMemory;

        #endregion

        #region Properties

        public uint ID => _id;

        public bool IsMapped => _mappedMemory != IntPtr.Zero;
        public IntPtr MappedPointer => _mappedMemory;
        public unsafe void* MappedRawPointer => _mappedMemory.ToPointer();

        #endregion

        #region Constructors

#if DEBUG
        protected internal GLDataBuffer(GLGraphicsDevice device, uint dataCapacity, DataBufferType type, DataBufferType alignmentType, bool isDeviceOnly) : base(device, dataCapacity, type.MakeBufferTypeTrivial(), alignmentType)
#else
        protected internal GLDataBuffer(GLGraphicsDevice device, uint dataCapacity, DataBufferType type, DataBufferType alignmentType) : base(device, dataCapacity, type.MakeBufferTypeTrivial(), alignmentType)
#endif
        {
            _isDisposed = false;
            _device = device;
#if DEBUG
            _isDeviceOnly = isDeviceOnly;
#endif
        }

        ~GLDataBuffer() => Dispose(false);

        #endregion

        #region Protected Methods

        protected abstract GLWaitableCommand AddFlushMappedDeviceMemoryCommand(GLCommandProcessor commandProcessor, ulong offset, ulong size);

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing)
                    // Dispose managed state (managed objects).

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL DataBuffer<{typeof(T).FullName}> from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                    _device.FreeResource(this);
                else Debug.WriteLine($"Warning: GLDataBuffer<{typeof(T).FullName}> cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        //Buffer Data
        public override void StoreData(GraphicsCommandBuffer commandBuffer, IntPtr data, ulong size, ulong offset = 0ul)
        {
            AssertStoreData(offset + size);
            if (_stagingBuffer == null)
            {
                Debug.Assert(_type.HasFlag(DataBufferType.Store), $"GLDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
                GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
                glCommandBuffer.AddDataTransferCommand(new GLBufferDataPtrCommand(this, new IntPtr((int)offset), glCommandBuffer.MemoryAllocator, data, size));
            }
            else
            {
                ulong stagingBufferOffset = _stagingBufferOffset + offset;
                _stagingBuffer.StoreData(commandBuffer, data, size, stagingBufferOffset);
                _stagingBuffer.CopyTo(commandBuffer, this, size, stagingBufferOffset, offset);
            }
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, ref T data, uint elementIndexOffset = 0u)
        {
            AssertStoreDataElements(elementIndexOffset + 1u);
            if (_stagingBuffer == null)
            {
                Debug.Assert(_type.HasFlag(DataBufferType.Store), $"GLDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
                GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
                glCommandBuffer.AddDataTransferCommand(new GLBufferDataSingleCommand<T>(this, new IntPtr(elementIndexOffset * _elementOffset), data));
            }
            else
            {
                uint stagingBufferOffset = _stagingBufferElementIndexOffset + elementIndexOffset;
                _stagingBuffer.StoreData(commandBuffer, ref data, stagingBufferOffset);
                _stagingBuffer.CopyElementsTo(commandBuffer, this, 1u, stagingBufferOffset, elementIndexOffset);
            }
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u)
        {
            AssertStoreDataElements(elementIndexOffset + (uint)data.Length);
            if (_stagingBuffer == null)
            {
                Debug.Assert(_type.HasFlag(DataBufferType.Store), $"GLDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
                GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
                glCommandBuffer.AddDataTransferCommand(new GLBufferDataArrayCommand<T>(this, new IntPtr(elementIndexOffset * _elementOffset), glCommandBuffer.MemoryAllocator, data));
            }
            else
            {
                uint stagingBufferOffset = _stagingBufferElementIndexOffset + elementIndexOffset;
                _stagingBuffer.StoreData(commandBuffer, data, stagingBufferOffset);
                _stagingBuffer.CopyElementsTo(commandBuffer, this, (uint)data.Length, stagingBufferOffset, elementIndexOffset);
            }
        }
        public override void StoreData(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u)
        {
            AssertStoreDataElements(elementIndexOffset + (uint)data.Length);
            if (_stagingBuffer == null)
            {
                Debug.Assert(_type.HasFlag(DataBufferType.Store), $"GLDataBuffer<{typeof(T).FullName}> has no Store usage for StoreData!");
                GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
                glCommandBuffer.AddDataTransferCommand(new GLBufferDataArrayCommand<T>(this, new IntPtr(elementIndexOffset * _elementOffset), glCommandBuffer.MemoryAllocator, data));
            }
            else
            {
                uint stagingBufferOffset = _stagingBufferElementIndexOffset + elementIndexOffset;
                _stagingBuffer.StoreData(commandBuffer, data, stagingBufferOffset);
                _stagingBuffer.CopyElementsTo(commandBuffer, this, (uint)data.Length, stagingBufferOffset, elementIndexOffset);
            }
        }

        //Resizing
        public override void Resize(ulong size)
        {
            base.Resize(size);
            _device.SubmitCommand(new GLResizeDataBufferCommand(this, (int)size));
        }
        public override void ResizeCapacity(uint dataCapacity)
        {
            base.ResizeCapacity(dataCapacity);
            _device.SubmitCommand(new GLResizeDataBufferCommand(this, (int)(dataCapacity * _elementOffset)));
        }

        //Staging Buffers
        public void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer)
        {
            _stagingBuffer = stagingBuffer;
            _stagingBufferOffset = 0ul;
            _stagingBufferElementIndexOffset = 0u;
        }
        public void UseStagingBuffer(IStagingDataBuffer<T> stagingBuffer, uint elementIndexOffset)
        {
            _stagingBuffer = stagingBuffer;
            _stagingBufferOffset = (ulong)elementIndexOffset * _elementOffset;
            _stagingBufferElementIndexOffset = elementIndexOffset;
        }
        public void ReleaseStagingBuffer()
        {
            _stagingBuffer = null;
            _stagingBufferOffset = 0ul;
            _stagingBufferElementIndexOffset = 0u;
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, ulong size, ulong sourceOffset = 0, ulong destinationOffset = 0)
        {
            AssertCopyTo(destination, sourceOffset, destinationOffset);
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);

            if (destination is IGLEmulatedStagingDataBuffer stagingDestination)
                glCommandBuffer.AddDataTransferCommand(new GLReadBufferIntoEmulatedStagingBufferCommand(this, new IntPtr((int)sourceOffset), stagingDestination, (int)destinationOffset, (int)size));
            else glCommandBuffer.AddDataTransferCommand(new GLCopyDataBufferCommand(this, Unsafe.As<IGLDataBuffer>(destination), new IntPtr((int)size), new IntPtr((int)sourceOffset), new IntPtr((int)destinationOffset)));
        }

        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in CopyBufferTextureRange range)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            glCommandBuffer.AddDataTransferCommand(new GLTexSubImageFromUnPackBufferCommand(Unsafe.As<IGLTexture>(destination), this, range));
        }
        public override void CopyTo(GraphicsCommandBuffer commandBuffer, ITexture destination, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            glCommandBuffer.AddDataTransferCommand(new GLTexSubImageMultipleRangesFromUnPackBufferCommand(Unsafe.As<IGLTexture>(destination), glCommandBuffer.MemoryAllocator, this, ranges));
        }

        //Mapping
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr MapMemory() => MapMemory(0ul, _size);
        public IntPtr MapMemory(ulong offset, ulong size)
        {
#if DEBUG
            Debug.Assert(_mappedMemory == IntPtr.Zero, $"Trying to MapMemory GLDataBuffer<{typeof(T).FullName}> which is already Mapped!");
#endif

            GLMapMemoryCommand mapMemoryCommand = new GLMapMemoryCommand(this, new IntPtr((int)offset), new IntPtr((int)size));
            _device.CommandProcessor.Submit(mapMemoryCommand);

            if (mapMemoryCommand.Wait() && mapMemoryCommand.Result.HasValue)
            {
                _mappedMemory = mapMemoryCommand.Result.Value;
                _mappedMemoryOffset = offset;
                _mappedMemorySize = size;
                return _mappedMemory;
            }
            else return IntPtr.Zero;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr MapMemoryElements(uint elementIndexOffset, uint elementCount) => MapMemory(elementIndexOffset * _elementOffset, elementCount * _elementOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedSystemMemory() => FlushMappedSystemMemory(_mappedMemoryOffset, _mappedMemorySize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedDeviceMemory() => FlushMappedDeviceMemory(_mappedMemoryOffset, _mappedMemorySize);
        public void FlushMappedSystemMemory(ulong offset, ulong size)
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to FlushMappedSystemMemory of GLDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif

            _device.CommandProcessor.Submit(new GLFlushMappedSystemMemoryCommand(this, new IntPtr((int)offset), new IntPtr((int)size)));
        }
        public void FlushMappedDeviceMemory(ulong offset, ulong size)
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to FlushMappedSystemMemory of GLDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif
            
            AddFlushMappedDeviceMemoryCommand(_device.CommandProcessor, offset, size).Wait();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedSystemMemoryElements(uint elementIndexOffset, uint elementCount) => FlushMappedSystemMemory(elementIndexOffset * _elementOffset, elementCount * _elementOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushMappedDeviceMemoryElements(uint elementIndexOffset, uint elementCount) => FlushMappedDeviceMemoryElements(elementIndexOffset * _elementOffset, elementCount * _elementOffset);

        public T[] GatherMappedDataElements()
        {
#if DEBUG
            Debug.Assert(_mappedMemory != IntPtr.Zero, $"Trying to Gather from GLDataBuffer<{typeof(T).FullName}> which is not Mapped!");
#endif
            return _mappedMemory.GatherAlignedElements<T>((int)_capacity, (int)_elementOffset, (int)_dataTypeSize);
        }

        public void UnMapMemory()
        {
            if (_mappedMemory != IntPtr.Zero)
            {
                _device.CommandProcessor.Submit(new GLUnMapMemoryCommand(this));
                _mappedMemory = IntPtr.Zero;
            }
        }

        //GL Command Execution
        public abstract void GLInitialize();
        public abstract void GLFree();

        public abstract void GLBindIndexBuffer();
        public abstract void GLBindUniformBuffer(uint index);
        public abstract void GLBindUniformBuffer(uint index, IntPtr offset, IntPtr size);
        public abstract void GLBindPackBuffer();
        public abstract void GLBindUnPackBuffer();

        public abstract void GLBufferData(IntPtr offset, IntPtr size, IntPtr data);
        public abstract void GLBufferData(IntPtr offset, T data);
        public abstract void GLReadData(IntPtr offset, IntPtr size, IntPtr data);
        public abstract void GLReadData(IntPtr offset, ref T data);

        public abstract void GLResize(int size);

        public abstract void GLCopyTo(IGLDataBuffer destination, IntPtr size, IntPtr sourceOffset, IntPtr destinationOffset);

        public abstract IntPtr GLMapMemory(IntPtr offset, IntPtr size);
        public abstract void GLFlushMappedSystemMemory(IntPtr offset, IntPtr size);
        public abstract void GLUnMapMemory();

        #endregion

    }
}
