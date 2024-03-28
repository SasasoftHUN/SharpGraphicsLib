using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using OpenTK.Graphics.ES30;
using SharpGraphics.Utils;
using System.Runtime.InteropServices;
#if ANDROID
using OpenTK.Platform.Android;
using Android.Views;
using Javax.Microedition.Khronos.Egl;
#endif

namespace SharpGraphics.OpenGL.OpenGLES30
{

    internal sealed class GLES30GraphicsDeviceLimits : GraphicsDeviceLimits
    {
        internal GLES30GraphicsDeviceLimits(GLGraphicsDeviceFeatures features)
        {
            GL.GetInteger(GetPName.UniformBufferOffsetAlignment, out int uniformBufferOffsetAlignment);
            UniformBufferAlignment = (uint)uniformBufferOffsetAlignment;

            if (features.IsAnisotropicFilterSupported)
            {
                GL.GetFloat((GetPName)0x84FF /*GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT*/, out float maxAnisotropy);
                MaxAnisotropy = maxAnisotropy;
            }
        }
    }

    internal sealed class GLES30GraphicsDeviceFeatures : GLGraphicsDeviceFeatures
    {

        public override IEnumerable<ShaderAPIVersion> ShaderAPIVersions { get; }

        internal GLES30GraphicsDeviceFeatures()
        {
            //Get OpenGL Version
            GL.GetInteger(GetPName.MajorVersion, out int major);
            GL.GetInteger(GetPName.MinorVersion, out int minor);
            GLVersionMajor = major; //Forcing 3.0, because the implementation is made for this version
            GLVersionMinor = minor;
            GLVersionNumber = major * 100 + minor * 10;

            if (GLVersionMajor < 3) //OpenGL ES 3.0 is the minimum supported by this implementation
                throw new GLGraphicsDeviceCreationException($"Created OpenGL Version {major}.{minor} is not supported!");

            ShaderAPIVersions = new ShaderAPIVersion[] { new ShaderAPIVersion(ShaderSourceType.Text, "GLSLES300") };

            //Check Extensions
            GL.GetInteger(GetPName.NumExtensions, out int extensionsCount);
            string[] extensions = new string[extensionsCount];
            for (int i = 0; i < extensionsCount; i++)
            {
#if ANDROID
                    try
                    {
                        unsafe
                        {
                            extensions[i] = UnsafeExtension.ParseByteString((byte*)GLES30Utils.GetStringi(StringNameIndexed.Extensions, i)) ?? "";
                        }
                    }
                    catch { }
#else
                extensions[i] = GL.GetString(StringNameIndexed.Extensions, i);
#endif
            }
            CheckExtensions(extensions);

            //Check Required Feature support
            IsSeparateVertexArrayFormatSupported = false; //Not supported in ES 3.0

            IsBufferStorageSupported = false;
            IsBufferPersistentMappingSupported = false;
            IsBufferCoherentMappingSupported = false;
            IsTextureStorageSupported = CheckFeatureSupport(IsTextureStorageSupported, 300, "Texture Storage", false);
            IsTextureViewSupported = false; //Missing from API binding....
            IsAnisotropicFilterSupported = CheckFeatureSupport(IsAnisotropicFilterSupported, int.MaxValue, "Anisotropic Filter", false);
            IsDirectStateAccessSupported = CheckFeatureSupport(IsDirectStateAccessSupported, int.MaxValue, "Direct State Access", false);
            IsExplicitUniformLocationSupported = false; //Fallback is forced in ES 300 implementation
            IsMultiBindSupported = false;
            IsSeparateBufferBlendSupported = true; //TODO: Is it really available by default on GLES2.0+?
        }
    }

    internal sealed class GLES30GraphicsDeviceInfo : GLGraphicsDeviceInfo
    {

        private readonly Version _apiVersion;
        private readonly Version _driverVersion;
        private readonly string _name;

        public override Version APIVersion => _apiVersion;
        public override Version DriverVersion => _driverVersion;
        public override string Name => _name;

        internal GLES30GraphicsDeviceInfo()
        {
            _apiVersion = new Version(0, 0, 0, 0);
            _driverVersion = new Version(0, 0, 0, 0);
            _name = "OpenGL Graphics Device";
        }
        internal GLES30GraphicsDeviceInfo(GLES30GraphicsDeviceFeatures features, GLES30GraphicsDeviceLimits limits) : base(new GLGraphicsCommandProcessorGroupInfo(GraphicsCommandProcessorType.Unknown, true), features, limits)
        {
            //Get OpenGL Version
            //GL.GetInteger(GetPName.MajorVersion, out int major);
            //GL.GetInteger(GetPName.MinorVersion, out int minor);
            int major = 3; //Forcing 3.0, because the implementation is made for this version
            int minor = 0;
            Debug.WriteLine($"OpenGL ES 3.0 Context created with Version {major}.{minor}!");
            _apiVersion = new Version(major, minor, 0, 0);
            GLVersionNumber = major * 100 + minor * 10;

            VendorID = 0u;
            DeviceID = 0u;
            _driverVersion = new Version(0, 0, 0, 0);
            Type = GraphicsDeviceType.Unknown; //TODO: Get Support for DeviceType (here and for CommandProcessorInfo as well)
            _name = $"{GL.GetString(StringName.Vendor)} {GL.GetString(StringName.Renderer)}";
        }

    }

}
