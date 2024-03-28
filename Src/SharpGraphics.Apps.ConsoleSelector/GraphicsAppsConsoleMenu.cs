using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using SharpGraphics;
using SharpGraphics.Factory;
using SharpGraphics.GraphicsViews;

namespace SharpGraphics.Apps.ConsoleSelector
{

    public delegate IGraphicsView CreateNewGraphicsView(uint width, uint height, bool isVSyncEnabled);

    public static class GraphicsAppsConsoleMenu
    {

        #region Private Methods

        private static bool TryReadGraphicsAPI(out GraphicsAPI api)
        {
            Console.WriteLine("Enter Graphics API (OpenGLCore, OpenGLES30, Vulkan):");
            string? input = Console.ReadLine()?.ToLower();
            switch (input)
            {
                case "openglcore": api = GraphicsAPI.OpenGLCore; Console.WriteLine("OpenGLCore API is set!"); return true;
                case "opengles30": api = GraphicsAPI.OpenGLES30; Console.WriteLine("OpenGLES30 API is set!"); return true;
                case "vulkan": api = GraphicsAPI.Vulkan; Console.WriteLine("Vulkan API is set!"); return true;
                default: api = GraphicsAPI.OpenGLCore; Console.WriteLine("Unknown API!"); return false;
            }
        }
        private static bool TryReadApp([NotNullWhen(returnValue: true)] out string? app)
        {
            Console.WriteLine("Enter Application");
            Console.WriteLine("\tHelloTriangle");
            Console.WriteLine("\tVertexAttributes");
            Console.WriteLine("\tQuadPlayground");
            Console.WriteLine("\tPushValues");
            Console.WriteLine("\tNormals");
            Console.WriteLine("\tNormalsThreads");
            Console.WriteLine("\tPostProcess");
            Console.WriteLine("\tDeferred");
            Console.WriteLine("\tModels");
            Console.WriteLine("\tSimpleRaytrace");
            string? input = Console.ReadLine()?.ToLower();
            switch (input)
            {
                case "hellotriangle":
                case "vertexattributes":
                case "quadplayground":
                case "pushvalues":
                case "normals":
                case "normalsthreads":
                case "postprocess":
                case "deferred":
                case "models":
                case "simpleraytrace":
                    app = input;
                    Console.WriteLine($"{input} app is set!");
                    return true;

                default: app = null; Console.WriteLine("Unknown app!"); return false;
            }
        }
        private static bool TryReadResolution(out uint width, out uint height)
        {
            Console.WriteLine("Enter Width:");
            if (uint.TryParse(Console.ReadLine(), out width) && width > 0u)
            {
                Console.WriteLine("Enter Height:");
                if (uint.TryParse(Console.ReadLine(), out height) && height > 0u)
                    return true;
            }
            height = 0u;
            return false;
        }
        private static void StartApp(GraphicsAPI api, OperatingSystem operatingSystem, IGraphicsView view, string app, bool isLoggingEnabled)
        {
            GraphicsManagement? graphicsManagement = null;

            try
            {
                graphicsManagement = GraphicsFactory.CreateForGraphics(api, operatingSystem);

                GraphicsApplicationBase? graphicsApplication = null;
                try
                {
                    graphicsApplication = app switch
                    {
                        "hellotriangle" => new GraphicsApplication<HelloTriangle.HelloTriangleApp>(graphicsManagement, view, HelloTriangle.HelloTriangleApp.Factory),
                        "vertexattributes" => new VertexAttributes.VertexAttributesApp(graphicsManagement, view),
                        "quadplayground" => new QuadPlayground.QuadPlaygroundApp(graphicsManagement, view),
                        "pushvalues" => new PushValues.PushValuesApp(graphicsManagement, view),
                        "normals" => new GraphicsApplication<Normals.NormalsApp>(graphicsManagement, view, Normals.NormalsApp.Factory),
                        "normalsthreads" => new GraphicsApplication<NormalsThreads.NormalsThreadsApp>(graphicsManagement, view, NormalsThreads.NormalsThreadsApp.Factory),
                        "postprocess" => new GraphicsApplication<PostProcess.PostProcessApp>(graphicsManagement, view, PostProcess.PostProcessApp.Factory),
                        "deferred" => new GraphicsApplication<Deferred.DeferredApp>(graphicsManagement, view, Deferred.DeferredApp.Factory),
                        "models" => new GraphicsApplication<Models.ModelsApp>(graphicsManagement, view, Models.ModelsApp.Factory),
                        "simpleraytrace" => new SimpleRaytrace.SimpleRaytraceApp(graphicsManagement, view),
                        _ => null,
                    };

                    if (graphicsApplication != null)
                    {
                        if (isLoggingEnabled)
                        {
                            graphicsApplication.Logger = new Loggers.FrameTimeLogger(new Loggers.FileStreamLogWriter())
                            {
                                LogName = $"{app}-{api}-{view.SwapChainConstructionRequest.mode}",
                            };
                        }

                        graphicsApplication.InitializeAndStart();
                        graphicsApplication.WaitForEnd();
                    }
                }
                finally
                {
                    if (graphicsApplication != null)
                        graphicsApplication.Dispose();
                    if (graphicsManagement != null)
                        graphicsManagement.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during App start: {e.Message}, {e.StackTrace}");
            }
        }

        private static OperatingSystem DetectOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OperatingSystem.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OperatingSystem.Linux;
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OperatingSystem.MacOS;
            else return OperatingSystem.Unknown;
        }

        #endregion

        #region Public Methods

        public static void Run(CreateNewGraphicsView viewFactory)
        {
            GraphicsAPI api = GraphicsAPI.Vulkan;
            string app = "hellotriangle";
            uint width = 320u;
            uint height = 240u;
            bool isVSyncEnabled = false;
            bool isLoggingEnabled = false;

            string? input;
            bool quit = false;
            while (!quit)
            {
                Console.WriteLine($"Graphics Console Menu - Current API: {api} - Current App: {app} - Resolution: {width}x{height} - VSync: {isVSyncEnabled} - Logging: {isLoggingEnabled}");
                Console.WriteLine("\tapi - Set Graphics API");
                Console.WriteLine("\tapp - Set Graphics Application");
                Console.WriteLine("\tres - Set Resolution");
                Console.WriteLine("\tvsync - Toggle VSync");
                Console.WriteLine("\tlog - Toggle logging");
                Console.WriteLine("\tstart - Start with current settings");
                Console.WriteLine("\tquit - ...");
                input = Console.ReadLine();
                switch (input)
                {
                    case "api":
                        {
                            if (TryReadGraphicsAPI(out GraphicsAPI apiInput))
                                api = apiInput;
                        }
                        break;
                    case "app":
                        {
                            if (TryReadApp(out string? appInput))
                                app = appInput;
                        }
                        break;
                    case "res":
                        {
                            if (TryReadResolution(out uint newWidth, out uint newHeight))
                            {
                                width = newWidth;
                                height = newHeight;
                            }
                        }
                        break;
                    case "vsync": isVSyncEnabled = !isVSyncEnabled; break;
                    case "log": isLoggingEnabled = !isLoggingEnabled; break;
                    case "start":
                        {
                            IGraphicsView view = viewFactory(width, height, isVSyncEnabled);
                            StartApp(api, DetectOS(), view, app, isLoggingEnabled);
                            if (view is IDisposable viewDisposable)
                                viewDisposable.Dispose();
                        }
                        break;

                    case "quit": quit = true; break;
                }

                Console.WriteLine();
            }
        }

        #endregion

    }
}
