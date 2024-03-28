using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.OpenGL.Commands;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.Allocator;
using System.Linq;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreMultiBindCombinedTextureSamplersCommand : IGLCommand
    {

        //TODO: Aren't all Counts are the same?
        private readonly IntPtr _bindingsPtr;
        private readonly int _bindingsCount;
        private readonly int _textureUnitStartIndex;
        private readonly IntPtr _textureIDsPtr;
        private readonly int _textureIDCount;
        private readonly IntPtr _samplerIDsPtr;
        private readonly int _samplerIDCount;

        internal GLCoreMultiBindCombinedTextureSamplersCommand(ReadOnlySpan<int> bindings, int textureUnitStartIndex, ReadOnlySpan<int> textureIDs, ReadOnlySpan<int> samplerIDs, IMemoryAllocator allocator)
        {
            _bindingsPtr = allocator.AllocateThenCopy(bindings);
            _bindingsCount = bindings.Length;
            _textureUnitStartIndex = textureUnitStartIndex;
            _textureIDsPtr = allocator.AllocateThenCopy(textureIDs);
            _textureIDCount = textureIDs.Length;
            _samplerIDsPtr = allocator.AllocateThenCopy(samplerIDs);
            _samplerIDCount = samplerIDs.Length;
        }

        public unsafe void Execute()
        {
            Span<int> textureIDs = new Span<int>(_textureIDsPtr.ToPointer(), _textureIDCount);
            GL.BindTextures(_textureUnitStartIndex, _textureIDCount, ref textureIDs[0]);

            Span<int> samplerIDs = new Span<int>(_samplerIDsPtr.ToPointer(), _samplerIDCount);
            GL.BindSamplers(_textureUnitStartIndex, _samplerIDCount, ref samplerIDs[0]);

            Span<int> bindings = new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount);
            for (int i = 0; i < _bindingsCount; i++)
                GL.Uniform1(bindings[i], _textureUnitStartIndex + i);
        }

        public unsafe override string ToString() => $"Multi-Bind Combined Textures (Bindings: {
            new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount).ToArray().Select(b => b.ToString()).Aggregate((b1, b2) => $"{b1}, {b2}")
            }, Texture Units: {_textureUnitStartIndex} - {_textureUnitStartIndex + _samplerIDCount}, Texture IDs: {
            new Span<int>(_textureIDsPtr.ToPointer(), _textureIDCount).ToArray().Select(tu => tu.ToString()).Aggregate((tu1, tu2) => $"{tu1}, {tu2}")
            }, Sampler IDs: {
            new Span<int>(_samplerIDsPtr.ToPointer(), _samplerIDCount).ToArray().Select(s => s.ToString()).Aggregate((s1, s2) => $"{s1}, {s2}")
            })";

    }
    internal sealed class GLCoreMultiUnBindCombinedTextureSamplersCommand : IGLCommand
    {

        private readonly int _textureUnitStartIndex;
        private readonly int _textureCount;

        internal GLCoreMultiUnBindCombinedTextureSamplersCommand(int textureUnitStartIndex, int textureCount)
        {
            _textureUnitStartIndex = textureUnitStartIndex;
            _textureCount = textureCount;
        }

        public void Execute()
        {
            Span<int> ids = stackalloc int[_textureCount];
            /*for (int i = 0; i < _textureCount; i++)
                ids[i] = 0;*/

            GL.BindTextures(_textureUnitStartIndex, _textureCount, ref ids[0]);
            GL.BindSamplers(_textureUnitStartIndex, _textureCount, ref ids[0]);
        }
        public unsafe override string ToString() => $"Multi-UnBind Combined Textures (Texture Units: {_textureUnitStartIndex} - {_textureUnitStartIndex + _textureCount})";

    }

    internal sealed class GLCoreMultiBindTexturesCommand : IGLCommand
    {

        //TODO: Aren't all Counts are the same?
        private readonly IntPtr _bindingsPtr;
        private readonly int _bindingsCount;
        private readonly int _textureUnitStartIndex;
        private readonly IntPtr _textureIDsPtr;
        private readonly int _textureIDCount;

        internal GLCoreMultiBindTexturesCommand(ReadOnlySpan<int> bindings, int textureUnitStartIndex, ReadOnlySpan<int> textureIDs, IMemoryAllocator allocator)
        {
            _bindingsPtr = allocator.AllocateThenCopy(bindings);
            _bindingsCount = bindings.Length;
            _textureUnitStartIndex = textureUnitStartIndex;
            _textureIDsPtr = allocator.AllocateThenCopy(textureIDs);
            _textureIDCount = textureIDs.Length;
        }

        public unsafe void Execute()
        {
            Span<int> textureIDs = new Span<int>(_textureIDsPtr.ToPointer(), _textureIDCount);
            GL.BindTextures(_textureUnitStartIndex, _textureIDCount, ref textureIDs[0]);

            Span<int> bindings = new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount);
            for (int i = 0; i < _bindingsCount; i++)
                GL.Uniform1(bindings[i], _textureUnitStartIndex + i);
        }

        public unsafe override string ToString() => $"Multi-Bind Textures (Bindings: {
            new Span<int>(_bindingsPtr.ToPointer(), _bindingsCount).ToArray().Select(b => b.ToString()).Aggregate((b1, b2) => $"{b1}, {b2}")
            }, Texture Units: {
            new Span<int>(_textureIDsPtr.ToPointer(), _textureIDCount).ToArray().Select(tu => tu.ToString()).Aggregate((tu1, tu2) => $"{tu1}, {tu2}")
            })";

    }
    internal sealed class GLCoreMultiUnBindTexturesCommand : IGLCommand
    {

        private readonly int _textureUnitStartIndex;
        private readonly int _textureCount;

        internal GLCoreMultiUnBindTexturesCommand(int textureUnitStartIndex, int textureCount)
        {
            _textureUnitStartIndex = textureUnitStartIndex;
            _textureCount = textureCount;
        }

        public void Execute()
        {
            Span<int> textureIDs = stackalloc int[_textureCount];
            /*for (int i = 0; i < _textureCount; i++)
                textureIDs[i] = 0;*/

            GL.BindTextures(_textureUnitStartIndex, _textureCount, ref textureIDs[0]);
        }
        public unsafe override string ToString() => $"Multi-UnBind Textures (Texture Units: {_textureUnitStartIndex} - {_textureUnitStartIndex + _textureCount})";

    }
}
