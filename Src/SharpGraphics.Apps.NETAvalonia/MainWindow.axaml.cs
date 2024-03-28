using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SharpGraphics.Factory;
using SharpGraphics.GraphicsViews;
using SharpGraphics.GraphicsViews.NETAvalonia;
using System;
using System.Linq;
using static SharpGraphics.Loggers.FrameStatLogger;

namespace SharpGraphics.Apps.NETAvalonia
{
    public partial class MainWindow : Window
    {

        #region Fields

        private OperatingSystem _operatingSystem = OperatingSystem.Unknown;
        private GraphicsAPI? _graphicsAPI = default(GraphicsAPI?);

        private bool _logging = false;
        private bool _loggingGC = false;
        private bool _loggingProcess = false;

        private AvaloniaGraphicsView? _graphicsView;
        private Menu? _menuBar;

        private CheckBox? _loggingChecker;
        private CheckBox? _logGCChecker;
        private CheckBox? _logProcChecker;

        private GraphicsManagement? _graphicsManagement;
        private GraphicsApplicationBase? _graphicsApplication;
        private GraphicsDeviceRequest? _deviceRequest = default(GraphicsDeviceRequest?);

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
            GraphicsFactory.DebugLevel = DebugLevel.Important;
#else
            GraphicsFactory.DebugLevel = DebugLevel.None;
#endif

            Closing += MainWindow_Closing;
        }

        #endregion

        #region Control Event Handlers

        private void MenuApplication_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && _graphicsManagement != null && _graphicsView != null)
            {
                GraphicsApplicationBase? newGraphicsApp = null;
                switch (menuItem.Tag)
                {
                    case "HelloTriangle":
                        newGraphicsApp = new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, _graphicsView, HelloTriangle.HelloTriangleApp.Factory);
                        break;

                    case "VertexAttributes":
                        newGraphicsApp = new VertexAttributes.VertexAttributesApp(_graphicsManagement, _graphicsView);
                        break;

                    case "QuadPlayground":
                        newGraphicsApp = new QuadPlayground.QuadPlaygroundApp(_graphicsManagement, _graphicsView);
                        break;

                    case "PushValues":
                        newGraphicsApp = new PushValues.PushValuesApp(_graphicsManagement, _graphicsView);
                        break;

                    case "Normals":
                        newGraphicsApp = new GraphicsApplication<Normals.NormalsApp>(_graphicsManagement, _graphicsView, Normals.NormalsApp.Factory);
                        break;

                    case "NormalsThreads":
                        newGraphicsApp = new GraphicsApplication<NormalsThreads.NormalsThreadsApp>(_graphicsManagement, _graphicsView, NormalsThreads.NormalsThreadsApp.Factory);
                        break;

                    case "PostProcess":
                        newGraphicsApp = new GraphicsApplication<PostProcess.PostProcessApp>(_graphicsManagement, _graphicsView, PostProcess.PostProcessApp.Factory);
                        break;

                    case "SimpleRaytrace":
                        newGraphicsApp = new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, _graphicsView);
                        break;

                    case "Models":
                        newGraphicsApp = new GraphicsApplication<Models.ModelsApp>(_graphicsManagement, _graphicsView, Models.ModelsApp.Factory);
                        break;

                    case "Deferred":
                        newGraphicsApp = new GraphicsApplication<Deferred.DeferredApp>(_graphicsManagement, _graphicsView, Deferred.DeferredApp.Factory);
                        break;
                }

                if (newGraphicsApp != null)
                {
                    if (_graphicsApplication != null)
                        _graphicsApplication.Dispose();
                    _graphicsApplication = newGraphicsApp;

                    if (_logging)
                    {
                        AdditionalLogOptions logOptions = AdditionalLogOptions.None;
                        if (_loggingGC) logOptions |= AdditionalLogOptions.GC;
                        if (_loggingProcess) logOptions |= AdditionalLogOptions.Process;

                        _graphicsApplication.Logger = new Loggers.FrameStatLogger(new Loggers.FileStreamLogWriter(), logOptions)
                        {
                            LogName = $"{menuItem.Tag}-{_graphicsAPI}-{_graphicsView.PresentMode}",
                        };
                    }

                    try
                    {
                        if (_deviceRequest.HasValue)
                            _graphicsApplication.InitializeAndStart(_deviceRequest.Value);
                        else _graphicsApplication.InitializeAndStart();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Failed to start application {_graphicsApplication}", ex.Message).Show();
                        _graphicsApplication.Dispose();
                        _graphicsApplication = null;
                    }
                }
            }
        }
        private void MenuGraphicsAPI_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
                switch (menuItem.Tag)
                {
                    case "Vulkan": SetGraphicsAPI(GraphicsAPI.Vulkan); break;
                    case "GLCore": SetGraphicsAPI(GraphicsAPI.OpenGLCore); break;
                    case "GLES30": SetGraphicsAPI(GraphicsAPI.OpenGLES30); break;
                }
        }
        private void MenuPresentMode_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && _graphicsView != null)
                switch (menuItem.Tag)
                {
                    case "Immediate": _graphicsView.PresentMode = PresentMode.Immediate; break;
                    case "VSyncDoubleBuffer": _graphicsView.PresentMode = PresentMode.VSyncDoubleBuffer; break;
                    case "VSyncTripleBuffer": _graphicsView.PresentMode = PresentMode.VSyncTripleBuffer; break;
                    case "AdaptiveDoubleBuffer": _graphicsView.PresentMode = PresentMode.AdaptiveDoubleBuffer; break;
                }
        }

        private void MenuLogging_Click(object? sender, RoutedEventArgs e)
        {
            _logging = !_logging;
            if (_loggingChecker != null)
                _loggingChecker.IsChecked = _logging;
        }
        private void MenuLoggingGC_Click(object? sender, RoutedEventArgs e)
        {
            _loggingGC = !_loggingGC;
            if (_logGCChecker != null)
                _logGCChecker.IsChecked = _loggingGC;
        }
        private void MenuLoggingProcess_Click(object? sender, RoutedEventArgs e)
        {
            _loggingProcess = !_loggingProcess;
            if (_logProcChecker != null)
                _logProcChecker.IsChecked = _loggingProcess;
        }

        private void MenuFullscreen_Click(object? sender, RoutedEventArgs e)
        {
            if (_menuBar != null)
            {
                _menuBar.IsVisible = false;
                WindowState = WindowState.FullScreen;
                Topmost = true;
            }
        }
        private void Window_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_menuBar != null)
                {
                    _menuBar.IsVisible = true;
                    Topmost = false;
                    WindowState = WindowState.Maximized;
                }
            }
        }

        private async void Resize_Click(object? sender, RoutedEventArgs e)
        {
            ResolutionWindow resolutionWindow = new ResolutionWindow();
            await resolutionWindow.ShowDialog(this);
            if (resolutionWindow.IsAccepted)
            {
                Width = resolutionWindow.ResolutionWidth;
                Height = resolutionWindow.ResolutionHeight + _menuBar?.Height ?? 0;
                HandleResized(new Size(Width, Height), PlatformResizeReason.Application);
            }
        }

        private void _graphicsView_Initialized(object? sender, ViewInitializedEventArgs e)
        {
            if (sender != null && sender is IGraphicsView view && view != null && _graphicsManagement != null)
            {
                _graphicsApplication = new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, view, HelloTriangle.HelloTriangleApp.Factory);
                //_graphicsApplication = new VertexAttributes.VertexAttributesApp(_graphicsManagement, view);
                //_graphicsApplication = new PushValues.PushValuesApp(_graphicsManagement, view);
                //_graphicsApplication = new GraphicsApplication<Normals.NormalsApp>(_graphicsManagement, view, Normals.NormalsApp.Factory);
                //_graphicsApplication = new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, view);

                try
                {
                    _graphicsApplication.InitializeAndStart();
                }
                catch (Exception ex)
                {
                    MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Failed to start application {_graphicsApplication}", ex.Message).Show();
                    _graphicsApplication.Dispose();
                    _graphicsApplication = null;
                }
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_graphicsApplication != null)
                _graphicsApplication.Dispose();
            if (_graphicsManagement != null)
                _graphicsManagement.Dispose();
        }

        #endregion

        #region Private Methods

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _graphicsView = this.FindControl<AvaloniaGraphicsView>("_graphicsView");
            _menuBar = this.FindControl<Menu>("_menuBar");

            _loggingChecker = this.FindControl<CheckBox>("_loggingChecker");
            _logGCChecker = this.FindControl<CheckBox>("_logGCChecker");
            _logProcChecker = this.FindControl<CheckBox>("_logProcChecker");

            _operatingSystem = _graphicsView.OperatingSystem.ToOperatingSystem();
            
            SetGraphicsAPI(GraphicsAPI.Vulkan);
        }

        private bool SetGraphicsAPI(GraphicsAPI graphicsAPI)
        {
            if (_graphicsAPI != graphicsAPI)
            {
                if (_graphicsApplication != null)
                    _graphicsApplication.Dispose();
                _graphicsApplication = null;
                if (_graphicsManagement != null)
                    _graphicsManagement.Dispose();
                _graphicsManagement = null;
                _graphicsAPI = default(GraphicsAPI?);

                try
                {
                    _graphicsManagement = GraphicsFactory.CreateForGraphics(graphicsAPI, _operatingSystem);
                }
                catch (Exception e)
                {
                    MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Failed to initialize {graphicsAPI}", e.Message).Show();
                    return false;
                }
                _graphicsAPI = graphicsAPI;

                return true;
            }
            else return false;
        }

        #endregion

    }
}
