using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30TextureSampler : GLTextureSampler
    {

        #region Constructors

        internal GLES30TextureSampler(GLES30GraphicsDevice device, in TextureSamplerConstruction construction) : base(device, construction) { }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            GL.GenSamplers(1, out _id);

            GL.SamplerParameter(_id, SamplerParameterName.TextureMinFilter, (int)_construction.minifyingFilter.ToTextureMinFilter(_construction.mipmapMode.mode));
            GL.SamplerParameter(_id, SamplerParameterName.TextureMagFilter, (int)_construction.magnifyingFilter.ToTextureMagFilter());

            if (_construction.anisotropy > 1f && _device.Limits.MaxAnisotropy > 1f && _device.GLFeatures.IsAnisotropicFilterSupported)
                GL.SamplerParameter(_id, (SamplerParameterName)34046/*SamplerParameterName.TextureMaxAnisotropyExt*/, Math.Min(_construction.anisotropy, _device.Limits.MaxAnisotropy));

            GL.SamplerParameter(_id, SamplerParameterName.TextureMinLod, _construction.mipmapMode.lodRange.start);
            GL.SamplerParameter(_id, SamplerParameterName.TextureMaxLod, _construction.mipmapMode.lodRange.End);
            //GL.SamplerParameter(_id, SamplerParameterName.TextureLodBias, _construction.mipmapMode.lodBias); //TODO: GLES30 sampler lod bias support?

            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapS, (int)_construction.wrap.u.ToTextureWrapMode());
            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapT, (int)_construction.wrap.v.ToTextureWrapMode());
            GL.SamplerParameter(_id, SamplerParameterName.TextureWrapR, (int)_construction.wrap.w.ToTextureWrapMode());
        }
        public override void GLFree()
        {
            if (_id != 0u)
            {
                GL.DeleteSamplers(1, ref _id);
                _id = 0;
            }
        }

        public override void GLBind(int textureUnit) => GL.BindSampler(textureUnit, _id);
        public override void GLUnBind(int textureUnit) => GL.BindSampler(textureUnit, 0);

        #endregion

    }
}
