using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL.Contexts
{

    public class WGLContextCreationRequest : IGLContextCreationRequest
    {
        public bool IsWindowRecreationExceptionAllowed { get; }

        public WGLContextCreationRequest(bool isWindowRecreationExceptionAllowed)
        {
            IsWindowRecreationExceptionAllowed = isWindowRecreationExceptionAllowed;
        }
    }

    public abstract class WGLContext : IGLContext
    {

        //WORD - ushort
        //DWORD - uint

        [Flags]
        private enum WGLExtendedWindowStyles : uint
        {
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_NOACTIVATE = 0x08000000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
            WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
            WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_WINDOWEDGE = 0x00000100,
        }
        [Flags]
        private enum WGLWindowStyles : uint
        {
            WS_BORDER = 0x00800000,
            WS_CAPTION = 0x00C00000,
            WS_CHILD = 0x40000000,
            WS_CHILDWINDOW = 0x40000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_DISABLED = 0x08000000,
            WS_DLGFRAME = 0x00400000,
            WS_GROUP = 0x00020000,
            WS_HSCROLL = 0x00100000,
            WS_ICONIC = 0x20000000,
            WS_MAXIMIZE = 0x01000000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_OVERLAPPED = 0x00000000,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000,
            WS_SIZEBOX = 0x00040000,
            WS_SYSMENU = 0x00080000,
            WS_TABSTOP = 0x00010000,
            WS_THICKFRAME = 0x00040000,
            WS_TILED = 0x00000000,
            WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x00200000,
        }


        [Flags]
        private enum FORMAT_MESSAGE : uint
        {
            ALLOCATE_BUFFER = 0x00000100,
            IGNORE_INSERTS = 0x00000200,
            FROM_SYSTEM = 0x00001000,
            ARGUMENT_ARRAY = 0x00002000,
            FROM_HMODULE = 0x00000800,
            FROM_STRING = 0x00000400
        }


        [Flags]
        private enum WGLPixelFormatDescriptorFlags : uint
        {
            // PixelFormatDescriptor flags
            DOUBLEBUFFER = 0x01,
            STEREO = 0x02,
            DRAW_TO_WINDOW = 0x04,
            DRAW_TO_BITMAP = 0x08,
            SUPPORT_GDI = 0x10,
            SUPPORT_OPENGL = 0x20,
            GENERIC_FORMAT = 0x40,
            NEED_PALETTE = 0x80,
            NEED_SYSTEM_PALETTE = 0x100,
            SWAP_EXCHANGE = 0x200,
            SWAP_COPY = 0x400,
            SWAP_LAYER_BUFFERS = 0x800,
            GENERIC_ACCELERATED = 0x1000,
            SUPPORT_DIRECTDRAW = 0x2000,
            SUPPORT_COMPOSITION = 0x8000,

            // PixelFormatDescriptor flags for use in ChoosePixelFormat only
            DEPTH_DONTCARE = unchecked((uint)0x20000000),
            DOUBLEBUFFER_DONTCARE = unchecked((uint)0x40000000),
            STEREO_DONTCARE = unchecked((uint)0x80000000),

            // With the glAddSwapHintRectWIN extension function
            PFD_SWAP_COPY = 0x00000400,
            PFD_SWAP_EXCHANGE = 0x00000200,
        }
        private enum WGLPixelType : byte
        {
            RGBA = 0,
            INDEXED = 1
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WGLPixelFormatDescriptor
        {
            public ushort Size;
            public ushort Version;
            public WGLPixelFormatDescriptorFlags Flags;
            public WGLPixelType PixelType;
            public byte ColorBits;
            public byte RedBits;
            public byte RedShift;
            public byte GreenBits;
            public byte GreenShift;
            public byte BlueBits;
            public byte BlueShift;
            public byte AlphaBits;
            public byte AlphaShift;
            public byte AccumBits;
            public byte AccumRedBits;
            public byte AccumGreenBits;
            public byte AccumBlueBits;
            public byte AccumAlphaBits;
            public byte DepthBits;
            public byte StencilBits;
            public byte AuxBuffers;
            public PFD_LAYER_TYPES LayerType;
            private byte Reserved;
            public uint LayerMask;
            public uint VisibleMask;
            public uint DamageMask;
        }
        private enum PFD_LAYER_TYPES : byte
        {
            PFD_MAIN_PLANE = 0,
            PFD_OVERLAY_PLANE = 1,
            PFD_UNDERLAY_PLANE = 255
        }

        //WGL_ARB_create_context
        private const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        private const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
        private const int WGL_CONTEXT_LAYER_PLANE_ARB   = 0x2093;
        private const int WGL_CONTEXT_FLAGS_ARB         = 0x2094;
        private const int WGL_CONTEXT_PROFILE_MASK_ARB  = 0x9126;

        [Flags]
        private enum WGL_CONTEXT_FLAGS_ARB_BITS : int
        {
            WGL_CONTEXT_DEBUG_BIT_ARB = 0x0001,
            WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB = 0x0002,
        }

        private const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;
        private const int WGL_CONTEXT_COMPATIBILITY_PROFILE_BIT_ARB = 0x00000002;

        private const int ERROR_INVALID_VERSION_ARB = 0x2095;
        private const int ERROR_INVALID_PROFILE_ARB = 0x2096;

        //WGL_ARB_pixel_format
        private const int WGL_NUMBER_PIXEL_FORMATS_ARB          = 0x2000;
        private const int WGL_DRAW_TO_WINDOW_ARB                = 0x2001;
        private const int WGL_DRAW_TO_BITMAP_ARB                = 0x2002;
        private const int WGL_ACCELERATION_ARB                  = 0x2003;
        private const int WGL_NEED_PALETTE_ARB                  = 0x2004;
        private const int WGL_NEED_SYSTEM_PALETTE_ARB           = 0x2005;
        private const int WGL_SWAP_LAYER_BUFFERS_ARB            = 0x2006;
        private const int WGL_SWAP_METHOD_ARB                   = 0x2007;
        private const int WGL_NUMBER_OVERLAYS_ARB               = 0x2008;
        private const int WGL_NUMBER_UNDERLAYS_ARB              = 0x2009;
        private const int WGL_TRANSPARENT_ARB                   = 0x200A;
        private const int WGL_TRANSPARENT_RED_VALUE_ARB         = 0x2037;
        private const int WGL_TRANSPARENT_GREEN_VALUE_ARB       = 0x2038;
        private const int WGL_TRANSPARENT_BLUE_VALUE_ARB        = 0x2039;
        private const int WGL_TRANSPARENT_ALPHA_VALUE_ARB       = 0x203A;
        private const int WGL_TRANSPARENT_INDEX_VALUE_ARB       = 0x203B;
        private const int WGL_SHARE_DEPTH_ARB                   = 0x200C;
        private const int WGL_SHARE_STENCIL_ARB                 = 0x200D;
        private const int WGL_SHARE_ACCUM_ARB                   = 0x200E;
        private const int WGL_SUPPORT_GDI_ARB                   = 0x200F;
        private const int WGL_SUPPORT_OPENGL_ARB                = 0x2010;
        private const int WGL_DOUBLE_BUFFER_ARB                 = 0x2011;
        private const int WGL_STEREO_ARB                        = 0x2012;
        private const int WGL_PIXEL_TYPE_ARB                    = 0x2013;
        private const int WGL_COLOR_BITS_ARB                    = 0x2014;
        private const int WGL_RED_BITS_ARB                      = 0x2015;
        private const int WGL_RED_SHIFT_ARB                     = 0x2016;
        private const int WGL_GREEN_BITS_ARB                    = 0x2017;
        private const int WGL_GREEN_SHIFT_ARB                   = 0x2018;
        private const int WGL_BLUE_BITS_ARB                     = 0x2019;
        private const int WGL_BLUE_SHIFT_ARB                    = 0x201A;
        private const int WGL_ALPHA_BITS_ARB                    = 0x201B;
        private const int WGL_ALPHA_SHIFT_ARB                   = 0x201C;
        private const int WGL_ACCUM_BITS_ARB                    = 0x201D;
        private const int WGL_ACCUM_RED_BITS_ARB                = 0x201E;
        private const int WGL_ACCUM_GREEN_BITS_ARB              = 0x201F;
        private const int WGL_ACCUM_BLUE_BITS_ARB               = 0x2020;
        private const int WGL_ACCUM_ALPHA_BITS_ARB              = 0x2021;
        private const int WGL_DEPTH_BITS_ARB                    = 0x2022;
        private const int WGL_STENCIL_BITS_ARB                  = 0x2023;
        private const int WGL_AUX_BUFFERS_ARB                   = 0x2024;

        private const int WGL_NO_ACCELERATION_ARB               = 0x2025;
        private const int WGL_GENERIC_ACCELERATION_ARB          = 0x2026;
        private const int WGL_FULL_ACCELERATION_ARB             = 0x2027;

        private const int WGL_SWAP_EXCHANGE_ARB                 = 0x2028;
        private const int WGL_SWAP_COPY_ARB                     = 0x2029;
        private const int WGL_SWAP_UNDEFINED_ARB                = 0x202A;

        private const int WGL_TYPE_RGBA_ARB                     = 0x202B;
        private const int WGL_TYPE_COLORINDEX_ARB               = 0x202C;


        [DllImport("User32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr CreateWindowExA(WGLExtendedWindowStyles dwExStyle, string lpClassName, string lpWindowName, WGLWindowStyles dwStlye, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        
        [DllImport("User32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern bool DestroyWindow(IntPtr hWnd);


        [DllImport("User32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("User32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("Kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern uint GetLastError();

        [DllImport("Kernel32.dll")]
        private static extern int FormatMessage(FORMAT_MESSAGE dwFlags, IntPtr lpSource, int dwMessageId, uint dwLanguageId, out StringBuilder lpBuffer, int nSize, IntPtr arguments);

        [DllImport("Kernel32.dll", EntryPoint = "LoadLibraryA", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr LoadLibraryA(string lpszProc);

        [DllImport("Kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr GetProcAddress(IntPtr module, string lpszProc);



        [DllImport("OPENGL32.dll", EntryPoint = "wglChoosePixelFormat", ExactSpelling = true, SetLastError = true)]
        private extern static int ChoosePixelFormat(IntPtr hDc, ref WGLPixelFormatDescriptor pPfd);

        [DllImport("gdi32.dll", EntryPoint = "GetPixelFormat", ExactSpelling = true, SetLastError = true)] //Why not opengl32.dll wglGetPixelFormat? Makes my unconfortable...
        private extern static int GetPixelFormat(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "SetPixelFormat", ExactSpelling = true, SetLastError = true)] //Why not opengl32.dll wglSetPixelFormat? Makes my unconfortable...
        private extern static bool SetPixelFormat(IntPtr hdc, int ipfd, ref WGLPixelFormatDescriptor ppfd);
        [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private extern static int DescribePixelFormat(IntPtr hDc, int iPixelFormat, uint nBytes, ref WGLPixelFormatDescriptor pPfd);

        [DllImport("OPENGL32.dll", EntryPoint = "wglCreateContext", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr CreateContext(IntPtr hDc);

        [DllImport("OPENGL32.dll", EntryPoint = "wglDeleteContext", ExactSpelling = true, SetLastError = true)]
        private extern static bool DeleteContext(IntPtr contextHandle);

        [DllImport("OPENGL32.dll", EntryPoint = "wglMakeCurrent", ExactSpelling = true, SetLastError = true)]
        private extern static bool MakeCurrent(IntPtr hDc, IntPtr newContext);

        [DllImport("OPENGL32.dll", EntryPoint = "wglSwapBuffers", ExactSpelling = true, SetLastError = true)]
        private extern static bool SwapBuffers(IntPtr hdc);


        [DllImport("OPENGL32.dll", EntryPoint = "wglGetProcAddress", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr wglGetProcAddress(string lpszProc);

        [DllImport("OPENGL32.dll", EntryPoint = "wglGetProcAddress", ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr wglGetProcAddress(IntPtr lpszProc);


        private delegate string wglGetExtensionsStringARB(IntPtr hdc);
        private delegate bool wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, uint nAttributes, int[] piAttributes, [Out] int[] piValues);
        private delegate bool wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, uint nAttributes, int[] piAttributes, [Out] float[] pfValues);
        private delegate bool wglChoosePixelFormatARB(IntPtr hdc, int[] piAttribIList, float[] pfAttribFList, uint nMaxFormats, [Out] int[] piFormats, out uint nNumFormats);
        private delegate IntPtr wglCreateContextAttribsARB(IntPtr hdc, IntPtr hShaderContext, int[] attribList);

        internal const uint ERROR_INVALID_PIXEL_FORMAT = 0x7D0;

        internal const uint ERROR_STATUS_DLL_NOT_FOUND = 0xC0072095;

        #region Fields

        private static bool _wglExtensionsQueried;
        private static wglCreateContextAttribsARB? _createContextAttribsARB;
        private static wglGetPixelFormatAttribivARB? _getPixelFormatAttribivARB;
        private static wglGetPixelFormatAttribfvARB? _getPixelFormatAttribfvARB;
        private static wglChoosePixelFormatARB? _choosePixelFormatARB;

        private bool _isDisposed;

        private readonly bool _ownWindow;
        private readonly IntPtr _viewHandle;
        private readonly IntPtr _hDC;
        private readonly IntPtr _contextHandle;

        #endregion

        #region Properties

        public abstract int SwapInterval { set; } //Need runtime binding, DllImport doesn't work, therefore it is abstract

        #endregion

        #region Constructors

        protected WGLContext()
        {
            _ownWindow = true;
            _viewHandle = CreateWindowExA(WGLExtendedWindowStyles.WS_EX_TRANSPARENT | WGLExtendedWindowStyles.WS_EX_LAYERED, "STATIC", "", WGLWindowStyles.WS_POPUP, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_viewHandle == IntPtr.Zero)
                throw new WGLGraphicsDeviceCreationException(GetLastError(), "creating Hidden Window");

            WGLPixelFormatDescriptor pfd = new WGLPixelFormatDescriptor()
            {
                Size = (ushort)Marshal.SizeOf<WGLPixelFormatDescriptor>(),
                Version = 1,
                Flags = WGLPixelFormatDescriptorFlags.DRAW_TO_BITMAP | WGLPixelFormatDescriptorFlags.SUPPORT_OPENGL | WGLPixelFormatDescriptorFlags.DOUBLEBUFFER,
                PixelType = WGLPixelType.RGBA,
                ColorBits = 32,
                RedBits = 8, RedShift = 0, GreenBits = 8, GreenShift = 0, BlueBits = 8, BlueShift = 0, AlphaBits = 8, AlphaShift = 0,
                AccumBits = 0, AccumRedBits = 0, AccumGreenBits = 0, AccumBlueBits = 0, AccumAlphaBits = 0,
                DepthBits = 24, StencilBits = 8,
                AuxBuffers = 0,
                LayerType = PFD_LAYER_TYPES.PFD_MAIN_PLANE,
                LayerMask = 0, VisibleMask = 0, DamageMask = 0,
            };

            _hDC = GetDC(_viewHandle);
            if (_hDC == IntPtr.Zero)
            {
                uint errno = GetLastError();
                DestroyWindow(_viewHandle);
                throw new WGLGraphicsDeviceCreationException(errno, "getting Device Context Handle");
            }

            try
            {
                int currentPixelFormat = GetPixelFormat(_hDC);
                if (currentPixelFormat == 0)
                {
                    uint getPixelFormatErrno = GetLastError();
                    if (getPixelFormatErrno != 0u && getPixelFormatErrno != ERROR_INVALID_PIXEL_FORMAT) //0 means GetPixelFormat successfully queried, but there is no format set yet
                        throw new WGLGraphicsDeviceCreationException(getPixelFormatErrno, "getting Pixel Format");
                }

                _contextHandle = CraeteContextObsoleted(_hDC, ref pfd, currentPixelFormat, false); //IntPtr.Zero return value tested internally

                if (!MakeCurrent(_hDC, _contextHandle))
                    throw new WGLGraphicsDeviceCreationException(GetLastError(), "activating created OpenGL Context");
                LoadBindings();
            }
            catch
            {
                ReleaseDC(_viewHandle, _hDC);
                DestroyWindow(_viewHandle);
                throw;
            }
        }
        protected WGLContext(IGraphicsView graphicsView, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext, WGLContextCreationRequest? contextCreationRequest)
        {
            SwapChainConstruction constructionRequest = graphicsView.SwapChainConstructionRequest;
            constructionRequest.mode.ToSwapInterval(out _, out int backBufferCount);
            constructionRequest.colorFormat.ToColorFormat(out int colorBits, out int redBits, out int greenBits, out int blueBits, out int alphaBits);
            constructionRequest.depthStencilFormat.ToDepthStencilFormat(out int depth, out int stencil);

            WGLPixelFormatDescriptor pfd = new WGLPixelFormatDescriptor()
            {
                Size = (ushort)Marshal.SizeOf<WGLPixelFormatDescriptor>(),
                Version = 1,
                Flags = WGLPixelFormatDescriptorFlags.DRAW_TO_WINDOW | WGLPixelFormatDescriptorFlags.SUPPORT_OPENGL | WGLPixelFormatDescriptorFlags.DOUBLEBUFFER,
                PixelType = WGLPixelType.RGBA,
                ColorBits = (byte)colorBits,
                RedBits = (byte)redBits, RedShift = 0, GreenBits = (byte)greenBits, GreenShift = 0, BlueBits = (byte)blueBits, BlueShift = 0, AlphaBits = (byte)alphaBits, AlphaShift = 0,
                AccumBits = 0, AccumRedBits = 0, AccumGreenBits = 0, AccumBlueBits = 0, AccumAlphaBits = 0,
                DepthBits = (byte)depth, StencilBits = (byte)stencil,
                AuxBuffers = 0,
                LayerType = PFD_LAYER_TYPES.PFD_MAIN_PLANE,
                LayerMask = 0, VisibleMask = 0, DamageMask = 0,
            };

            _viewHandle = graphicsView.ViewHandle;
            _hDC = GetDC(graphicsView.ViewHandle);
            if (_hDC == IntPtr.Zero)
                throw new WGLGraphicsDeviceCreationException(GetLastError(), "getting Device Context Handle");

            try
            {
                int currentPixelFormat = GetPixelFormat(_hDC);
                if (currentPixelFormat == 0)
                {
                    uint getPixelFormatErrno = GetLastError();
                    if (getPixelFormatErrno != 0u && getPixelFormatErrno != ERROR_INVALID_PIXEL_FORMAT) //0 means GetPixelFormat successfully queried, but there is no format set yet
                        throw new WGLGraphicsDeviceCreationException(getPixelFormatErrno, "getting Pixel Format");
                }
                bool isWindowRecreationExceptionAllowed = contextCreationRequest?.IsWindowRecreationExceptionAllowed ?? false;

                if (_createContextAttribsARB == null) //Don't queried yet, or not supported, lets create an "obsoleted" Context
                {
                    _contextHandle = CraeteContextObsoleted(_hDC, ref pfd, currentPixelFormat, isWindowRecreationExceptionAllowed); //IntPtr.Zero return value tested internally

                    if (!MakeCurrent(_hDC, _contextHandle))
                        throw new WGLGraphicsDeviceCreationException(GetLastError(), "activating created OpenGL Context");
                    LoadBindings();

                    //Check if "modern" Context creation is supported.
                    //A "Dummy" context is needed to check extensions and call extension functions for it. The previously created Context can be used for this
                    if (!_wglExtensionsQueried)
                    {
                        GetWGLExtensions(_hDC);

                        //Check if "modern" Context creation extension is supported
                        if (_createContextAttribsARB != null) //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_create_context.txt
                        {
                            //YOLO We're doing it, no turning back now
                            DeleteContext(_contextHandle);
                            _contextHandle = CreateContextModern(_hDC, ref pfd, currentPixelFormat, isWindowRecreationExceptionAllowed, graphicsView, contextVersionRequests, debugContext); //IntPtr.Zero return value tested internally

                            if (!MakeCurrent(_hDC, _contextHandle))
                                throw new WGLGraphicsDeviceCreationException(GetLastError(), "activating created OpenGL Context");
                            LoadBindings();
                        }
                        //else //No "modern" Context creation is supported, the originally created Context will be used
                    }
                    //else //No "modern" Context creation is supported, the originally created Context will be used
                }
                else //Queried previously and it's supported, lets create a "modern" Context
                {
                    _contextHandle = CreateContextModern(_hDC, ref pfd, currentPixelFormat, isWindowRecreationExceptionAllowed, graphicsView, contextVersionRequests, debugContext); //IntPtr.Zero return value tested internally

                    if (!MakeCurrent(_hDC, _contextHandle))
                        throw new WGLGraphicsDeviceCreationException(GetLastError(), "activating created OpenGL Context");
                    LoadBindings();
                }
            }
            catch
            {
                ReleaseDC(graphicsView.ViewHandle, _hDC);
                throw;
            }
        }

        ~WGLContext() => Dispose(disposing: false);

        #endregion

        #region Private Methods

        private static IntPtr CraeteContextObsoleted(IntPtr hdc, ref WGLPixelFormatDescriptor pfd, int currentPixelFormat, bool isWindowRecreationExceptionAllowed)
        {
            int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            if (pixelFormat == 0)
                throw new WGLGraphicsDeviceCreationException(GetLastError(), "choosing Pixel Format");

            SetPixelFormat(hdc, ref pfd, currentPixelFormat, pixelFormat, isWindowRecreationExceptionAllowed);

            IntPtr contextHandle = CreateContext(hdc);
            if (contextHandle == IntPtr.Zero)
                throw new WGLGraphicsDeviceCreationException(GetLastError(), "creating OpenGL Context");
            else return contextHandle;
        }
        private static IntPtr CreateContextModern(IntPtr hdc, ref WGLPixelFormatDescriptor pfd, int currentPixelFormat, bool isWindowRecreationExceptionAllowed,
            IGraphicsView graphicsView, in ReadOnlySpan<GLContextVersion> contextVersionRequests, bool debugContext)
        {
            if (_choosePixelFormatARB != null) //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_pixel_format.txt
            {
                SwapChainConstruction constructionRequest = graphicsView.SwapChainConstructionRequest;
                constructionRequest.colorFormat.ToColorFormat(out int colorBits, out int redBits, out int greenBits, out int blueBits, out int alphaBits);
                constructionRequest.depthStencilFormat.ToDepthStencilFormat(out int depth, out int stencil);

                int[] piAttribIList =
                {
                    WGL_DRAW_TO_WINDOW_ARB, 1,
                    WGL_SUPPORT_OPENGL_ARB, 1,
                    WGL_ACCELERATION_ARB, WGL_FULL_ACCELERATION_ARB,
                    WGL_DOUBLE_BUFFER_ARB, 1,
                    WGL_PIXEL_TYPE_ARB, WGL_TYPE_RGBA_ARB,
                    WGL_COLOR_BITS_ARB, colorBits,
                    WGL_RED_BITS_ARB, redBits,
                    WGL_RED_SHIFT_ARB, 0,
                    WGL_GREEN_BITS_ARB, greenBits,
                    WGL_GREEN_SHIFT_ARB, 0,
                    WGL_BLUE_BITS_ARB, blueBits,
                    WGL_BLUE_SHIFT_ARB, 0,
                    WGL_ALPHA_BITS_ARB, alphaBits,
                    WGL_ALPHA_SHIFT_ARB, 0,
                    WGL_DEPTH_BITS_ARB, depth,
                    WGL_STENCIL_BITS_ARB, stencil,
                    0, 0 // End
                };
                float[] pfAttribFList = new float[] { 0f, 0f };
                int[] formats = new int[1];
                if (!_choosePixelFormatARB(hdc, piAttribIList, pfAttribFList, 1u, formats, out uint formatCount))
                    throw new WGLGraphicsDeviceCreationException(GetLastError(), "choosing Pixel Format (ARB)");

                SetPixelFormat(hdc, ref pfd, currentPixelFormat, formats[0], isWindowRecreationExceptionAllowed);
            }
            else
            {
                int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
                if (pixelFormat == 0)
                    throw new WGLGraphicsDeviceCreationException(GetLastError(), "choosing Pixel Format");

                SetPixelFormat(hdc, ref pfd, currentPixelFormat, pixelFormat, isWindowRecreationExceptionAllowed);
            }

            //Try to Create Context with requested versions
            IntPtr contextHandle;
            if (_createContextAttribsARB != null)
            {
                foreach (GLContextVersion contextVersionRequest in contextVersionRequests)
                {
                    //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_create_context.txt
                    WGL_CONTEXT_FLAGS_ARB_BITS contextFlags = 0;
                    if (debugContext) contextFlags |= WGL_CONTEXT_FLAGS_ARB_BITS.WGL_CONTEXT_DEBUG_BIT_ARB;
                    if (contextVersionRequest.forwardCompatible) contextFlags |= WGL_CONTEXT_FLAGS_ARB_BITS.WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB;
                    int[] contextAttribList = new int[]
                    {
                        WGL_CONTEXT_MAJOR_VERSION_ARB, contextVersionRequest.major,
                        WGL_CONTEXT_MINOR_VERSION_ARB, contextVersionRequest.minor,
                        WGL_CONTEXT_FLAGS_ARB, (int)contextFlags,
                        WGL_CONTEXT_PROFILE_MASK_ARB, contextVersionRequest.coreProfile ? WGL_CONTEXT_CORE_PROFILE_BIT_ARB : WGL_CONTEXT_COMPATIBILITY_PROFILE_BIT_ARB,
                        0, 0 //End
                    };
                    contextHandle = _createContextAttribsARB!(hdc, IntPtr.Zero, contextAttribList); //Cannot be null, checked outside
                    if (contextHandle == IntPtr.Zero)
                    {
                        uint errno = GetLastError();
                        if (errno != ERROR_STATUS_DLL_NOT_FOUND)
                            throw new WGLGraphicsDeviceCreationException(errno, "creating OpenGL Context Attribs (ARB)");
                    }
                    else return contextHandle;
                }
            }

            //Just create any kind of context, version and extension checks outside will throw if not acceptable
            contextHandle = CreateContext(hdc);
            if (contextHandle == IntPtr.Zero)
                throw new WGLGraphicsDeviceCreationException(GetLastError(), "creating OpenGL Context");
            else return contextHandle;
        }

        private static void SetPixelFormat(IntPtr hdc, ref WGLPixelFormatDescriptor pfd, int currentPixelFormat, int chosenPixelFormat, bool isWindowRecreationExceptionAllowed)
        {
            if (currentPixelFormat == 0 || currentPixelFormat == chosenPixelFormat) //Pixel Format is not set yet on this Window (or the same as previously set)
            {
                //int maxPixelFormatIndex = DescribePixelFormat(hdc, chosenPixelFormat, (uint)Marshal.SizeOf(pfd), ref pfd);
                if (!SetPixelFormat(hdc, chosenPixelFormat, ref pfd))
                {
                    //Error, try again with "old" approach, maybe it was the "modern" approach
                    chosenPixelFormat = ChoosePixelFormat(hdc, ref pfd);
                    if (chosenPixelFormat == 0)
                        throw new WGLGraphicsDeviceCreationException(GetLastError(), "choosing Pixel Format");
                    if (SetPixelFormat(hdc, chosenPixelFormat, ref pfd))
                        _choosePixelFormatARB = null; //Something is wrong with "modern" Context creation's ChoosePixelFormat, disable it
                    else throw new WGLGraphicsDeviceCreationException(GetLastError(), "setting Pixel Format");
                }
            }
            else //Different Pixel Format is requested
            {
                if (isWindowRecreationExceptionAllowed)
                    throw new WGLWindowRecreatonRequestException("setting Pixel Format");
                else
                {
                    if (!SetPixelFormat(hdc, currentPixelFormat, ref pfd)) //Must use the current Pixel Format, the SwapChain has to do fallback
                        throw new WGLGraphicsDeviceCreationException(GetLastError(), "setting Pixel Format");
                }
            }
        }

        private static void GetWGLExtensions(IntPtr hdc)
        {
            IntPtr getExtensionsStringARBPtr = GetFunctionPointer("wglGetExtensionsStringARB"); //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_extensions_string.txt
            if (getExtensionsStringARBPtr != IntPtr.Zero)
            {
                wglGetExtensionsStringARB getExtensionsStringARB = Marshal.GetDelegateForFunctionPointer<wglGetExtensionsStringARB>(getExtensionsStringARBPtr);
                string[] extensions = getExtensionsStringARB(hdc).Split(' '); //TODO: crash when called second time???

                if (extensions.Contains("WGL_ARB_create_context")) //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_create_context.txt
                    _createContextAttribsARB = GetDelegate<wglCreateContextAttribsARB>();

                if (extensions.Contains("WGL_ARB_pixel_format")) //https://registry.khronos.org/OpenGL/extensions/ARB/WGL_ARB_pixel_format.txt
                {
                    _getPixelFormatAttribivARB = GetDelegate<wglGetPixelFormatAttribivARB>();
                    _getPixelFormatAttribfvARB = GetDelegate<wglGetPixelFormatAttribfvARB>();
                    _choosePixelFormatARB = GetDelegate<wglChoosePixelFormatARB>();
                }
            }
            _wglExtensionsQueried = true;
        }

        #endregion

        #region Protected Methods

        protected abstract void LoadBindings();

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                if (_contextHandle != IntPtr.Zero)
                    if (!DeleteContext(_contextHandle))
                        Debug.WriteLine($"Failed to Delete WGL Context with error code: {GetLastError()}");

                if (_hDC != IntPtr.Zero)
                    ReleaseDC(_viewHandle, _hDC);

                if (_ownWindow && _viewHandle != IntPtr.Zero)
                    DestroyWindow(_viewHandle);

                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Internal Methods

        internal static string GetErrorName(uint errno)
        {
            StringBuilder lpBuffer = new StringBuilder(256);
            if (FormatMessage(
                FORMAT_MESSAGE.ALLOCATE_BUFFER | FORMAT_MESSAGE.FROM_SYSTEM | FORMAT_MESSAGE.IGNORE_INSERTS,
                IntPtr.Zero, (int)errno, 0u,
                out lpBuffer, lpBuffer.Capacity, IntPtr.Zero) != 0)
                return lpBuffer.ToString();
            else return "Unknown Error";
        }

        #endregion

        #region Public Methods

        public void Bind() => MakeCurrent(_hDC, _contextHandle);
        public void UnBind() => MakeCurrent(_hDC, IntPtr.Zero);

        public void SwapBuffers() => SwapBuffers(_hDC);

        public IntPtr GetProcAddress(string name) => GetFunctionPointer(name); //May need to have an instance method for IBindingContext in subclasses

        public static IntPtr GetFunctionPointer(string name)
        {
            IntPtr ptr = wglGetProcAddress(name);

            if (ptr == IntPtr.Zero ||
                ptr == new IntPtr(0x1) || ptr == new IntPtr(0x2) || ptr == new IntPtr(0x3) ||
                ptr == new IntPtr(-1))
            {
                IntPtr module = LoadLibraryA("opengl32.dll");
                ptr = GetProcAddress(module, name);
            }

            return ptr;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetDelegate<T>() where T : Delegate
            => GetDelegate<T>(typeof(T).Name);
        public static T GetDelegate<T>(string name) where T : Delegate
        {
            IntPtr functionPtr = GetFunctionPointer(name);
            if (functionPtr != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
            else throw new EntryPointNotFoundException($"wglGetProcAddress and GetProcAddress has not found function {name}!");
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

    public class WGLGraphicsDeviceCreationException : GLGraphicsDeviceCreationException
    {

        public uint WGLErrorCode { get; }

        internal WGLGraphicsDeviceCreationException(uint wglErrorCode) : base($"Failed to Create WGL Context with error code: {wglErrorCode} ({WGLContext.GetErrorName(wglErrorCode)})")
        {
            WGLErrorCode = wglErrorCode;
        }
        internal WGLGraphicsDeviceCreationException(uint wglErrorCode, string creationStepName) : base($"Failed to Create WGL Context ({creationStepName}) with error code: {wglErrorCode} ({WGLContext.GetErrorName(wglErrorCode)})")
        {
            WGLErrorCode = wglErrorCode;
        }

    }

    public class WGLWindowRecreatonRequestException : WGLGraphicsDeviceCreationException
    {

        internal WGLWindowRecreatonRequestException() : base(WGLContext.ERROR_INVALID_PIXEL_FORMAT) { }
        internal WGLWindowRecreatonRequestException(string creationStepName) : base(WGLContext.ERROR_INVALID_PIXEL_FORMAT, creationStepName) { }

    }

}
