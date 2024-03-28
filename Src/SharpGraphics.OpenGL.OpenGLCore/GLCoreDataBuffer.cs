using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.OpenGLCore.CommandBuffers;
using SharpGraphics.OpenGL.OpenGLCore.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    
    internal interface IGLCoreDataBuffer : IGLDataBuffer
    {

    }
    internal interface IGLCoreDataBuffer<T> : IDataBuffer<T>, IGLCoreDataBuffer where T : unmanaged
    {
    }

    internal class GLCoreDataBuffer<T> : GLDataBuffer<T>, IGLCoreDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private readonly MappableMemoryType? _memoryType;
        private bool _sizeInitialized = false;

        #endregion

        #region Constructors

        internal GLCoreDataBuffer(GLCoreGraphicsDevice device, uint dataCapacity, DataBufferType bufferType, DataBufferType alignmentType, MappableMemoryType? memoryType) :
#if DEBUG
            base(device, dataCapacity, bufferType, alignmentType, !memoryType.HasValue)
#else
            base(device, dataCapacity, bufferType, alignmentType)
#endif
        {
            _memoryType = memoryType;
        }

        #endregion

        #region Protected Methods

        protected override GLWaitableCommand AddFlushMappedDeviceMemoryCommand(GLCommandProcessor commandProcessor, ulong offset, ulong size)
        {
            GLCoreFlushMappedDeviceMemoryCommand flushCommand = new GLCoreFlushMappedDeviceMemoryCommand();
            commandProcessor.Submit(flushCommand);
            return flushCommand;
        }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported && _device.GLFeatures.IsBufferStorageSupported)
                GL.CreateBuffers(1, out _id);
            else GL.GenBuffers(1, out _id);

            if (_size > 0ul)
                GLResize((int)_size);
        }
        public override void GLFree()
        {
            if (_id > 0)
            {
                GL.DeleteBuffer(_id);
                _id = 0;
            }
            _sizeInitialized = false;
        }

        public override void GLBindIndexBuffer() => GL.BindBuffer(BufferTarget.ElementArrayBuffer, _id);
        public override void GLBindUniformBuffer(uint index) => GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, (uint)_id);
        public override void GLBindUniformBuffer(uint index, IntPtr offset, IntPtr size) => GL.BindBufferRange(BufferRangeTarget.UniformBuffer, index, (uint)_id, offset, size);
        public override void GLBindPackBuffer() => GL.BindBuffer(BufferTarget.PixelPackBuffer, _id);
        public override void GLBindUnPackBuffer() => GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _id);

        public override void GLBufferData(IntPtr offset, IntPtr size, IntPtr data)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.NamedBufferSubData(_id, offset, size, data);
            else
            {
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                GL.BufferSubData(BufferTarget.CopyWriteBuffer, offset, size, data);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            }
        }
        public override void GLBufferData(IntPtr offset, T data)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.NamedBufferSubData(_id, offset, new IntPtr(_dataTypeSize), ref data);
            else
            {
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                GL.BufferSubData(BufferTarget.CopyWriteBuffer, offset, new IntPtr(_dataTypeSize), ref data);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            }
        }
        public override void GLReadData(IntPtr offset, IntPtr size, IntPtr data)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.GetNamedBufferSubData(_id, offset, size, data);
            else
            {
                GL.BindBuffer(BufferTarget.CopyReadBuffer, _id);
                GL.GetBufferSubData(BufferTarget.CopyReadBuffer, offset, size, data);
                GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
            }
        }
        public override void GLReadData(IntPtr offset, ref T data)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.GetNamedBufferSubData(_id, offset, new IntPtr(_dataTypeSize), ref data);
            else
            {
                GL.BindBuffer(BufferTarget.CopyReadBuffer, _id);
                GL.GetBufferSubData(BufferTarget.CopyReadBuffer, offset, new IntPtr(_dataTypeSize), ref data);
                GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
            }
        }

        public override void GLResize(int size)
        {
            if (_device.GLFeatures.IsBufferStorageSupported)
            {
                if (_sizeInitialized)
                {
                    GLFree();
                    GLInitialize();
                }
                else
                {
                    if (_device.GLFeatures.IsDirectStateAccessSupported)
                        GL.NamedBufferStorage(_id, new IntPtr((int)_size), IntPtr.Zero, _type.ToBufferStorageFlags(_memoryType));
                    else
                    {
                        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                        GL.BufferStorage(BufferTarget.CopyWriteBuffer, new IntPtr((int)_size), IntPtr.Zero, _type.ToBufferStorageFlags(_memoryType));
                        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
                    }
                }
            }
            else
            {
                if (_device.GLFeatures.IsDirectStateAccessSupported)
                    GL.NamedBufferData(_id, new IntPtr((int)_size), IntPtr.Zero, _type.ToBufferUsageHint(_memoryType));
                else
                {
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                    GL.BufferData(BufferTarget.CopyWriteBuffer, new IntPtr((int)_size), IntPtr.Zero, _type.ToBufferUsageHint(_memoryType));
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
                }
            }
            _sizeInitialized = true;
        }

        public override void GLCopyTo(IGLDataBuffer destination, IntPtr size, IntPtr sourceOffset, IntPtr destinationOffset)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.CopyNamedBufferSubData(_id, destination.ID, sourceOffset, destinationOffset, size);
            else
            {
                GL.BindBuffer(BufferTarget.CopyReadBuffer, _id);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, destination.ID);
                GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, sourceOffset, destinationOffset, size);
                GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            }
        }

        public override IntPtr GLMapMemory(IntPtr offset, IntPtr size)
        {
            BufferAccessMask accessMask = BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit;
            if (_device.GLFeatures.IsBufferPersistentMappingSupported)
                accessMask |= BufferAccessMask.MapPersistentBit;
            if (_memoryType.HasValue && (_memoryType.Value.HasFlag(MappableMemoryType.Coherent) || _memoryType.Value.HasFlag(MappableMemoryType.Cached)) && _device.GLFeatures.IsBufferCoherentMappingSupported)
                accessMask |= BufferAccessMask.MapCoherentBit;

            if (_device.GLFeatures.IsDirectStateAccessSupported)
                return GL.MapNamedBufferRange(_id, offset, size, accessMask);
            else
            {
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                IntPtr result = GL.MapBufferRange(BufferTarget.CopyWriteBuffer, offset, size, accessMask);
                if (_device.GLFeatures.IsBufferPersistentMappingSupported)
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
                return result;
            }
        }
        public override void GLFlushMappedSystemMemory(IntPtr offset, IntPtr size)
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.FlushMappedNamedBufferRange(_id, offset, size);
            else
            {
                if (_device.GLFeatures.IsBufferPersistentMappingSupported)
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                GL.FlushMappedBufferRange(BufferTarget.CopyWriteBuffer, offset, size);
                if (_device.GLFeatures.IsBufferPersistentMappingSupported)
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            }
        }
        public override void GLUnMapMemory()
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.UnmapNamedBuffer(_id);
            else
            {
                if (_device.GLFeatures.IsBufferPersistentMappingSupported)
                    GL.BindBuffer(BufferTarget.CopyWriteBuffer, _id);
                GL.UnmapBuffer(BufferTarget.CopyWriteBuffer);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            }
        }

        #endregion

    }
}
