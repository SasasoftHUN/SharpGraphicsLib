using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using Silk.NET.SDL;

namespace SharpGraphics.GraphicsViews.SilkNETSDL
{
    public unsafe class SilkNETSDL2GraphicsView : IGraphicsView, IUserInputSource, IDisposable
    {

        #region Fields

        private readonly Sdl _sdl;

        private bool _isDisposed;

        private Thread* _thread;
        private Window* _window;
        private volatile bool _isRunning = true;

        private bool _immediateInputEvents = false;

        #endregion

        #region Properties

        public bool IsViewInitialized => _window != null;
        public object? NativeView => null;
        public nint ViewHandle { get; private set; }

        public IPlatformSpecificViewInfo? PlatformSpecificViewInfo { get; private set; }
        public Vector2UInt ViewSize { get; private set; } = new Vector2UInt(800, 600);
        public SwapChainConstruction SwapChainConstructionRequest => new SwapChainConstruction(VSyncRequest ? PresentMode.VSyncDoubleBuffer : PresentMode.Immediate, DataFormat.Undefined, DataFormat.Undefined);
        public IUserInputSource? UserInputSource => this;

        public bool ImmediateInputEvents
        {
            get => _immediateInputEvents;
            set
            {
                if (_immediateInputEvents != value)
                {
                    _immediateInputEvents = value;
                    if (_immediateInputEvents)
                        QueryInputEvents();
                }
            }
        }
        public IEnumerable<KeyboardKey>? HandleKeys { get; set; }
        public IEnumerable<MouseButton>? HandleMouseButtons { get; set; }
        public bool HandleMouseWheel { get; set; }
        public bool HandleMouseMove { get; set; }
        public bool HandleTouch { get; set; }

        public bool VSyncRequest { get; set; }

        #endregion

        #region Events

        public event EventHandler<ViewInitializedEventArgs>? ViewInitialized;
        public event EventHandler<ViewSizeChangedEventArgs>? ViewSizeChanged;
        public event EventHandler<UserInputEventArgs>? UserInput;
        public event EventHandler? ViewDestroyed;

        #endregion

        #region Constructors

        public SilkNETSDL2GraphicsView() : this(800, 600)
        {
        }
        public SilkNETSDL2GraphicsView(uint width, uint height)
        {
            ViewSize = new Vector2UInt(width, height);
            _sdl = Sdl.GetApi();
            InitializeSDLThread();
        }

        ~SilkNETSDL2GraphicsView() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private void InitializeSDLThread()
        {
            _thread = _sdl.CreateThread(new PfnThreadFunction(SDLThread), "SDL2_WindowThread", null, new PfnSDLCurrentBeginThread(), new PfnSDLCurrentEndThread());
            if (_thread == null)
                throw new Exception($"SDL Window Thread creation error: {_sdl.GetErrorS()}");
        }
        private void CleanSDLThread()
        {
            _isRunning = false;
            if (_thread != null)
            {
                int resultStatus = 0;
                _sdl.WaitThread(_thread, ref resultStatus);

                _thread = null;
            }
        }

        private int SDLThread(void* data)
        {
            _window = _sdl.CreateWindow("CSharp SDL", 100, 100, (int)ViewSize.x, (int)ViewSize.y, (uint)(WindowFlags.AllowHighdpi | WindowFlags.Resizable));
            if (_window == null)
            {
                //throw new Exception($"SDL Window Creation error: {_sdl.GetErrorS()}");
                return -1;
            }

            SysWMInfo sysWMInfo = new SysWMInfo();
            _sdl.GetVersion(&sysWMInfo.Version);
            if (!_sdl.GetWindowWMInfo(_window, &sysWMInfo))
            {
                CleanWindow();
                //throw new Exception($"SDL Window Info Get error: {_sdl.GetErrorS()}");
                return -1;
            }

            string platform = _sdl.GetPlatformS();
            switch (platform)
            {
                case "Windows":
                    ViewHandle = new IntPtr(sysWMInfo.Info.Win.Hwnd);
                    PlatformSpecificViewInfo = new WindowsSpecificViewInfo();
                    break;

                case "Linux":
                    if (sysWMInfo.Info.X11.Window != null)
                    {
                        ViewHandle = new IntPtr(sysWMInfo.Info.X11.Window);
                        PlatformSpecificViewInfo = new LinuxX11SpecificViewInfo(new IntPtr(sysWMInfo.Info.X11.Display), new IntPtr(sysWMInfo.Info.X11.Window));
                    }
                    else
                    {
                        CleanWindow();
                        //throw new Exception($"SDL unsupported Linux Window System");
                        return -1;
                    }
                    break;

                default:
                    CleanWindow();
                    //throw new Exception($"SDL unsupported Platform: {platform}");
                    return -1;
            }

            ViewInitialized?.Invoke(this, new ViewInitializedEventArgs(ViewSize, ViewHandle));

            _isRunning = true;
            while (_isRunning)
            {
                if (_immediateInputEvents)
                    QueryInputEvents();
                else
                {
                    _sdl.PumpEvents();
                    _sdl.Delay(1);
                }
            }

            CleanWindow();

            return 0;
        }
        private void CleanWindow()
        {
            if (_window != null)
            {
                _sdl.DestroyWindow(_window);
                _window = null;
            }
            ViewHandle = IntPtr.Zero;
        }

        private void OnUserInputEvent(UserInputEventArgs userInputEventArgs)
            => UserInput?.Invoke(this, userInputEventArgs);

        #endregion

        #region Public Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                CleanSDLThread();
                ViewDestroyed?.Invoke(this , EventArgs.Empty);
                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Reinitialize()
        {
            CleanSDLThread();
            InitializeSDLThread();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void QueryInputEvents()
        {
            Event ev;
            while (_sdl.PollEvent(&ev) != 0)
            {
                switch ((EventType)ev.Type)
                {
                    case EventType.Quit:
                        OnUserInputEvent(new UserInputCloseEventArgs());
                        break;

                    case EventType.Windowevent:
                        {
                            switch ((WindowEventID)ev.Window.Event)
                            {
                                case WindowEventID.Resized:
                                    ViewSize = new Vector2UInt((uint)ev.Window.Data1, (uint)ev.Window.Data2);
                                    ViewSizeChanged?.Invoke(this, new ViewSizeChangedEventArgs(ViewSize));
                                    break;
                            }
                        }
                        break;

                    case EventType.Keydown:
                        OnUserInputEvent(new KeyboardEventArgs(ev.Key.Keysym.ToKeyboardKey(), true));
                        break;
                    case EventType.Keyup:
                        OnUserInputEvent(new KeyboardEventArgs(ev.Key.Keysym.ToKeyboardKey(), false));
                        break;

                    case EventType.Mousemotion:
                        OnUserInputEvent(new MouseMoveEventArgs(new InputMovement2D(ev.Motion.X, ev.Motion.Y, ev.Motion.Xrel, ev.Motion.Yrel)));
                        break;
                    case EventType.Mousebuttondown:
                        OnUserInputEvent(new MouseButtonEventArgs(ev.Button.Button.ToMouseButton(), true));
                        break;
                    case EventType.Mousebuttonup:
                        OnUserInputEvent(new MouseButtonEventArgs(ev.Button.Button.ToMouseButton(), false));
                        break;
                }
            }
        }
        public void DiscardInputEvents()
        {
            Event ev;
            while (_sdl.PollEvent(&ev) != 0) ;
        }

        #endregion

    }
}
