using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.OpenGLES30.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{

    internal interface IGLES30DataBuffer : IGLDataBuffer
    {
    }
    internal interface IGLES30DataBuffer<T> : IDataBuffer<T>, IGLES30DataBuffer where T : unmanaged
    {
    }

    internal class GLES30DataBuffer<T> : GLDataBuffer<T>, IGLES30DataBuffer<T> where T : unmanaged
    {

        #region Fields

        private readonly MappableMemoryType? _memoryType;

#if ANDROID
        private readonly BufferUsage _usage;
#else
        private readonly BufferUsageHint _usage;
#endif

        #endregion

        #region Constructors

        protected internal GLES30DataBuffer(GLES30GraphicsDevice device, uint dataCapacity, DataBufferType bufferType, DataBufferType alignmentType, MappableMemoryType? memoryType) :
#if DEBUG
            base(device, dataCapacity, bufferType, alignmentType, !memoryType.HasValue)
#else
            base(device, dataCapacity, bufferType, alignmentType)
#endif
        {
            _memoryType = memoryType;
#if ANDROID
            _usage = bufferType.ToBufferUsage(memoryType);
#else
            _usage = bufferType.ToBufferUsageHint(memoryType);
#endif
        }

        //~GLES30DataBuffer() => Dispose(false); //Base dispose is fine, it will call Free from GL thread

        #endregion

        #region Protected Methods

        protected override GLWaitableCommand AddFlushMappedDeviceMemoryCommand(GLCommandProcessor commandProcessor, ulong offset, ulong size)
        {
            GLES30FlushMappedDeviceMemoryCommand flushCommand = new GLES30FlushMappedDeviceMemoryCommand();
            commandProcessor.Submit(flushCommand);
            return flushCommand;
        }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            GL.GenBuffers(1, out _id);
            if (_size > 0ul)
                GLResize((int)_size);
        }
        public override void GLFree()
        {
            if (_id > 0)
            {
                GL.DeleteBuffers(1, ref _id);
                _id = 0;
            }
        }

        public override void GLBindIndexBuffer() => GL.BindBuffer(BufferTarget.ElementArrayBuffer, _id);
        public override void GLBindUniformBuffer(uint index) => GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, (uint)_id);
        public override void GLBindUniformBuffer(uint index, IntPtr offset, IntPtr size) => GL.BindBufferRange(BufferRangeTarget.UniformBuffer, index, _id, offset, size);
        public override void GLBindPackBuffer() => GL.BindBuffer(BufferTarget.PixelPackBuffer, _id);
        public override void GLBindUnPackBuffer() => GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _id);

        public override void GLBufferData(IntPtr offset, IntPtr size, IntPtr data)
        {
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            GL.BufferSubData(BufferTarget.CopyWriteBuffer, offset, size, data);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }
        public override void GLBufferData(IntPtr offset, T data)
        {
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            GL.BufferSubData(BufferTarget.CopyWriteBuffer, offset, new IntPtr(_dataTypeSize), ref data);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }
        public override void GLReadData(IntPtr offset, IntPtr size, IntPtr data)
        {
            IntPtr mappedMemory = GLMapMemory(offset, size);
            unsafe { System.Buffer.MemoryCopy(mappedMemory.ToPointer(), data.ToPointer(), (long)size, (long)size); }
            GLUnMapMemory();
        }
        public override void GLReadData(IntPtr offset, ref T data)
        {
            data = Marshal.PtrToStructure<T>(GLMapMemory(offset, new IntPtr(_dataTypeSize)));
            GLUnMapMemory();
        }

        public override void GLResize(int size)
        {
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            GL.BufferData(BufferTarget.CopyWriteBuffer, new IntPtr(size), IntPtr.Zero, _usage);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }

        public override void GLCopyTo(IGLDataBuffer destination, IntPtr size, IntPtr sourceOffset, IntPtr destinationOffset)
        {
            GL.BindBuffer(BufferTarget.CopyReadBuffer, _id);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, destination.ID);
            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, sourceOffset, destinationOffset, size);
            GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }

        public override IntPtr GLMapMemory(IntPtr offset, IntPtr size)
        {
            BufferAccessMask accessMask = BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit |
                BufferAccessMask.MapFlushExplicitBit;
            //Persistent and Coherent Mapping not supported

            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            IntPtr result = GL.MapBufferRange(BufferTarget.CopyWriteBuffer, offset, size, accessMask);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            return result;
        }
        public override void GLFlushMappedSystemMemory(IntPtr offset, IntPtr size)
        {
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            GL.FlushMappedBufferRange(BufferTarget.CopyWriteBuffer, offset, size);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }
        public override void GLUnMapMemory()
        {
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
            GL.UnmapBuffer(BufferTarget.CopyWriteBuffer);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        }

        #endregion

    }
}
