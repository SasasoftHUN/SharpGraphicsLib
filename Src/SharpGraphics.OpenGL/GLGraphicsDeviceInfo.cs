using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.OpenGL
{

    public abstract class GLGraphicsDeviceFeatures : GraphicsDeviceFeatures
    {

        public int GLVersionMajor { get; protected set; }
        public int GLVersionMinor { get; protected set; }
        public int GLVersionNumber { get; protected set; }

        public bool IsSeparateVertexArrayFormatSupported { get; protected set; }
        public bool IsBufferStorageSupported { get; protected set; }
        public bool IsTextureStorageSupported { get; protected set; }
        public bool IsAnisotropicFilterSupported { get; protected set; }
        public bool IsDirectStateAccessSupported { get; protected set; }
        public bool IsExplicitUniformLocationSupported { get; protected set; }

        public bool IsMultiBindSupported { get; protected set; }
        public bool IsSeparateBufferBlendSupported { get; protected set; }

        protected GLGraphicsDeviceFeatures()
        {
            IsBufferCachedMappingSupported = false;
        }

        protected virtual void CheckExtensions(IEnumerable<string> extensions)
        {
            foreach (string extension in extensions)
                switch (extension)
                {
                    case "ARB_vertex_attrib_binding": case "GL_ARB_vertex_attrib_binding": IsSeparateVertexArrayFormatSupported = true; break;
                    case "ARB_buffer_storage": case "GL_ARB_buffer_storage": IsBufferStorageSupported = true; break;
                    case "ARB_texture_storage": case "GL_ARB_texture_storage": IsTextureStorageSupported = true; break;
                    case "ARB_texture_view": case "GL_ARB_texture_view": case "EXT_texture_view": case "GL_EXT_texture_view": IsTextureViewSupported = true; break;
                    case "ARB_texture_filter_anisotropic": case "GL_ARB_texture_filter_anisotropic": case "EXT_texture_filter_anisotropic": case "GL_EXT_texture_filter_anisotropic": IsAnisotropicFilterSupported = true; break;
                    case "EXT_direct_state_access": case "GL_EXT_direct_state_access": IsDirectStateAccessSupported = true; break;
                    case "ARB_explicit_uniform_location": case "GL_ARB_explicit_uniform_location": IsExplicitUniformLocationSupported = true; break;
                    case "ARB_multi_bind": case "GL_ARB_multi_bind": IsMultiBindSupported = true; break;
                    case "ARB_draw_buffers_blend": case "GL_ARB_draw_buffers_blend": IsSeparateBufferBlendSupported = true; break;
                }
        }

        protected bool CheckFeatureSupport(bool isSupportedByExtension, int supportedByCoreVersion, string featureName, bool isMandatory)
        {
            if (isSupportedByExtension || GLVersionNumber >= supportedByCoreVersion)
                return true;
            else
            {
                if (isMandatory)
                    throw new NotSupportedException(featureName);
                else Debug.WriteLine($"OpenGL feature {featureName} is not supported.");
                return false;
            }
        }

    }
    internal sealed class GLDummyGraphicsDeviceFeatures : GLGraphicsDeviceFeatures
    {
        public override IEnumerable<ShaderAPIVersion> ShaderAPIVersions => new List<ShaderAPIVersion>(); //not optimal, but technically this should be called rarely if ever...
    }
    internal sealed class GLDummyGraphicsDeviceLimits : GraphicsDeviceLimits { }


    public sealed class GLGraphicsCommandProcessorGroupInfo : GraphicsCommandProcessorGroupInfo
    {
        private readonly bool _supportsPresent = true;

        public GLGraphicsCommandProcessorGroupInfo() : base(GraphicsCommandProcessorType.Unknown, 1u) { }
        public GLGraphicsCommandProcessorGroupInfo(GraphicsCommandProcessorType type, bool supportsPresent) : base(type, 1u) => _supportsPresent = supportsPresent;

        public override bool IsViewSupported(IGraphicsView graphicsView) => _supportsPresent;
    }
    public abstract class GLGraphicsDeviceInfo : GraphicsDeviceInfo
    {

        private readonly GraphicsCommandProcessorGroupInfo[] _commandProcessorGroups;
        private readonly GLGraphicsDeviceFeatures _features;

        public int GLVersionNumber { get; protected set; }

        public override ReadOnlySpan<GraphicsCommandProcessorGroupInfo> CommandProcessorGroups => _commandProcessorGroups;
        public override GraphicsDeviceFeatures Features => _features;
        public override GraphicsDeviceLimits Limits { get; }

        public GLGraphicsDeviceFeatures GLFeatures => _features;

        protected GLGraphicsDeviceInfo()
        {
            AreDetailsAvailable = false;
            IsPresentSupported = true;

            VendorID = 0u;
            DeviceID = 0u;
            Type = GraphicsDeviceType.Unknown;

            _commandProcessorGroups = new GraphicsCommandProcessorGroupInfo[] { new GLGraphicsCommandProcessorGroupInfo() };
            Limits = new GLDummyGraphicsDeviceLimits();
            _features = new GLDummyGraphicsDeviceFeatures();
        }
        protected GLGraphicsDeviceInfo(GLGraphicsCommandProcessorGroupInfo commandProcessorGroupInfo, GLGraphicsDeviceFeatures features, GraphicsDeviceLimits limits)
        {
            AreDetailsAvailable = true;

            _commandProcessorGroups = new GraphicsCommandProcessorGroupInfo[] { commandProcessorGroupInfo };
            _features = features;
            Limits = limits;
        }

        public override uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView) => new uint[] { 0u };
        public override uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView, GraphicsCommandProcessorType requiredType) => new uint[] { 0u };

    }
}
