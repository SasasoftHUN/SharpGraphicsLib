# SharpGraphics
Prototype low-level Graphics API abstraction layer library implemented in C# for real-time graphics application development.

## Features
- Low-level Graphics API access abstraction: Buffers, Textures, Shaders, Pipelines, Command Buffers, FrameBuffers, Render Passes, ect.
- OpenGL, OpenGL ES 3.0 and Vulkan backend implementations
- Support presentation on Silk.SDL2 windows and embedding into WPF, Avalonia and Xamarin.Android.
- C# EDSL for shader development: .NET Roslyn Analyzer tool allowing Vertex and Fragment shader implementations in C# classes.

Tested on Windows, Linux and Android with NVidia and Intel GPUs. This project is a proof-of-concept. While I consider the library well designed and optimized, it is not ready for use in production.

## Build
The projects can be built using Visual Studio 2022 or with the dotnet CLI tool (*dotnet run* or *dotnet publish* commands). The head projects are targeting .NET 8, but can target older versions down to .NET Core 3.1. Building projects referencing Android libaries requires Xamarin.Android and Mono compilers.

The first build may fail due to the C# EDSL Shader tool may not prepare the shader validator tools in time (known bug). Try the build again, the second time it should complete without errors.

## Project Structure
![Project Structure](/Images/ProjectStructure.png)

The repository contains a Visual Studio 2022 solution with multiple projects.

### Graphics API abstraction, implementation layer and UI layers
- **SharpGraphics** project encapsulates the complete Graphics API abstraction layer.
    - For implementing a graphics engine or rendering algorithm reference this project. Best to implement in a .NET Standard shared library.
- **SharpGraphics.Shaders** project contains the C# EDSL for shader development and the Roslyn Analyzer component.
    - For implementing shaders in Your C# project reference this project as an analyzer.
- **SharpGraphics.\[Vulkan/OpenGLCore/OpenGLES30\]** implementation projects for Graphics API backends.
    - Reference only the Graphics API project which You wish to use on a given platform. Best to reference in a Platform head project (e.g. WPF, Avalonia, Xamarin.Android).
- **SharpGraphics.GraphicsViews.\[SilkNETSDL/WPF/NETAvalonia/XamAndroid\]** presentation support libraries for Window or UI frameworks.
    - Reference only the project matching Your project's Window or UI framework. Best to reference in a Platform head project.

### Sample Applications
Projects beginning with **SharpGraphics.Apps.** in the **GraphicsApps** solution folder are sample rendering applications implemented with the *SharpGraphics* base library and *SharpGraphics.Shaders* EDSL libraries. These projects cannot be executed on its own, must be referenced and invoked from a Platform head project.
- **HelloTriangle**: Rendering a triangle with Vertex and Fragment shaders.
- **VertexAttributes**: Rendering a quad with Vertex Buffer.
- **PushValues**: Rendering a quad with rotating camera using transformations and Uniform Buffer.
- **Normals**: Rendering textures spheres with Phong shading.
- **NormalsThreads**: Rendering multiple textured spheres with Phong shading using multi-threaded Command Buffer construction.
- **PostProcess**: Rendering spheres with B/W post-process using multi-step Render Pass.
- **Deferred**: Deferred rendering of spheres with multi-step Render Pass.
- **Models**: Forward rendering of Sponza with Phong shading.
- **SimpleRaytrace**: Fragment shader based Raytracing of spheres.

### Platform Head Projects
Projects beginning with **SharpGraphics.Apps.** in the **Apps** solution folder should be selected as Startup projects to be executed. Referencing the App, Graphics API implementation and UI support projects, these projects set up and initialize the rendering application for a given platform and framework.
- **NETCoreSilkSDL**: .NET 8 project for Windows and Linux using Silk.SDL2 library for presentation window creation.
- **NETCoreWPF**: .NET 8 project for Windows embedding into WPF framework.
- **NETAvalonia**: .NET 8 project for Windows and Linux embedding into Avalonia framework.
- **XamAndroid**: Xamarin project for Android.
- **XamWearOS**: Xamarin project for Android WearOS.

**SharpGraphics.Shaders.StandAloneShaderGenerator** is a proof-of-concept CLI C# EDSL shader compiler tool. Can compile C# shaders from .NET solutions, projects or C# sources.

## Abstraction layer structure
![Abstraction Layer Class Hierarchy](/Images/AbstractionClassHierarchy.png)

## Credits
SharpGraphics prototype library is developed by Dávid Szabó (sasasoft@inf.elte.hu) at Eötvös Loránd University, Budapest, Hungary.