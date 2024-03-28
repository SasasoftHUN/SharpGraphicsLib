using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreTextureSampler : GLTextureSampler
    {

        #region Constructors

        internal GLCoreTextureSampler(GLCoreGraphicsDevice device, in TextureSamplerConstruction construction) : base(device, construction) { }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            _id = GL.GenSampler();

            GL.SamplerParameter(_id, SamplerParameterName.TextureMinFilter, (int)_construction.minifyingFilter.ToTextureMinFilter(_construction.mipmapMode.mode));
            GL.SamplerParameter(_id, SamplerParameterName.TextureMagFilter, (int)_construction.magnifyingFilter.ToTextureMagFilter());

            if (_construction.anisotropy > 1f && _device.Limits.MaxAnisotropy > 1f && _device.GLFeatures.IsAnisotropicFilterSupported)
                GL.SamplerParameter(_id, SamplerParameterName.TextureMaxAnisotropyExt, Math.Min(_construction.anisotropy, _device.Limits.MaxAnisotropy));

            GL.SamplerParameter(_id, SamplerParameterName.TextureMinLod, _construction.mipmapMode.lodRange.start);
            GL.SamplerParameter(_id, SamplerParameterName.TextureMaxLod, _construction.mipmapMode.lodRange.count == float.MaxValue ? 1000f : _construction.mipmapMode.lodRange.End);
            GL.SamplerParameter(_id, SamplerParameterName.TextureLodBias, _construction.mipmapMode.lodBias);

            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapS, (int)_construction.wrap.u.ToTextureWrapMode());
            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapT, (int)_construction.wrap.v.ToTextureWrapMode());
            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapR, (int)_construction.wrap.w.ToTextureWrapMode());
        }
        public override void GLFree()
        {
            if (_id != 0)
            {
                GL.DeleteSampler(_id);
                _id = 0;
            }
        }

        public override void GLBind(int textureUnit) => GL.BindSampler(textureUnit, _id);
        public override void GLUnBind(int textureUnit) => GL.BindSampler(textureUnit, 0);

        #endregion

    }
}
