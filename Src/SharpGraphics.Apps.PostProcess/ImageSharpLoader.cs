//https://github.com/xamarin/mobile-samples/blob/master/EmbeddedResources/SharedLib/ResourceLoader.cs

using SharpGraphics.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpGraphics.Apps.PostProcess
{
    /// <summary>
    /// Utility class that can be used to find and load embedded resources into memory.
    /// </summary>
    public static class ImageSharpLoader
    {
        public static Image<T> GetEmbeddedResourceImage<T>(string resourceFileName) where T : unmanaged, IPixel, IPixel<T>
            => GetEmbeddedResourceImage<T>(Assembly.GetCallingAssembly(), resourceFileName);

        public static Image<T> GetEmbeddedResourceImage<T>(Assembly assembly, string resourceFileName) where T : unmanaged, IPixel, IPixel<T>
        {
            using (Stream stream = ResourceLoader.GetEmbeddedResourceStream(assembly, resourceFileName))
                return Image.Load<T>(stream);
        }
    }
}
