using System;

namespace SharpGraphics.Factory
{

    public enum GraphicsAPI
    {
        OpenGLCore = 0,
        //OpenGLES10 = 1, OpenGLES11 = 2, OpenGLES20 = 3,
        OpenGLES30 = 4, //OpenGLES31 = 5, OpenGLES32 = 6,
        //WebGL10 = 16, WebGL20 = 17,

        Vulkan = 32,
        //DirectX11 = 64, DirectX12 = 65,
        //Metal = 96,
    }

    //TODO: Use Attributes and Compiler magic to determine GraphicsAPI
    //TODO: Use Attributes and Compiler magic to determine OpetatingSystem
    public static class GraphicsFactory
    {

        public static DebugLevel DebugLevel { get; set; } = DebugLevel.None;

        public static GraphicsManagement CreateForGraphics(GraphicsAPI graphicsAPI, OperatingSystem operatingSystem)
        {
            switch (graphicsAPI)
            {
                case GraphicsAPI.OpenGLCore:
                    return new OpenGL.OpenGLCore.GLCoreGraphicsManagement(operatingSystem, DebugLevel);
                case GraphicsAPI.OpenGLES30:
                    return new OpenGL.OpenGLES30.GLES30GraphicsManagement(operatingSystem, DebugLevel);

                case GraphicsAPI.Vulkan:
                    return Vulkan.VulkanGraphicsManagement.Create(operatingSystem, DebugLevel);

                default: throw new NotSupportedException(graphicsAPI + " not supported!");
            }
        }

    }
}
