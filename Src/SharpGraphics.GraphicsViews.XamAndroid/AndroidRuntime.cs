using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.GraphicsViews.XamAndroid
{
    /// <summary>
    /// Function imports from the Android runtime library (android.so).
    /// </summary>
    internal static class AndroidRuntime
    {
        private const string LIBRARY_NAME = "android.so";

        [DllImport(LIBRARY_NAME)]
        public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);
        [DllImport(LIBRARY_NAME)]
        public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);
        [DllImport(LIBRARY_NAME)]
        public static extern void ANativeWindow_release(IntPtr aNativeWindow);
    }
}