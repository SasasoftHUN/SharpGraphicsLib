using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using SharpGraphics.Allocator;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreBindVertexBufferCommand : IGLCommand
    {

        private readonly uint _binding;
        private readonly uint _vertexBufferID;
        private readonly IntPtr _offset;
        private readonly int _stride;

        internal GLCoreBindVertexBufferCommand(uint binding, uint vertexBufferID, IntPtr offset, int stride)
        {
            _binding = binding;
            _vertexBufferID = vertexBufferID;
            _offset = offset;
            _stride = stride;
        }

        public void Execute() => GL.BindVertexBuffer(_binding, _vertexBufferID, _offset, _stride);

        public override string ToString() => $"Bind Vertex Buffer (VBO ID: {_vertexBufferID}, Binding: {_binding}, Offset: {(int)_offset}, Stride: {_stride})";

    }

    internal sealed class GLCoreBindVertexBuffersCommand : IGLCommand
    {

        internal GLCoreBindVertexBuffersCommand()
        {
        }

        public void Execute() { }

        public override string ToString() => "";//$"Bind Vertex Buffer (VBO ID: {_vertexBuffer.ID}, Binding: {_binding}, Offset: {(int)_offset}, Stride: {_stride})";

    }

    internal sealed class GLCoreUnBindVertexBufferCommand : IGLCommand
    {

        private readonly uint _binding;

        internal GLCoreUnBindVertexBufferCommand(uint binding) => _binding = binding;

        public void Execute() => GL.BindVertexBuffer(_binding, 0u, IntPtr.Zero, 0);

        public override string ToString() => $"UnBind Vertex Buffer (Binding: {_binding})";

    }
    internal sealed class GLCoreUnBindVertexBuffersCommand : IGLCommand
    {

        private readonly IntPtr _bindingsPtr;
        private readonly int _bindingsCount;

        internal GLCoreUnBindVertexBuffersCommand(in ReadOnlySpan<int> bindings, IMemoryAllocator allocator)
        {
            _bindingsPtr = allocator.AllocateThenCopy(bindings);
            _bindingsCount = bindings.Length;
        }

        public unsafe void Execute()
        {
            foreach (int binding in new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount))
                GL.BindVertexBuffer(binding, 0, IntPtr.Zero, 0);
        }

        public unsafe override string ToString() => $"UnBind Vertex Buffers (Bindings: {new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount).ToArray().Select(b => b.ToString()).Aggregate((b1, b2) => $"{b1}, {b2}")})";

    }

}
