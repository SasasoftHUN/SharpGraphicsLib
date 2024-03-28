using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal abstract class GLCoreTexture : GLTexture
    {

        #region Fields

        protected readonly TextureTarget _target;

        protected readonly PixelInternalFormat _pixelInternalFormat;
        protected readonly PixelFormat _pixelFormat;
        protected readonly PixelType _pixelType;

        #endregion

        #region Properties

        #endregion

        #region Constructors

        //Create Texture by allocating memory
        protected GLCoreTexture(GLGraphicsDevice device, TextureTarget target, Vector3UInt extent, TextureType type, DataFormat dataFormat, uint layers, uint mipLevels) :
            base(device, extent, layers, mipLevels, dataFormat, type)
        {
            _target = target;

            dataFormat.ToPixelFormat(out _pixelInternalFormat, out _pixelFormat, out _pixelType);
        }
        //Create just View
        protected GLCoreTexture(GLGraphicsDevice device, TextureTarget target, GLCoreTexture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, referenceTexture, swizzle, mipmapRange, layerRange, dataFormat)
        {
            _target = target;

            dataFormat.ToPixelFormat(out _pixelInternalFormat, out _pixelFormat, out _pixelType);
        }

        //Not needed, base calls it!
        //~GLCoreTexture() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected void GLInitializeSwizzle(bool dsa)
        {
            if (!_swizzle.IsOriginal)
            {
                unsafe
                {
                    Span<int> swizzle = stackalloc int[4]
                    {
                        _swizzle.red.ToSwizzleInt(TextureSwizzleType.Red),
                        _swizzle.green.ToSwizzleInt(TextureSwizzleType.Green),
                        _swizzle.blue.ToSwizzleInt(TextureSwizzleType.Blue),
                        _swizzle.alpha.ToSwizzleInt(TextureSwizzleType.Alpha)
                    };
                    fixed (int* p = &swizzle[0])
                    {
                        if (dsa)
                            GL.TextureParameter(_id, TextureParameterName.TextureSwizzleRgba, p);
                        else GL.TexParameter(_target, TextureParameterName.TextureSwizzleRgba, p);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public override void GLFree()
        {
            if (_id != 0u)
            {
                GL.DeleteTexture(_id);
                _id = 0;
            }
        }

        public override void GLBind(int binding, int textureUnit)
        {
            //Not using DSA. MultiBind is probably supported when DSA is available so no reason to do branching here
            GL.ActiveTexture((TextureUnit.Texture0 + textureUnit));
            GL.BindTexture(_target, _id);
            GL.Uniform1(binding, textureUnit);
        }
        public override void GLUnBind(int textureUnit)
        {
            //Not using DSA. MultiBind is probably supported when DSA is available so no reason to do branching here
            GL.ActiveTexture((TextureUnit.Texture0 + textureUnit));
            GL.BindTexture(_target, 0);
        }

        public override void GLGenerateMipMaps()
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.GenerateTextureMipmap(_id);
            else
            {
                GL.BindTexture(_target, _id);
                GL.GenerateMipmap((GenerateMipmapTarget)_target);
                GL.BindTexture(_target, 0);
            }
        }

        #endregion

    }
}
