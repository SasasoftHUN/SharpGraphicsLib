using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace SharpGraphics.OpenGL.OpenGLCore
{

    internal sealed class GLCoreGraphicsDeviceLimits : GraphicsDeviceLimits
    {
        internal GLCoreGraphicsDeviceLimits(GLGraphicsDeviceFeatures features)
        {
            UniformBufferAlignment = (uint)GL.GetInteger(GetPName.UniformBufferOffsetAlignment);
            MaxAnisotropy = features.IsAnisotropicFilterSupported ? GL.GetFloat((GetPName)0x84FF /*GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT*/) : 1f;
        }
    }

    internal sealed class GLCoreGraphicsDeviceFeatures : GLGraphicsDeviceFeatures
    {

        public override IEnumerable<ShaderAPIVersion> ShaderAPIVersions { get; }

        public GLCoreGraphicsDeviceFeatures()
        {
            //Get OpenGL Version
            GLVersionMajor = GL.GetInteger(GetPName.MajorVersion);
            GLVersionMinor = GL.GetInteger(GetPName.MinorVersion);
            GLVersionNumber = GLVersionMajor * 100 + GLVersionMinor * 10;

            if (GLVersionMajor < 4) //OpenGL Core 4.2 is the minimum supported by this implementation at the moment (4.0, 4.1 may be supported with extensions)
                throw new GLGraphicsDeviceCreationException($"Created OpenGL Version {GLVersionMajor}.{GLVersionMinor} is not supported!");

            List<ShaderAPIVersion> shaderAPIVersions = new List<ShaderAPIVersion>(6);
            for (int i = 6; i >= 0; i--)
                if (GLVersionMinor >= i)
                    shaderAPIVersions.Add(new ShaderAPIVersion(ShaderSourceType.Text, $"GLSL4{i}0"));
            ShaderAPIVersions = shaderAPIVersions.ToArray();

            //Check Extensions
            GL.GetInteger(GetPName.NumExtensions, out int extensionsCount);
            string[] extensions = new string[extensionsCount];
            for (int i = 0; i < extensionsCount; i++)
                extensions[i] = GL.GetString(StringNameIndexed.Extensions, i);
            CheckExtensions(extensions);

            //Check Required Feature support
            IsSeparateVertexArrayFormatSupported = CheckFeatureSupport(IsBufferStorageSupported, 430, "Separate Vertex Array Format", true);

            IsBufferStorageSupported = CheckFeatureSupport(IsBufferStorageSupported, 440, "Buffer Storage", false);
            IsBufferPersistentMappingSupported = IsBufferStorageSupported;
            IsBufferCoherentMappingSupported = IsBufferStorageSupported;
            IsTextureStorageSupported = CheckFeatureSupport(IsTextureStorageSupported, 420, "Texture Storage", false);
            IsTextureViewSupported = CheckFeatureSupport(IsTextureViewSupported, 430, "Texture View", false) && IsTextureStorageSupported;
            IsAnisotropicFilterSupported = CheckFeatureSupport(IsAnisotropicFilterSupported, 460, "Anisotropic Filter", false);
            IsDirectStateAccessSupported = CheckFeatureSupport(IsDirectStateAccessSupported, 450, "Direct State Access", false) && IsBufferStorageSupported && IsTextureStorageSupported;
            IsExplicitUniformLocationSupported = CheckFeatureSupport(IsExplicitUniformLocationSupported, 430, "Explicit Uniform Location", false);
            IsMultiBindSupported = CheckFeatureSupport(IsMultiBindSupported, 440, "MutliBind", false);
            IsSeparateBufferBlendSupported = CheckFeatureSupport(IsSeparateBufferBlendSupported, 400, "Separate Buffer Blend", false);
        }
    }

    internal sealed class GLCoreGraphicsDeviceInfo : GLGraphicsDeviceInfo
    {

        private readonly Version _apiVersion;
        private readonly Version _driverVersion;
        private readonly string _name;

        public override Version APIVersion => _apiVersion;
        public override Version DriverVersion => _driverVersion;
        public override string Name => _name;


        internal GLCoreGraphicsDeviceInfo()
        {
            _apiVersion = new Version(0, 0, 0, 0);
            _driverVersion = new Version(0, 0, 0, 0);
            _name = "OpenGL Graphics Device";
        }
        internal GLCoreGraphicsDeviceInfo(GLCoreGraphicsDeviceFeatures features, GLCoreGraphicsDeviceLimits limits) : base(new GLGraphicsCommandProcessorGroupInfo(GraphicsCommandProcessorType.Unknown, true), features, limits)
        {
            //Get OpenGL Version
            int major = GL.GetInteger(GetPName.MajorVersion);
            int minor = GL.GetInteger(GetPName.MinorVersion);
            Debug.WriteLine($"OpenGL Core Context created with Version {major}.{minor}!");
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
