using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using SharpGraphics.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGraphics.GraphicsViews.XamAndroid
{
    public class XamAndroidGraphicsView : SurfaceView, ISurfaceHolderCallback, IGraphicsView, IUserInputSource
    {

        #region Fields

        private bool _isSurfaceInitialized = false;
        private bool _haveSize = false;

        private bool _immediateInputEvents = false;
        private ConcurrentQueue<UserInputEventArgs> _inputQueue = new ConcurrentQueue<UserInputEventArgs>();

        private Dictionary<int, Vector2> _touchStartPositions = new Dictionary<int, Vector2>();

        private bool _isDisposed = false;

        #endregion

        #region Properties

        public bool IsViewInitialized { get; private set; }
        public object NativeView { get => this; }
        public IntPtr ViewHandle { get; private set; }
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
        public IEnumerable<KeyboardKey> HandleKeys { get; set; }
        public IEnumerable<MouseButton> HandleMouseButtons { get; set; }
        public bool HandleMouseWheel { get; set; }
        public bool HandleMouseMove { get; set; }
        public bool HandleTouch { get; set; }

        public SwapChainConstruction SwapChainConstructionRequest => new SwapChainConstruction(VSyncRequest ? PresentMode.VSyncDoubleBuffer : PresentMode.Immediate, DataFormat.Undefined, DataFormat.Depth24un_Stencil8ui);

        public IUserInputSource UserInputSource => this;

        public bool VSyncRequest { get; set; } = true;

        #endregion

        #region Events

        public event EventHandler<ViewInitializedEventArgs> ViewInitialized;
        public event EventHandler<ViewSizeChangedEventArgs> ViewSizeChanged;
        public event EventHandler<UserInputEventArgs> UserInput;
        public event EventHandler? ViewDestroyed;

        #endregion

        #region Constructors

        public XamAndroidGraphicsView(Context context, IAttributeSet attrs) : base(context, attrs) => InitializeSurface();
        public XamAndroidGraphicsView(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) => InitializeSurface();

        ~XamAndroidGraphicsView() => Dispose(false);

        #endregion

        #region Private Methods

        private void InitializeSurface()
        {
            Holder.AddCallback(this);
        }

        private void Initialize()
        {
            if (_isSurfaceInitialized && _haveSize && !IsViewInitialized)
            {
                Touch += XamAndroidGraphicsView_Touch;
                GenericMotion += XamAndroidGraphicsView_GenericMotion;
                IsViewInitialized = true;
                ViewInitialized?.Invoke(this, new ViewInitializedEventArgs(ViewSize, ViewHandle));
            }
        }

        private void ReleaseSurface()
        {
            if (ViewHandle != IntPtr.Zero)
            {
                AndroidRuntime.ANativeWindow_release(ViewHandle);
                ViewHandle = IntPtr.Zero;
            }

            Touch -= XamAndroidGraphicsView_Touch;
            GenericMotion -= XamAndroidGraphicsView_GenericMotion;
            _isSurfaceInitialized = false;
            _haveSize = false;
            IsViewInitialized = false;
            PlatformSpecificViewInfo = null;
        }

        private void XamAndroidGraphicsView_Touch(object sender, TouchEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Touch: {e.Event}");

            if (IsViewInitialized/* && !e.Handled*/)
            {
                for (int i = 0; i < e.Event.PointerCount; i++)
                {
                    int pointerID = e.Event.GetPointerId(i);
                    Vector2? startPosition = _touchStartPositions.TryGetValue(pointerID, out Vector2 sp) ? sp : default(Vector2?);
                    if (e.ToUserInterfaceEvent(i, startPosition, out UserInputEventArgs ev, out Vector2? position))
                    {
                        if (_immediateInputEvents)
                            OnUserInterfaceInput(ev);
                        else _inputQueue.Enqueue(ev);

                        if (position.HasValue)
                            _touchStartPositions[pointerID] = position.Value;
                        else _touchStartPositions.Remove(pointerID);

                        if (HandleTouch)
                            e.Handled = true;
                    }
                }
            }
        }
        private void XamAndroidGraphicsView_GenericMotion(object sender, GenericMotionEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Generic: {e.Event}");

            if (IsViewInitialized/* && !e.Handled*/)
            {
                for (int i = 0; i < e.Event.PointerCount; i++)
                    if (e.ToUserInterfaceEvent(i, out UserInputEventArgs ev))
                    {
                        if (_immediateInputEvents)
                            OnUserInterfaceInput(ev);
                        else _inputQueue.Enqueue(ev);

                        //TODO: Handle... handles...
                    }
            }
        }

        #endregion

        #region Protected Methods

        protected void OnUserInterfaceInput(UserInputEventArgs inputEvent) => UserInput?.Invoke(this, inputEvent);

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                //if (disposing) ?

                ReleaseSurface();

                _isDisposed = true;
            }
            
            base.Dispose(disposing);
        }

        #endregion

        #region ISurfaceHolderCallback Public Methods

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            ViewHandle = AndroidRuntime.ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);
            PlatformSpecificViewInfo = new AndroidSpecificViewInfo();
            _isSurfaceInitialized = true;

            Initialize();
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            if (width > 0 && height > 0)
            {
                _haveSize = true;
                ViewSize = new Vector2UInt((uint)width, (uint)height);
            
                if (IsViewInitialized)
                    ViewSizeChanged?.Invoke(this, new ViewSizeChangedEventArgs(ViewSize));

                Initialize();
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            _isSurfaceInitialized = false;
            _haveSize = false;
            IsViewInitialized = false;
            ViewHandle = IntPtr.Zero;
            PlatformSpecificViewInfo = null;
        }

        #endregion

        #region Public Methods

        public void Reinitialize() { }

        public void QueryInputEvents()
        {
            while (_inputQueue.TryDequeue(out UserInputEventArgs inputEvent))
                OnUserInterfaceInput(inputEvent);
        }
        public void DiscardInputEvents() => _inputQueue.Clear();

        #endregion

    }
}