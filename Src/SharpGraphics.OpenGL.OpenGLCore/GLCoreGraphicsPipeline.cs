using OpenTK.Graphics.OpenGL;
using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreGraphicsPipeline : GLGraphicsPipeline
    {

        #region Fields

        private readonly PrimitiveType _primitiveType;

        private int _vaoID;

        #endregion

        #region Properties

        internal PrimitiveType GeometryType => _primitiveType;

        #endregion

        #region Constructors

        internal GLCoreGraphicsPipeline(GLCoreGraphicsDevice device, in GraphicsPipelineConstuctionParameters constuction) : base(device, constuction)
        {
            _primitiveType = constuction.geometryType.ToPrimitiveType();
        }

        #endregion

        #region Private Methods
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BindUniformBlockIndex(int programID, uint uniformBinding)
        {
            string uniformName = $"uniformblock_{uniformBinding}_uniform";
            int uniformIndex = GL.GetUniformBlockIndex(programID, uniformName);
            GL.UniformBlockBinding(programID, uniformIndex, (int)uniformBinding);
        }

        private static int GLCreateVAO(in VertexInputs vertexInput, bool dsa)
        {
            int vaoID;

            if (dsa)
            {
                GL.CreateVertexArrays(1, out vaoID);
                CreateVAOAttributes((uint)vaoID, vertexInput);
            }
            else
            {
                GL.GenVertexArrays(1, out vaoID);
                GL.BindVertexArray(vaoID);
                CreateVAOAttributes(vertexInput);
                GL.BindVertexArray(0);
            }

            return vaoID;
        }

        private static void CreateVAOAttributes(in VertexInputs vertexInput)
        {
            foreach (VertexInputAttribute attribute in vertexInput.Attributes)
            {
                switch (attribute.format)
                {
                    case DataFormat.R8un: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.R8n: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.R8us: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.R8s: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.R8ui: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.R8i: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.R8srgb: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RG8un: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RG8n: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RG8us: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RG8s: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RG8ui: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RG8i: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RG8srgb: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RGB8un: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RGB8n: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RGB8us: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGB8s: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGB8ui: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGB8i: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGB8srgb: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGR8un: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGR8n: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.BGR8us: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGR8s: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGR8ui: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGR8i: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGR8srgb: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RGBA8un: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RGBA8n: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RGBA8us: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGBA8s: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGBA8ui: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGBA8i: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGBA8srgb: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGRA8un: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGRA8n: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.BGRA8us: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGRA8s: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGRA8ui: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGRA8i: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGRA8srgb: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;


                    case DataFormat.R16un: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.R16n: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.R16us: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.R16s: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.R16ui: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.R16i: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.R16f: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RG16un: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RG16n: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RG16us: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RG16s: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RG16ui: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RG16i: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RG16f: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RGB16un: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RGB16n: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RGB16us: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGB16s: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGB16ui: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGB16i: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGB16f: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RGBA16un: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RGBA16n: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RGBA16us: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGBA16s: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGBA16ui: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGBA16i: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGBA16f: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.HalfFloat, false, attribute.offset); break;


                    case DataFormat.R32ui: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.R32i: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.R32f: GL.VertexAttribFormat(attribute.location, 1, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RG32ui: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RG32i: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RG32f: GL.VertexAttribFormat(attribute.location, 2, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RGB32ui: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RGB32i: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RGB32f: GL.VertexAttribFormat(attribute.location, 3, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RGBA32ui: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RGBA32i: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RGBA32f: GL.VertexAttribFormat(attribute.location, 4, VertexAttribType.Float, false, attribute.offset); break;


                    case DataFormat.R64ui: GL.VertexAttribIFormat(attribute.location, 1, VertexAttribIntegerType.UnsignedInt, attribute.offset); break;
                    case DataFormat.R64i: GL.VertexAttribIFormat(attribute.location, 1, VertexAttribIntegerType.Int, attribute.offset); break;
                    case DataFormat.R64f: GL.VertexAttribLFormat(attribute.location, 1, VertexAttribDoubleType.Double, attribute.offset); break;

                    case DataFormat.RG64ui: GL.VertexAttribIFormat(attribute.location, 2, VertexAttribIntegerType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RG64i: GL.VertexAttribIFormat(attribute.location, 2, VertexAttribIntegerType.Int, attribute.offset); break;
                    case DataFormat.RG64f: GL.VertexAttribLFormat(attribute.location, 2, VertexAttribDoubleType.Double, attribute.offset); break;

                    case DataFormat.RGB64ui: GL.VertexAttribIFormat(attribute.location, 3, VertexAttribIntegerType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RGB64i: GL.VertexAttribIFormat(attribute.location, 3, VertexAttribIntegerType.Int, attribute.offset); break;
                    case DataFormat.RGB64f: GL.VertexAttribLFormat(attribute.location, 3, VertexAttribDoubleType.Double, attribute.offset); break;

                    case DataFormat.RGBA64ui: GL.VertexAttribIFormat(attribute.location, 4, VertexAttribIntegerType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RGBA64i: GL.VertexAttribIFormat(attribute.location, 4, VertexAttribIntegerType.Int, attribute.offset); break;
                    case DataFormat.RGBA64f: GL.VertexAttribLFormat(attribute.location, 4, VertexAttribDoubleType.Double, attribute.offset); break;

                    default:
                        throw new ArgumentException($"Attribute format {attribute.format} is not supported in an OpenGL Vertex Array.", "vertexInputs");
                }

                GL.VertexAttribBinding(attribute.location, attribute.binding);
                GL.EnableVertexAttribArray(attribute.location);
            }
        }
        private static void CreateVAOAttributes(uint vaoID, in VertexInputs vertexInput)
        {
            foreach (VertexInputAttribute attribute in vertexInput.Attributes)
            {
                switch (attribute.format)
                {
                    case DataFormat.R8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.R8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.R8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.R8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.R8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.R8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.R8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RG8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RG8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RG8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RG8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RG8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RG8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RG8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RGB8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RGB8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RGB8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGB8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGB8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGB8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGB8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGR8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGR8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.BGR8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGR8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGR8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGR8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGR8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedByte, true, attribute.offset); break;

                    case DataFormat.RGBA8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.RGBA8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.RGBA8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGBA8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGBA8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.RGBA8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.RGBA8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGRA8un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;
                    case DataFormat.BGRA8n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, true, attribute.offset); break;
                    case DataFormat.BGRA8us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGRA8s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGRA8ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, false, attribute.offset); break;
                    case DataFormat.BGRA8i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Byte, false, attribute.offset); break;
                    case DataFormat.BGRA8srgb: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedByte, true, attribute.offset); break;


                    case DataFormat.R16un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.R16n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.R16us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.R16s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.R16ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.R16i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.R16f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RG16un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RG16n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RG16us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RG16s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RG16ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RG16i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RG16f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RGB16un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RGB16n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RGB16us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGB16s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGB16ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGB16i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGB16f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.HalfFloat, false, attribute.offset); break;

                    case DataFormat.RGBA16un: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedShort, true, attribute.offset); break;
                    case DataFormat.RGBA16n: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Short, true, attribute.offset); break;
                    case DataFormat.RGBA16us: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGBA16s: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGBA16ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedShort, false, attribute.offset); break;
                    case DataFormat.RGBA16i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Short, false, attribute.offset); break;
                    case DataFormat.RGBA16f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.HalfFloat, false, attribute.offset); break;


                    case DataFormat.R32ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.R32i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.R32f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 1, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RG32ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RG32i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RG32f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 2, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RGB32ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RGB32i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RGB32f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 3, VertexAttribType.Float, false, attribute.offset); break;

                    case DataFormat.RGBA32ui: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedInt, false, attribute.offset); break;
                    case DataFormat.RGBA32i: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Int, false, attribute.offset); break;
                    case DataFormat.RGBA32f: GL.VertexArrayAttribFormat(vaoID, attribute.location, 4, VertexAttribType.Float, false, attribute.offset); break;


                    case DataFormat.R64ui: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 1, VertexAttribType.UnsignedInt, attribute.offset); break;
                    case DataFormat.R64i: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 1, VertexAttribType.Int, attribute.offset); break;
                    case DataFormat.R64f: GL.VertexArrayAttribLFormat(vaoID, attribute.location, 1, VertexAttribType.Double, attribute.offset); break;

                    case DataFormat.RG64ui: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 2, VertexAttribType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RG64i: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 2, VertexAttribType.Int, attribute.offset); break;
                    case DataFormat.RG64f: GL.VertexArrayAttribLFormat(vaoID, attribute.location, 2, VertexAttribType.Double, attribute.offset); break;

                    case DataFormat.RGB64ui: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 3, VertexAttribType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RGB64i: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 3, VertexAttribType.Int, attribute.offset); break;
                    case DataFormat.RGB64f: GL.VertexArrayAttribLFormat(vaoID, attribute.location, 3, VertexAttribType.Double, attribute.offset); break;

                    case DataFormat.RGBA64ui: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 4, VertexAttribType.UnsignedInt, attribute.offset); break;
                    case DataFormat.RGBA64i: GL.VertexArrayAttribIFormat(vaoID, attribute.location, 4, VertexAttribType.Int, attribute.offset); break;
                    case DataFormat.RGBA64f: GL.VertexArrayAttribLFormat(vaoID, attribute.location, 4, VertexAttribType.Double, attribute.offset); break;

                    default:
                        throw new ArgumentException($"Attribute format {attribute.format} is not supported in an OpenGL Vertex Array.", "vertexInputs");
                }

                GL.VertexArrayAttribBinding(vaoID, attribute.location, attribute.binding);
                GL.EnableVertexArrayAttrib(vaoID, attribute.location);
            }
        }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            _programID = GL.CreateProgram();

            foreach (IGLShaderProgram shader in Shaders)
                GL.AttachShader(_programID, shader.ID);

            //Attribute locations are "bound" in Vertex Buffers, not here
            //Fragment Data locations are "bound" in the shader source

            GL.LinkProgram(_programID);

            GL.GetProgram(_programID, GetProgramParameterName.LinkStatus, out int result);
            if (0 == result)
            {
                Debug.Fail(GL.GetProgramInfoLog(_programID));
                GL.DeleteProgram(_programID);
                _programID = 0;
                return;
            }

            //Fallback, if explicit uniform bindings are not supported
            ReadOnlySpan<GLPipelineResourceLayout> resourceLayouts = ResourceLayouts;
            if (resourceLayouts.Length > 0 && !_device.GLFeatures.IsExplicitUniformLocationSupported)
            {
                for (int j = 0; j < resourceLayouts.Length; j++)
                {
                    foreach (uint uniformBinding in resourceLayouts[j].UniformBindings)
                    {
                        uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(uniformBinding);
                        BindUniformBlockIndex(_programID, uniqueBinding);
                    }
                    foreach (uint uniformBinding in resourceLayouts[j].DynamicUniformBindings)
                    {
                        uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(uniformBinding);
                        BindUniformBlockIndex(_programID, uniqueBinding);
                    }


                    //Sampler Locations
                    if (resourceLayouts[j].CombinedTextureSamplerBindings.Any() || resourceLayouts[j].InputAttachmentBindings.Any())
                    {
                        _samplerLocations = new SortedDictionary<uint, int>();
                        foreach (uint samplerBinding in resourceLayouts[j].CombinedTextureSamplerBindings)
                        {
                            uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(samplerBinding);
                            _samplerLocations[uniqueBinding] = GL.GetUniformLocation(_programID, $"uniformsampler_{uniqueBinding}");
                        }
                        foreach (uint samplerBinding in resourceLayouts[j].InputAttachmentBindings)
                        {
                            uint uniqueBinding = resourceLayouts[j].GetUniqueBinding(samplerBinding);
                            _samplerLocations[uniqueBinding] = GL.GetUniformLocation(_programID, $"uniformsampler_{uniqueBinding}");
                        }
                    }
                }
            }

            if (_vertexInputs.HasValue)
                _vaoID = GLCreateVAO(_vertexInputs.Value, _device.GLFeatures.IsDirectStateAccessSupported);
            else _vaoID = GLCreateVAO(new VertexInputs(), _device.GLFeatures.IsDirectStateAccessSupported); //Still, it needs an empty VAO or glDrawArrays throw INVALID_OPERATION
        }
        public override void GLFree()
        {
            if (_vaoID > 0)
            {
                GL.DeleteVertexArray(_vaoID);
                _vaoID = 0;
            }

            if (_programID > 0)
            {
                GL.DeleteProgram(_programID);
                _programID = 0;
            }
        }

        public override void GLBind()
        {
            GL.UseProgram(_programID);

            GL.BindVertexArray(_vaoID);

            //Rasterization
            if (_rasterisation.HasValue)
            {
                GL.Disable(EnableCap.RasterizerDiscard);
                RasterizationOptions rasterization = _rasterisation.Value;

                //Culling and Rasterization
                if (rasterization.cullMode != CullMode.None)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(rasterization.cullMode.ToCullFaceModeFlags());
                    GL.FrontFace(rasterization.frontFace.ToFrontFaceDirection());
                    if (rasterization.frontPolygonMode == rasterization.backPolygonMode)
                        GL.PolygonMode(MaterialFace.FrontAndBack, rasterization.frontPolygonMode.ToPolygonMode());
                    else
                    {
                        GL.PolygonMode(MaterialFace.Front, rasterization.frontPolygonMode.ToPolygonMode());
                        GL.PolygonMode(MaterialFace.Back, rasterization.backPolygonMode.ToPolygonMode());
                    }
                }
                else GL.Disable(EnableCap.CullFace);

                //Color Mask
                ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = ColorAttachmentUsages;
                int colorBufferCount = _renderPassStep.ColorAttachmentIndices.Length;
                if (colorAttachmentUsages.Length > 0)
                {
                    for (int i = 0; i < colorAttachmentUsages.Length; i++)
                    {
                        ColorComponents colorMask = colorAttachmentUsages[i].colorWriteMask;
                        GL.ColorMask(i, colorMask.HasFlag(ColorComponents.Red), colorMask.HasFlag(ColorComponents.Green), colorMask.HasFlag(ColorComponents.Blue), colorMask.HasFlag(ColorComponents.Alpha));
                    }
                }
                else
                {
                    for (int i = 0; i < colorBufferCount; i++)
                        GL.ColorMask(i, true, true, true, true);
                }

                //Depth Test
                if (_depthUsage.testEnabled)
                {
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthFunc(_depthUsage.comparison.ToDepthFunction());
                }
                else GL.Disable(EnableCap.DepthTest);
                GL.DepthMask(_depthUsage.write);

                //Blend
                if (_isBlend)
                {
                    if (_device.GLFeatures.IsSeparateBufferBlendSupported)
                    {
                        for (int i = 0; i < colorAttachmentUsages.Length; i++)
                        {
                            if (colorAttachmentUsages[i].blend.HasValue)
                            {
                                BlendAttachment blend = colorAttachmentUsages[i].blend!.Value;
                                GL.Enable(IndexedEnableCap.Blend, i);
                                GL.BlendFuncSeparate(i, blend.sourceColorBlendFactor.ToBlendingFactorSrc(), blend.destinationColorBlendFactor.ToBlendingFactorDest(), blend.sourceAlphaBlendFactor.ToBlendingFactorSrc(), blend.destinationAlphaBlendFactor.ToBlendingFactorDest());
                                GL.BlendEquationSeparate(i, blend.colorBlendOperation.ToBlendEquationMode(), blend.alphaBlendOperation.ToBlendEquationMode());
                            }
                            else GL.Disable(IndexedEnableCap.Blend, i);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < colorAttachmentUsages.Length; i++)
                        {
                            if (colorAttachmentUsages[i].blend.HasValue)
                            {
                                BlendAttachment blend = colorAttachmentUsages[i].blend!.Value;
                                GL.Enable(IndexedEnableCap.Blend, i);
                                GL.BlendFunc(i, blend.sourceColorBlendFactor.ToBlendingFactorSrc(), blend.destinationColorBlendFactor.ToBlendingFactorDest());
                                GL.BlendEquation(i, blend.colorBlendOperation.ToBlendEquationMode());
                            }
                            else GL.Disable(IndexedEnableCap.Blend, i);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < colorBufferCount; i++)
                        GL.Disable(IndexedEnableCap.Blend, i);
                }
            }
            else GL.Enable(EnableCap.RasterizerDiscard);
        }

        #endregion

    }
}
