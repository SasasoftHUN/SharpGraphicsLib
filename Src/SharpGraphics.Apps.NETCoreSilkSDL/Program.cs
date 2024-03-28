using SharpGraphics.Apps.ConsoleSelector;
using SharpGraphics.GraphicsViews;
using SharpGraphics.GraphicsViews.SilkNETSDL;
using Silk.NET.SDL;
using System;
using System.Runtime.InteropServices;

namespace SharpGraphics.Apps.NETCoreSilkSDL
{
    internal class Program
    {

        private static IGraphicsView CreateSDL2Window(uint width, uint height, bool isVSyncEnabled)
            => new SilkNETSDL2GraphicsView(width, height) { VSyncRequest = isVSyncEnabled };

        unsafe static void Main(string[] args)
        {
            Sdl sdl = Sdl.GetApi();

            if (sdl.Init(Sdl.InitVideo) < 0)
            {
                Console.Error.WriteLine($"SDL Init error: {sdl.GetErrorS()}");
                return;
            }

            GraphicsAppsConsoleMenu.Run(CreateSDL2Window);

            sdl.Quit();
        }
    }
}