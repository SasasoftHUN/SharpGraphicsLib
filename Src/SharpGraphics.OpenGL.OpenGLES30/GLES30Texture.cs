﻿using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;
using System.Diagnostics;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal abstract class GLES30Texture : GLTexture
    {

        #region Fields

        protected readonly TextureTarget _target;

        protected readonly int _pixelInternalFormat;
        protected readonly PixelFormat _pixelFormat;
        protected readonly PixelType _pixelType;

        #endregion

        #region Constructors

        //Create Texture by allocating memory
        protected GLES30Texture(GLGraphicsDevice device, TextureTarget target, Vector3UInt extent, TextureType type, DataFormat dataFormat, uint layers, uint mipLevels) :
            base(device, extent, layers, mipLevels, dataFormat, type)
        {
            _target = target;

            dataFormat.ToPixelFormat(out _pixelInternalFormat, out _pixelFormat, out _pixelType);
        }
        //Create just View
        protected GLES30Texture(GLGraphicsDevice device, TextureTarget target, GLES30Texture referenceTexture, DataFormat dataFormat, in TextureSwizzle swizzle, in TextureRange mipmapRange, in TextureRange layerRange) :
            base(device, referenceTexture, swizzle, mipmapRange, layerRange, dataFormat)
        {
            throw new NotSupportedException("OpenGL ES 3.0 does not support Texture Views!");
        }

        //Not needed, base calls it!
        //~GLES30Texture() => Dispose(disposing: false);

        #endregion

        #region Protected Methods

        protected void GLInitializeSwizzle()
        {
            if (!_swizzle.IsOriginal)
            {
                unsafe
                {
#if ANDROID
                    if (_swizzle.red != TextureSwizzleType.Original || _swizzle.red != TextureSwizzleType.Red)
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleR, _swizzle.red.ToSwizzleInt(TextureSwizzleType.Red));
                    if (_swizzle.green != TextureSwizzleType.Original || _swizzle.green != TextureSwizzleType.Green)
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleG, _swizzle.green.ToSwizzleInt(TextureSwizzleType.Green));
                    if (_swizzle.blue != TextureSwizzleType.Original || _swizzle.blue != TextureSwizzleType.Blue)
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleB, _swizzle.blue.ToSwizzleInt(TextureSwizzleType.Blue));
                    if (_swizzle.alpha != TextureSwizzleType.Original || _swizzle.alpha != TextureSwizzleType.Alpha)
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleA, _swizzle.alpha.ToSwizzleInt(TextureSwizzleType.Alpha));
#else
                    Span<int> swizzle = stackalloc int[4]
                    {
                        _swizzle.red.ToSwizzleInt(TextureSwizzleType.Red),
                        _swizzle.green.ToSwizzleInt(TextureSwizzleType.Green),
                        _swizzle.blue.ToSwizzleInt(TextureSwizzleType.Blue),
                        _swizzle.alpha.ToSwizzleInt(TextureSwizzleType.Alpha)
                    };
                    fixed (int* p = &swizzle[0])
                        GL.TexParameter(_target, TextureParameterName.TextureSwizzleRgba, p);
#endif
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
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
            GL.BindTexture(_target, _id);
            GL.Uniform1(binding, textureUnit);
        }
        public override void GLUnBind(int textureUnit)
        {
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
            GL.BindTexture(_target, 0);
        }

        public override void GLGenerateMipMaps()
        {
            GL.BindTexture(_target, _id);
            GL.GenerateMipmap(_target);
            GL.BindTexture(_target, 0);
        }

        #endregion

    }
}
