using System;
using System.Runtime.InteropServices;

namespace SharpGraphics.Shaders.Utils
{
	public static class EnvUtils
	{
		public static OSPlatform? GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OSPlatform.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSPlatform.OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OSPlatform.Linux;
            else return default(OSPlatform?);
        }
	}
}

