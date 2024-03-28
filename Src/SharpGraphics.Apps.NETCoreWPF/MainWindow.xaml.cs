using SharpGraphics.Factory;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
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
using static SharpGraphics.Loggers.FrameStatLogger;

namespace SharpGraphics.Apps.NETCoreWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Fields

        private GraphicsAPI? _graphicsAPI = default(GraphicsAPI?);

        private GraphicsManagement? _graphicsManagement;
        private IGraphicsView? _view;
        private GraphicsApplicationBase? _graphicsApplication;
        private GraphicsDeviceRequest? _deviceRequest = default(GraphicsDeviceRequest?);

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            GraphicsFactory.DebugLevel = DebugLevel.Important;
#else
            GraphicsFactory.DebugLevel = DebugLevel.None;
#endif
            SetGraphicsAPI(GraphicsAPI.OpenGLCore);
        }

        #endregion

        #region Control Event Handlers

        private void MenuApplication_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && _graphicsManagement != null && _view != null)
            {
                GraphicsApplicationBase? newGraphicsApp = null;

                switch (menuItem.Tag)
                {
                    case "HelloTriangle":
                        newGraphicsApp = new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, _view, HelloTriangle.HelloTriangleApp.Factory);
                        break;

                    case "VertexAttributes":
                        newGraphicsApp = new VertexAttributes.VertexAttributesApp(_graphicsManagement, _view);
                        break;

                    case "QuadPlayground":
                        newGraphicsApp = new QuadPlayground.QuadPlaygroundApp(_graphicsManagement, _view);
                        break;

                    case "PushValues":
                        newGraphicsApp = new PushValues.PushValuesApp(_graphicsManagement, _view);
                        break;

                    case "Normals":
                        newGraphicsApp = new GraphicsApplication<Normals.NormalsApp>(_graphicsManagement, _view, Normals.NormalsApp.Factory);
                        break;

                    case "NormalsThreads":
                        newGraphicsApp = new GraphicsApplication<NormalsThreads.NormalsThreadsApp>(_graphicsManagement, _view, NormalsThreads.NormalsThreadsApp.Factory);
                        break;

                    case "PostProcess":
                        newGraphicsApp = new GraphicsApplication<PostProcess.PostProcessApp>(_graphicsManagement, _view, PostProcess.PostProcessApp.Factory);
                        break;

                    case "SimpleRaytrace":
                        newGraphicsApp = new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, _view);
                        break;

                    case "Models":
                        newGraphicsApp = new GraphicsApplication<Models.ModelsApp>(_graphicsManagement, _view, Models.ModelsApp.Factory);
                        break;

                    case "Deferred":
                        newGraphicsApp = new GraphicsApplication<Deferred.DeferredApp>(_graphicsManagement, _view, Deferred.DeferredApp.Factory);
                        break;
                }

                if (newGraphicsApp != null)
                {
                    if (_graphicsApplication != null)
                        _graphicsApplication.Dispose();
                    _graphicsApplication = newGraphicsApp;

                    if (_loggingChecker.IsChecked)
                    {
                        AdditionalLogOptions logOptions = AdditionalLogOptions.None;
                        if (_logGCChecker.IsChecked) logOptions |= AdditionalLogOptions.GC;
                        if (_logProcChecker.IsChecked) logOptions |= AdditionalLogOptions.Process;

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
                        MessageBox.Show(ex.Message, $"Failed to start application {_graphicsApplication}");
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
        private void MenuGraphicsDevice_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int deviceIndex && _graphicsManagement != null && _view != null &&
                deviceIndex >= 0 && deviceIndex < _graphicsManagement.AvailableDevices.Length)
            {
                uint[] commandGroupIndices = _graphicsManagement.AvailableDevices[deviceIndex].GetCommandProcessorGroupIndicesSupportingView(_view);
                if (commandGroupIndices.Length > 0)
                {
                    _deviceRequest = new GraphicsDeviceRequest((uint)deviceIndex, _view, new PresentCommandProcessorRequest(commandGroupIndices[0]));
                    if (_graphicsApplication != null)
                        _graphicsApplication.Dispose();
                }
                else MessageBox.Show("Selected Graphics Device does not support Presentation and Graphics.");
            }
        }
        private void MenuPresentMode_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
                switch (menuItem.Tag)
                {
                    case "Immediate": _graphicsView.PresentMode = PresentMode.Immediate; break;
                    case "VSyncDoubleBuffer": _graphicsView.PresentMode = PresentMode.VSyncDoubleBuffer; break;
                    case "VSyncTripleBuffer": _graphicsView.PresentMode = PresentMode.VSyncTripleBuffer; break;
                    case "AdaptiveDoubleBuffer": _graphicsView.PresentMode = PresentMode.AdaptiveDoubleBuffer; break;
                }
        }

        private void MenuFullscreen_Click(object? sender, RoutedEventArgs e)
        {
            _menuBar.Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            Topmost = true;
            WindowState = WindowState.Maximized;
        }
        private void Window_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _menuBar.Visibility = Visibility.Visible;
                Topmost = false;
                WindowStyle = WindowStyle.ThreeDBorderWindow;
            }
        }

        private void Resize_Click(object? sender, RoutedEventArgs e)
        {
            ResolutionWindow resolutionWindow = new ResolutionWindow();
            if (resolutionWindow.ShowDialog() ?? false)
            {
                Width = resolutionWindow.ResolutionWidth;
                Height = resolutionWindow.ResolutionHeight + _menuBar.ActualHeight;
            }
        }

        private void _graphicsView_ViewInitialized(object? sender, ViewInitializedEventArgs e)
        {
            if (sender is IGraphicsView view)
            {
                _view = view;

                if (_graphicsManagement != null)
                {
                    _graphicsApplication = new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, _view, HelloTriangle.HelloTriangleApp.Factory);
                    //_graphicsApplication = new VertexAttributes.VertexAttributesApp(_graphicsManagement, view);
                    //_graphicsApplication = new QuadPlayground.QuadPlaygroundApp(_graphicsManagement, _view);
                    //_graphicsApplication = new PushValues.PushValuesApp(_graphicsManagement, view);
                    //_graphicsApplication = new Normals.NormalsApp(_graphicsManagement, view);
                    //_graphicsApplication = new GraphicsApplication<PostProcess.PostProcessApp>(_graphicsManagement, _view, PostProcess.PostProcessApp.Factory);
                    //_graphicsApplication = new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, view);
                    //_graphicsApplication = new GraphicsApplication<Models.ModelsApp>(_graphicsManagement, _view, Models.ModelsApp.Factory);

                    try
                    {
                        _graphicsApplication.InitializeAndStart();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, $"Failed to start application {_graphicsApplication}");
                        _graphicsApplication.Dispose();
                        _graphicsApplication = null;
                    }
                }
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_graphicsApplication != null)
                _graphicsApplication.Dispose();
            if (_graphicsManagement != null)
                _graphicsManagement.Dispose();
        }

        #endregion

        #region Private Methods

        [MemberNotNullWhen(returnValue: true, "_graphicsManagement")]
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
                    _graphicsManagement = GraphicsFactory.CreateForGraphics(graphicsAPI, OperatingSystem.Windows);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, $"Failed to initialize {graphicsAPI}");
                    return false;
                }
                _graphicsAPI = graphicsAPI;


                _deviceRequest = default(GraphicsDeviceRequest?);

                foreach (object menuItemObj in _graphicsDeviceMenu.Items)
                    if (menuItemObj is MenuItem menuItem)
                        menuItem.Click -= MenuGraphicsDevice_Click;
                _graphicsDeviceMenu.Items.Clear();

                int deviceIndex = 0;
                foreach (GraphicsDeviceInfo graphicsDeviceInfo in _graphicsManagement.AvailableDevices)
                {
                    MenuItem graphicsDeviceItem = new MenuItem()
                    {
                        Header = graphicsDeviceInfo.Name,
                        Tag = deviceIndex++,
                    };
                    graphicsDeviceItem.Click += MenuGraphicsDevice_Click;
                    _graphicsDeviceMenu.Items.Add(graphicsDeviceItem);
                }

                return true;
            }
            else return false;
        }

        #endregion

    }
}
