using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpGraphics.GraphicsViews
{
    public interface IGraphicsView
    {

        #region Properties

        [MemberNotNullWhen(returnValue: true, "NativeView", "PlatformSpecificViewInfo")] bool IsViewInitialized { get; }
        object? NativeView { get; }
        IntPtr ViewHandle { get; }
        IPlatformSpecificViewInfo? PlatformSpecificViewInfo { get; }
        Vector2UInt ViewSize { get; }

        SwapChainConstruction SwapChainConstructionRequest { get; }

        IUserInputSource? UserInputSource { get; }

        #endregion

        #region Events

        /// <summary>
        /// Should fire when all of the <see cref="IGraphicsView"/>'s components have been initialized and it's ready to present Graphics
        /// </summary>
        event EventHandler<ViewInitializedEventArgs> ViewInitialized;

        /// <summary>
        /// Should fire only after the <see cref="IGraphicsView"/> is Initialized and it's size have changed.
        /// Handlers will not check if the <see cref="IGraphicsView"/> is Initialized or not!
        /// </summary>
        event EventHandler<ViewSizeChangedEventArgs> ViewSizeChanged;

        /// <summary>
        /// Should fire when the <see cref="IGraphicsView"/> is closed and destroyed by the user or by the system and it should not be Recreated, the application should close
        /// </summary>
        event EventHandler ViewDestroyed;

        #endregion

        #region Public Methods

        /// <summary>
        /// <see cref="GraphicsSwapChain"/> calls this from Dispose. The UI element should be in a clear, initialized state and ready to be attached onto a new Swap-Chain.
        /// </summary>
        public void Reinitialize();

        #endregion

    }

    public interface IPlatformSpecificViewInfo
    {

    }

    public class WindowsSpecificViewInfo : IPlatformSpecificViewInfo
    {
        
        public IntPtr ProcessHandle { get; private set; }

        public WindowsSpecificViewInfo()
        {
            using (Process process = Process.GetCurrentProcess())
                ProcessHandle = process.Handle;
        }

    }

    public abstract class LinuxSpecificViewInfo : IPlatformSpecificViewInfo
    {
    }
    public class LinuxX11SpecificViewInfo : LinuxSpecificViewInfo
    {
        public IntPtr X11Display { get; private set; }
        public int X11Screen { get; private set; }
        public IntPtr X11ServerConnectionHandle { get; private set; }
        public IntPtr X11WindowHandle { get; private set; }

        [DllImport("libX11.so.6")]
        private static extern int XDefaultScreen(IntPtr display);
        [DllImport("libX11-xcb.so.1")]
        private static extern IntPtr XGetXCBConnection(IntPtr display);

        public LinuxX11SpecificViewInfo(IntPtr x11Display, IntPtr x11WindowHandle)
        {
            X11Display = x11Display;
            X11Screen = XDefaultScreen(x11Display);
            X11ServerConnectionHandle = XGetXCBConnection(x11Display);
            X11WindowHandle = x11WindowHandle;
        }
        public LinuxX11SpecificViewInfo(IntPtr x11Display, int x11Screen, IntPtr x11ServerConnectionHandle, IntPtr x11WindowHandle)
        {
            X11Display = x11Display;
            X11Screen = x11Screen;
            X11ServerConnectionHandle = x11ServerConnectionHandle;
            X11WindowHandle = x11WindowHandle;
        }
    }

    public class MacSpecificViewInfo : IPlatformSpecificViewInfo
    {

    }

    public class AndroidSpecificViewInfo : IPlatformSpecificViewInfo
    {

    }

}
