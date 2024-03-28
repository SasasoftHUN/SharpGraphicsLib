using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Graphics.ES30;
using SharpGraphics.OpenGL.OpenGLES30.CommandBuffers;
using SharpGraphics.OpenGL.OpenGLES30.Commands;

namespace SharpGraphics.OpenGL.OpenGLES30
{

    internal interface IGLES30VertexDataBuffer : IGLES30DataBuffer
    {
        void GLBindVertexBuffer(in VertexInputs vertexInputs, uint binding, int offset);
        void GLBindVertexBuffers(in VertexInputs vertexInputs, in ReadOnlySpan<GLES30VertexBufferBinding> vertexBuffers);
    }
    internal interface IGLES30VertexDataBuffer<T> : IGLES30DataBuffer, IGLES30VertexDataBuffer where T : unmanaged
    {
    }

    internal sealed class GLES30VertexDataBuffer<T> : GLES30DataBuffer<T>, IGLES30VertexDataBuffer<T> where T : unmanaged
    {

        #region Fields

        private int _vaoID;

        private VertexInputs? _vaoBindings;
        private bool _vaoSingleVBOBind;
        private uint[]? _vaoMultiVBOBindIDs;

        #endregion

        #region Constructors

        internal GLES30VertexDataBuffer(GLES30GraphicsDevice device, DataBufferType bufferType, uint dataCapacity, MappableMemoryType? memoryType, bool isAligned) :
            base(device, dataCapacity, DataBufferType.VertexData | bufferType, isAligned ? DataBufferType.VertexData | bufferType : DataBufferType.Unknown, memoryType) { }

        //~GLES30VertexDataBuffer() => Dispose(false); //Base dispose is fine, it will call Free from GL thread

        #endregion

        #region Private Methods

        private void GLInitializeVAO(in VertexInputs vertexInputs, in ReadOnlySpan<GLES30VertexBufferBinding> vertexBuffers)
        {
            GLFreeVAO();

            GL.GenVertexArrays(1, out _vaoID);
            GL.BindVertexArray(_vaoID);

            ReadOnlySpan<VertexInputBinding> bindings = vertexInputs.Bindings;
            foreach (VertexInputAttribute attribute in vertexInputs.Attributes)
            {
                int stride = (int)bindings[(int)attribute.binding].stride;
                int offset = (int)attribute.offset;

                for (int i = 0; i < vertexBuffers.Length; i++)
                    if (vertexBuffers[i].binding == attribute.binding)
                    {
                        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffers[i].vertexBufferID);
                        offset += vertexBuffers[i].offset;
                        break;
                    }

                GL.EnableVertexAttribArray(attribute.location);
                switch (attribute.format)
                {
                    case DataFormat.R8un: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.R8n: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.R8us: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.R8s: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.R8ui: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.R8i: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.R8srgb: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;

                    case DataFormat.RG8un: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.RG8n: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.RG8us: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RG8s: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RG8ui: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RG8i: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RG8srgb: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;

                    case DataFormat.RGB8un: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.RGB8n: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.RGB8us: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RGB8s: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RGB8ui: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RGB8i: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RGB8srgb: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.BGR8un: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.BGR8n: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.BGR8us: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.BGR8s: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.BGR8ui: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.BGR8i: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.BGR8srgb: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;

                    case DataFormat.RGBA8un: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.RGBA8n: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.RGBA8us: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RGBA8s: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RGBA8ui: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.RGBA8i: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.RGBA8srgb: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.BGRA8un: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;
                    case DataFormat.BGRA8n: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, true, stride, offset); break;
                    case DataFormat.BGRA8us: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.BGRA8s: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.BGRA8ui: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, false, stride, offset); break;
                    case DataFormat.BGRA8i: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Byte, false, stride, offset); break;
                    case DataFormat.BGRA8srgb: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedByte, true, stride, offset); break;


                    case DataFormat.R16un: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedShort, true, stride, offset); break;
                    case DataFormat.R16n: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Short, true, stride, offset); break;
                    case DataFormat.R16us: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.R16s: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.R16ui: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.R16i: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.R16f: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.HalfFloat, false, stride, offset); break;

                    case DataFormat.RG16un: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedShort, true, stride, offset); break;
                    case DataFormat.RG16n: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Short, true, stride, offset); break;
                    case DataFormat.RG16us: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RG16s: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RG16ui: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RG16i: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RG16f: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.HalfFloat, false, stride, offset); break;

                    case DataFormat.RGB16un: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedShort, true, stride, offset); break;
                    case DataFormat.RGB16n: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Short, true, stride, offset); break;
                    case DataFormat.RGB16us: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RGB16s: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RGB16ui: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RGB16i: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RGB16f: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.HalfFloat, false, stride, offset); break;

                    case DataFormat.RGBA16un: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedShort, true, stride, offset); break;
                    case DataFormat.RGBA16n: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Short, true, stride, offset); break;
                    case DataFormat.RGBA16us: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RGBA16s: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RGBA16ui: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedShort, false, stride, offset); break;
                    case DataFormat.RGBA16i: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Short, false, stride, offset); break;
                    case DataFormat.RGBA16f: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.HalfFloat, false, stride, offset); break;


                    case DataFormat.R32ui: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.UnsignedInt, false, stride, offset); break;
                    case DataFormat.R32i: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Int, false, stride, offset); break;
                    case DataFormat.R32f: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RG32ui: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.UnsignedInt, false, stride, offset); break;
                    case DataFormat.RG32i: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Int, false, stride, offset); break;
                    case DataFormat.RG32f: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RGB32ui: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.UnsignedInt, false, stride, offset); break;
                    case DataFormat.RGB32i: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Int, false, stride, offset); break;
                    case DataFormat.RGB32f: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RGBA32ui: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.UnsignedInt, false, stride, offset); break;
                    case DataFormat.RGBA32i: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Int, false, stride, offset); break;
                    case DataFormat.RGBA32f: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Float, false, stride, offset); break;


                    case DataFormat.R64ui: GL.VertexAttribIPointer((int)attribute.location, 1, VertexAttribIntegerType.UnsignedInt, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.R64i: GL.VertexAttribIPointer((int)attribute.location, 1, VertexAttribIntegerType.Int, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.R64f: GL.VertexAttribPointer((int)attribute.location, 1, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RG64ui: GL.VertexAttribIPointer((int)attribute.location, 2, VertexAttribIntegerType.UnsignedInt, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RG64i: GL.VertexAttribIPointer((int)attribute.location, 2, VertexAttribIntegerType.Int, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RG64f: GL.VertexAttribPointer((int)attribute.location, 2, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RGB64ui: GL.VertexAttribIPointer((int)attribute.location, 3, VertexAttribIntegerType.UnsignedInt, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RGB64i: GL.VertexAttribIPointer((int)attribute.location, 3, VertexAttribIntegerType.Int, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RGB64f: GL.VertexAttribPointer((int)attribute.location, 3, VertexAttribPointerType.Float, false, stride, offset); break;

                    case DataFormat.RGBA64ui: GL.VertexAttribIPointer((int)attribute.location, 4, VertexAttribIntegerType.UnsignedInt, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RGBA64i: GL.VertexAttribIPointer((int)attribute.location, 4, VertexAttribIntegerType.Int, stride, (IntPtr)attribute.offset); break;
                    case DataFormat.RGBA64f: GL.VertexAttribPointer((int)attribute.location, 4, VertexAttribPointerType.Float, false, stride, offset); break;

                    default:
                        throw new ArgumentException($"Attribute format {attribute.format} is not supported in an OpenGL ES 3.0 Vertex Array.", "vertexInputs");
                }
            }

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            _vaoBindings = vertexInputs;
            if (vertexBuffers.Length == 1)
                _vaoSingleVBOBind = true;
            else
            {
                _vaoSingleVBOBind = false;
                if (_vaoMultiVBOBindIDs == null || _vaoMultiVBOBindIDs.Length != vertexBuffers.Length)
                    _vaoMultiVBOBindIDs = new uint[vertexBuffers.Length];
                for (int i = 0; i < vertexBuffers.Length; i++)
                    _vaoMultiVBOBindIDs[i] = vertexBuffers[i].vertexBufferID;
            }
        }
        private void GLFreeVAO()
        {
            if (_vaoID != 0)
            {
                GL.DeleteVertexArrays(1, ref _vaoID);
                _vaoID = 0;
            }
        }

        #endregion

        #region Public Methods

        public override void GLFree()
        {
            base.GLFree();
            GLFreeVAO();
        }

        public void GLBindVertexBuffer(in VertexInputs vertexInputs, uint binding, int offset)
        {
            if (!_vaoSingleVBOBind || _vaoBindings != vertexInputs)
                GLInitializeVAO(vertexInputs, stackalloc GLES30VertexBufferBinding[] { new GLES30VertexBufferBinding(binding, _id, offset) });
            GL.BindVertexArray(_vaoID);
        }
        public void GLBindVertexBuffers(in VertexInputs vertexInputs, in ReadOnlySpan<GLES30VertexBufferBinding> vertexBuffers)
        {
            if (_vaoSingleVBOBind || _vaoMultiVBOBindIDs == null || _vaoMultiVBOBindIDs.Length != vertexBuffers.Length || _vaoBindings != vertexInputs)
                GLInitializeVAO(vertexInputs, vertexBuffers);
            else for (int i = 0; i < vertexBuffers.Length; i++)
                if (_vaoMultiVBOBindIDs[i] != vertexBuffers[i].vertexBufferID)
                {
                    GLInitializeVAO(vertexInputs, vertexBuffers);
                    break;
                }

            GL.BindVertexArray(_vaoID);
        }

        #endregion

    }
}
