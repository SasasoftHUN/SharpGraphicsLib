using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SharpGraphics.Utils;
using System.Runtime.InteropServices;

namespace SharpGraphics.GraphicsViews.NETAvalonia
{
    public partial class NativeControl : NativeControlHost
    {

        #region Fields

        private OperatingSystemType _operatingSystem;

        #endregion

        #region Properties

        public IntPtr Handle { get; private set; }
        public IPlatformSpecificViewInfo? ViewInfo { get; private set; }
        public bool IsHandleInitialized { get; private set; }

        public OperatingSystemType OperatingSystem => _operatingSystem;

        #endregion

        #region Events

        public event EventHandler? HandleInitialized;
        public event EventHandler? Destroyed;

        #endregion

        #region Constructors

        public NativeControl()
        {
            IRuntimePlatform? runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            if (runtimePlatform != null)
                _operatingSystem = runtimePlatform.GetRuntimeInfo().OperatingSystem;

            switch (_operatingSystem)
            {
                case OperatingSystemType.WinNT:
                    ViewInfo = new WindowsSpecificViewInfo();
                    break;

                case OperatingSystemType.Linux:
                    ViewInfo = CreateLinuxSpecificViewInfo();
                    break;

                //case OperatingSystemType.Unknown:
                //case OperatingSystemType.OSX:
                //case OperatingSystemType.Android:
                //case OperatingSystemType.iOS:
                default:
                    break;
            }

            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr displayName);
        [DllImport("libX11.so.6")]
        private static extern int XDefaultScreen(IntPtr display);
        [DllImport("libX11.so.6")]
        private static extern IntPtr XGetInputFocus(IntPtr display, out IntPtr window, out int focusState);
        [DllImport("libX11-xcb.so.1")]
        private static extern IntPtr XGetXCBConnection(IntPtr display);
        private LinuxSpecificViewInfo CreateLinuxSpecificViewInfo()
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);
            int screenNumber = XDefaultScreen(display);
            IntPtr connection = XGetXCBConnection(display);
            XGetInputFocus(display, out IntPtr window, out _);

            return new LinuxX11SpecificViewInfo(display, screenNumber, connection, window);
        }

        #endregion

        #region Protected Methods

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            IPlatformHandle handle = base.CreateNativeControlCore(parent);
            if (!IsHandleInitialized && handle.Handle != IntPtr.Zero)
            {
                Handle = handle.Handle;
                IsHandleInitialized = true;
                HandleInitialized?.Invoke(this, EventArgs.Empty);
            }
            return handle;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            base.DestroyNativeControlCore(control);
            IsHandleInitialized = false;
            Handle = IntPtr.Zero;
            ViewInfo = null;
            Destroyed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

    }
}
