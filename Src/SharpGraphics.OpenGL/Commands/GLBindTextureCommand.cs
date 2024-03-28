using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLBindCombinedTextureSamplerCommand : IGLCommand
    {

        private readonly int _binding;
        private readonly int _textureUnit;
        private readonly GLTextureSampler _sampler;
        private readonly IGLTexture _texture;

        internal GLBindCombinedTextureSamplerCommand(int binding, int textureUnit, GLTextureSampler sampler, IGLTexture texture)
        {
            _binding = binding;
            _textureUnit = textureUnit;
            _sampler = sampler;
            _texture = texture;
        }

        public void Execute()
        {
            _texture.GLBind(_binding, _textureUnit);
            _sampler.GLBind(_textureUnit);
        }

        public override string ToString() => $"Bind Combined Texture Sampler (Binding: {_binding}, TextureUnit: {_textureUnit})";

    }
    internal sealed class GLUnBindCombinedTextureSamplerCommand : IGLCommand
    {

        private readonly int _textureUnit;
        private readonly GLTextureSampler _sampler;
        private readonly IGLTexture _texture;

        internal GLUnBindCombinedTextureSamplerCommand(int textureUnit, GLTextureSampler sampler, IGLTexture texture)
        {
            _textureUnit = textureUnit;
            _sampler = sampler;
            _texture = texture;
        }

        public void Execute()
        {
            _texture.GLUnBind(_textureUnit);
            _sampler.GLUnBind(_textureUnit);
        }

        public override string ToString() => $"UnBind Combined Texture Sampler (TextureUnit: {_textureUnit})";

    }

    internal sealed class GLBindTextureCommand : IGLCommand
    {

        private readonly int _binding;
        private readonly int _textureUnit;
        private readonly IGLTexture _texture;

        internal GLBindTextureCommand(int binding, int textureUnit, IGLTexture texture)
        {
            _binding = binding;
            _textureUnit = textureUnit;
            _texture = texture;
        }

        public void Execute() => _texture.GLBind(_binding, _textureUnit);

        public override string ToString() => $"Bind Texture (Binding: {_binding}, TextureUnit: {_textureUnit})";

    }
    internal sealed class GLUnBindTextureCommand : IGLCommand
    {

        private readonly int _textureUnit;
        private readonly IGLTexture _texture;

        internal GLUnBindTextureCommand(IGLTexture texture, int textureUnit)
        {
            _texture = texture;
            _textureUnit = textureUnit;
        }

        public void Execute() => _texture.GLUnBind(_textureUnit);

        public override string ToString() => $"UnBind Texture (TextureUnit: {_textureUnit})";

    }
}
