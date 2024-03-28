using SharpGraphics.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SharpGraphics.GraphicsViews.WPF
{
    /// <summary>
    /// Interaction logic for WPFGraphicsView.xaml
    /// </summary>
    public partial class WPFGraphicsView : UserControl, IGraphicsView, IUserInputSource, IDisposable
    {
        
        #region Fields

        private bool _isDisposed;
        private bool _immediateInputEvents = false;

        private System.Windows.Forms.PictureBox? _wfPicture;

        private ConcurrentQueue<UserInputEventArgs> _inputQueue = new ConcurrentQueue<UserInputEventArgs>();
        private System.Windows.Point? _previousMousePosition = default(System.Windows.Point?);

        #endregion

        #region Properties

        public bool IsViewInitialized { get; private set; }
        public object? NativeView => _wfPicture;
        public IntPtr ViewHandle => _wfPicture?.Handle ?? IntPtr.Zero;
        public Vector2UInt ViewSize { get; private set; }
        public IPlatformSpecificViewInfo PlatformSpecificViewInfo { get; private set; }

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

        #endregion

        #region Events

        public event EventHandler<ViewInitializedEventArgs>? ViewInitialized;
        public event EventHandler<ViewSizeChangedEventArgs>? ViewSizeChanged;
        public event EventHandler<UserInputEventArgs>? UserInput;
        public event EventHandler? ViewDestroyed;

        #endregion

        #region Constructors

        public WPFGraphicsView()
        {
            PlatformSpecificViewInfo = new WindowsSpecificViewInfo();
            InitializeComponent();
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /*~WPFGraphicsView()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }*/

        #endregion

        #region Control Event Handlers

        private void UserControl_Loaded(object? sender, RoutedEventArgs e)
        {
            CreateWFPicture();
            IsViewInitialized = true;
            ViewInitialized?.Invoke(this, new ViewInitializedEventArgs(ViewSize, ViewHandle));
        }

        private void _wfHost_SizeChanged(object? sender, SizeChangedEventArgs e)
            => SetSize(e.NewSize.Width, e.NewSize.Height);

        private void _wfPicture_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsViewInitialized/* && !e.Handled*/)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent(true));
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent(true));

                /*if (HandleMouseButtons != null && HandleMouseButtons.Contains(e.ChangedButton.ToMouseButton()))
                    e.Handled = true;*/
            }
        }
        private void _wfPicture_MouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsViewInitialized/* && !e.Handled*/)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent(false));
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent(false));

                /*if (HandleMouseButtons != null && HandleMouseButtons.Contains(e.ChangedButton.ToMouseButton()))
                    e.Handled = true;*/
            }
        }
        private void _wfPicture_MouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsViewInitialized/* && !e.Handled*/)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(GetMouseMoveEvent(new System.Windows.Point(e.Location.X, e.Location.Y)));
                else _inputQueue.Enqueue(GetMouseMoveEvent(new System.Windows.Point(e.Location.X, e.Location.Y)));

                /*if (HandleMouseMove)
                    e.Handled = true;*/
            }
        }
        private void _wfPicture_MouseLeave(object? sender, EventArgs e)
            => _previousMousePosition = default(System.Windows.Point?);

        #endregion

        #region Private Methods

        private void CreateWFPicture()
        {
            if (_wfPicture != null)
            {
                _wfPicture.MouseDown -= _wfPicture_MouseDown;
                _wfPicture.MouseUp -= _wfPicture_MouseUp;
                _wfPicture.MouseMove -= _wfPicture_MouseMove;
                _wfPicture.MouseLeave -= _wfPicture_MouseLeave;
                _wfPicture.Dispose();
            }

            _wfPicture = new System.Windows.Forms.PictureBox()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
            };
            _wfHost.Child = _wfPicture;
            SetSize();

            _wfPicture.MouseDown += _wfPicture_MouseDown;
            _wfPicture.MouseUp += _wfPicture_MouseUp;
            _wfPicture.MouseMove += _wfPicture_MouseMove;
            _wfPicture.MouseLeave += _wfPicture_MouseLeave;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSize() => SetSize(Convert.ToUInt32(Math.Ceiling(_wfHost.ActualWidth)), Convert.ToUInt32(Math.Ceiling(_wfHost.ActualHeight)));
        private void SetSize(double width, double height)
        {
            float scaleX = 1f;
            float scaleY = 1f;
            using (Graphics graphics = Graphics.FromHwnd(_wfHost.Handle))
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

                Debug.WriteLine("WPF GraphicsView Size Changed: " + ViewSize.ToString());
            }
        }

        private SharpGraphics.GraphicsViews.MouseMoveEventArgs GetMouseMoveEvent(System.Windows.Point position)
        {
            if (_previousMousePosition.HasValue)
            {
                System.Windows.Point difference = new System.Windows.Point(position.X - _previousMousePosition.Value.X, position.Y - _previousMousePosition.Value.Y);
                _previousMousePosition = position;
                return new SharpGraphics.GraphicsViews.MouseMoveEventArgs(new InputMovement2D(position.X, position.Y, difference.X, difference.Y));
            }
            else
            {
                _previousMousePosition = position;
                return new SharpGraphics.GraphicsViews.MouseMoveEventArgs(new InputMovement2D(position.X, position.Y));
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (IsViewInitialized && !e.Handled)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent());
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent());

                if (HandleKeys != null && HandleKeys.Contains(e.Key.ToKeyboardKey()))
                    e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            if (IsViewInitialized && !e.Handled)
            {
                if (_immediateInputEvents)
                    OnUserInterfaceInput(e.ToUserInterfaceEvent());
                else _inputQueue.Enqueue(e.ToUserInterfaceEvent());

                if (HandleKeys != null && HandleKeys.Contains(e.Key.ToKeyboardKey()))
                    e.Handled = true;
            }
            base.OnKeyUp(e);
        }

        protected void OnUserInterfaceInput(UserInputEventArgs inputEvent) => UserInput?.Invoke(this, inputEvent);

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                IsViewInitialized = false;

                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    if (_wfPicture != null)
                        _wfPicture.Dispose();
                    _wfHost.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Reinitialize() => CreateWFPicture();

        public void QueryInputEvents()
        {
            while (_inputQueue.TryDequeue(out UserInputEventArgs? inputEvent) && inputEvent != null)
                OnUserInterfaceInput(inputEvent);
        }
#if NETFRAMEWORK
        public void DiscardInputEvents()
        {
            while (_inputQueue.TryDequeue(out _)) ;
        }
#else
        public void DiscardInputEvents() => _inputQueue.Clear();
#endif


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

#endregion

    }
}