using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SharpGraphics.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGraphics.GraphicsViews.NETAvalonia
{
    public class AvaloniaGraphicsView : Panel, IGraphicsView, IUserInputSource, IDisposable
    {

        #region Fields

        private bool _isDisposed = false;
        private NativeControl? _nativeControl;
        private OperatingSystemType _operatingSystem;

        private bool _immediateInputEvents = false;

        private ConcurrentQueue<UserInputEventArgs> _inputQueue = new ConcurrentQueue<UserInputEventArgs>();
        //private Point? _previousMousePosition = default(Point?);

        #endregion

        #region Properties

        public bool IsViewInitialized { get; private set; }
        public object? NativeView => _nativeControl;
        public IntPtr ViewHandle => _nativeControl != null ? _nativeControl.Handle : IntPtr.Zero;
        public Vector2UInt ViewSize { get; private set; }
        public IPlatformSpecificViewInfo? PlatformSpecificViewInfo => _nativeControl?.ViewInfo;

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

        public SwapChainConstruction SwapChainConstructionRequest => new SwapChainConstruction(PresentMode, ColorFormat, DepthStencilFormat);

        public IUserInputSource? UserInputSource => this;

        public PresentMode PresentMode { get; set; } = PresentMode.VSyncDoubleBuffer;
        public DataFormat ColorFormat { get; set; } = DataFormat.Undefined;
        public DataFormat DepthStencilFormat { get; set; } = DataFormat.Depth24un;

        public OperatingSystemType OperatingSystem => _operatingSystem;

        #endregion

        #region Events

        public event EventHandler<ViewInitializedEventArgs>? ViewInitialized;
        public event EventHandler<ViewSizeChangedEventArgs>? ViewSizeChanged;
        public event EventHandler<UserInputEventArgs>? UserInput;
        public event EventHandler? ViewDestroyed;

        #endregion

        #region Constructors

        public AvaloniaGraphicsView()
        {
            IRuntimePlatform? runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            if (runtimePlatform != null)
                _operatingSystem = runtimePlatform.GetRuntimeInfo().OperatingSystem;

            InitializeComponent();
        }

        ~AvaloniaGraphicsView() => Dispose(false);

        #endregion

        #region Control Event Handlers

        private void AvaloniaGraphicsView_Initialized(object? sender, EventArgs e)
        {
            CreateNativeControl();
        }

        private void _nativeControl_HandleInitialized(object? sender, EventArgs e) => InitializeGraphicsView();
        private void _nativeControl_Destroyed(object? sender, EventArgs e)
        {
            if (sender != null && sender is NativeControl nativeControl)
            {
                nativeControl.HandleInitialized -= _nativeControl_HandleInitialized;
                nativeControl.Destroyed -= _nativeControl_Destroyed;

                if (nativeControl == _nativeControl)
                {
                    IsViewInitialized = false;
                    _nativeControl = null;
                }
            }
        }

        private void Control_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (IsViewInitialized && !e.Handled)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent(true));
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent(true));

                if (HandleKeys != null && HandleKeys.Contains(e.Key.ToKeyboardKey()))
                    e.Handled = true;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            Initialized += AvaloniaGraphicsView_Initialized;

            AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Direct | Avalonia.Interactivity.RoutingStrategies.Bubble, true);
        }

        private void CreateNativeControl()
        {
            if (!IsInitialized)
                return;
            
            if (_nativeControl != null)
            {
                Children.Remove(_nativeControl);
            }
            
            _nativeControl = new NativeControl()
            {
                IsHitTestVisible = false,
                Focusable = false,
            };
            _nativeControl.HandleInitialized += _nativeControl_HandleInitialized;
            _nativeControl.Destroyed += _nativeControl_Destroyed;
            Children.Add(_nativeControl);
            InitializeGraphicsView();
        }

        private void InitializeGraphicsView()
        {
            if (_nativeControl != null && _nativeControl.IsHandleInitialized)
            {
                if (!IsViewInitialized)
                {
                    IsViewInitialized = true;
                    ViewInitialized?.Invoke(this, new ViewInitializedEventArgs(ViewSize, ViewHandle));
                }
            }
        }

        private bool SetSize(double width, double height)
        {
            if (double.IsNaN(width) || double.IsNaN(height) || width <= 0 || height <= 0 || _nativeControl == null)
                return false;
            
            float scaleX = 1f;
            float scaleY = 1f;
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(_nativeControl.Handle))
            {
                scaleX = graphics.DpiX / 96f;
                scaleY = graphics.DpiY / 96f;
            }

            lock (this)
            {
                ViewSize = new Vector2UInt(
                    Convert.ToUInt32(Math.Ceiling(width * scaleX)),
                    Convert.ToUInt32(Math.Ceiling(height * scaleY))
                );

                if (IsViewInitialized)
                    ViewSizeChanged?.Invoke(this, new ViewSizeChangedEventArgs(ViewSize));

                Debug.WriteLine("SizeChange: " + ViewSize.ToString());
            }

            return false;
        }

        #endregion

        #region Protected Methods

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);
            SetSize(size.Width, size.Height);
            return size;
        }

        protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
        {
            if (IsViewInitialized && !e.Handled)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent(true));
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent(true));

                if (HandleKeys != null && HandleKeys.Contains(e.Key.ToKeyboardKey()))
                    e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(Avalonia.Input.KeyEventArgs e)
        {
            if (IsViewInitialized && !e.Handled)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent(false));
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent(false));

                if (HandleKeys != null && HandleKeys.Contains(e.Key.ToKeyboardKey()))
                    e.Handled = true;
            }
            base.OnKeyUp(e);
        }


        protected void OnUserInterfaceInput(UserInputEventArgs inputEvent) => UserInput?.Invoke(this, inputEvent);

        protected void Dispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                //Free any other managed objects here.
            }

            //Free any unmanaged objects here.
            IsViewInitialized = false;

            _isDisposed = true;
        }

        #endregion

        #region Public Methods

        public void Reinitialize() => CreateNativeControl();

        public void QueryInputEvents()
        {
            while (_inputQueue.TryDequeue(out UserInputEventArgs? inputEvent))
                OnUserInterfaceInput(inputEvent);
        }
        public void DiscardInputEvents() => _inputQueue.Clear();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
