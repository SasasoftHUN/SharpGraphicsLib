using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.OpenGLES30.CommandBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.ES30;
using SharpGraphics.Allocator;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{

    internal readonly struct GLES30VertexBufferBinding
    {
        public readonly uint binding;
        public readonly uint vertexBufferID;
        public readonly int offset;

        public GLES30VertexBufferBinding(uint binding, uint vertexBufferID, int offset)
        {
            this.binding = binding;
            this.vertexBufferID = vertexBufferID;
            this.offset = offset;
        }

        public override string ToString() => $"ID: {vertexBufferID}, Binding: {binding}, Offset: {offset}";
    }

    internal sealed class GLES30BindVertexBufferCommand : IGLCommand
    {

        private readonly IGLES30VertexDataBuffer _vertexBuffer;
        private readonly VertexInputs _vertexInputs;
        private readonly uint _binding;
        private readonly int _offset;

        internal GLES30BindVertexBufferCommand(IGLES30VertexDataBuffer vertexBuffer, in VertexInputs vertexInputs, uint binding, int offset)
        {
            _vertexBuffer = vertexBuffer;
            _vertexInputs = vertexInputs;
            _binding = binding;
            _offset = offset;
        }

        public void Execute() => _vertexBuffer.GLBindVertexBuffer(_vertexInputs, _binding, _offset);

        public override string ToString() => $"Bind Vertex Buffer (ID: {_vertexBuffer.ID}, Binding: {_binding}, Offset: {_offset})";

    }

    internal sealed class GLES30BindVertexBuffersCommand : IGLCommand
    {

        private readonly IGLES30VertexDataBuffer _vertexBuffer;
        private readonly VertexInputs _vertexInputs;
        private readonly IntPtr _vertexBuffersPtr;
        private readonly int _vertexBuffersCount;

        internal GLES30BindVertexBuffersCommand(IGLES30VertexDataBuffer vertexBuffer, in VertexInputs vertexInputs, in ReadOnlySpan<GLES30VertexBufferBinding> vertexBuffers, IMemoryAllocator allocator)
        {
            _vertexBuffer = vertexBuffer;
            _vertexInputs = vertexInputs;
            _vertexBuffersPtr = allocator.AllocateThenCopy(vertexBuffers);
            _vertexBuffersCount = vertexBuffers.Length;
        }

        public unsafe void Execute() => _vertexBuffer.GLBindVertexBuffers(_vertexInputs, new Span<GLES30VertexBufferBinding>(_vertexBuffersPtr.ToPointer(), _vertexBuffersCount));

        public unsafe override string ToString() => $"Bind Vertex Buffers (IDs: {
            new Span<GLES30VertexBufferBinding>(_vertexBuffersPtr.ToPointer(), _vertexBuffersCount).ToArray().Select(vb => $"({vb})").Aggregate((vb1, vb2) => $"{vb1}, {vb2}")})";

    }


    //Used to bind "empty VAO" from GLES30GraphicsDevice for Pipelines with no VertexInputs
    internal sealed class GLES30BindVertexArrayCommand : IGLCommand
    {

        private readonly int _vaoID;

        internal GLES30BindVertexArrayCommand(int vaoID) =>_vaoID = vaoID;

        public void Execute() => GL.BindVertexArray(_vaoID);

        public override string ToString() => $"Bind Vertex Array (ID: {_vaoID})";

    }

}
