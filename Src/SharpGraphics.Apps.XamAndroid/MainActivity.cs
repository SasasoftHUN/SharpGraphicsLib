using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using SharpGraphics.GraphicsViews;
using SharpGraphics.GraphicsViews.XamAndroid;
using SharpGraphics.Factory;
using AndroidX.Core.View;
using Android.Content;
using System.IO;

namespace SharpGraphics.Apps.XamAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        #region Fields

        public const int CREATE_LOG_FILE = 1;

        private bool _isDisposed = false;
        private bool _autoStartApp = true;

        private GraphicsAPI? _graphicsAPI = default(GraphicsAPI?);
        private Android.Net.Uri _logFileUri = null;

        private XamAndroidGraphicsView _graphicsView;

        private GraphicsManagement _graphicsManagement;
        private GraphicsApplicationBase _graphicsApplication;

        #endregion

        #region Private Methods

        private void SetGraphicsAPI(GraphicsAPI graphicsAPI)
        {
            if (_graphicsAPI != graphicsAPI)
            {
                if (_graphicsApplication != null)
                    _graphicsApplication.Dispose();
                if (_graphicsManagement != null)
                    _graphicsManagement.Dispose();

                _graphicsAPI = graphicsAPI;
#if DEBUG
                GraphicsFactory.DebugLevel = DebugLevel.Important;
#else
                GraphicsFactory.DebugLevel = DebugLevel.None;
#endif
                _graphicsManagement = GraphicsFactory.CreateForGraphics(graphicsAPI, OperatingSystem.Android);
            }
        }
        private void StartApp(GraphicsApplicationBase app, string appName)
        {
            if (_graphicsApplication != null)
                _graphicsApplication.Dispose();

            _graphicsApplication = app;

            if (_logFileUri != null)
            {
                try
                {
                    Stream logStream = ContentResolver.OpenOutputStream(_logFileUri, "w");
                    if (logStream != null)
                        _graphicsApplication.Logger = new Loggers.FrameTimeLogger(new Loggers.StreamLogWriter(logStream), 40000)
                        {
                            LogName = $"{appName}-{_graphicsAPI}-{(_graphicsView.VSyncRequest ? "VSync" : "Immediate")}",
                        };
                }
                catch (Exception ex)
                {
                    Android.Widget.Toast.MakeText(this, $"Logging: {ex.Message}", Android.Widget.ToastLength.Long).Show();
                }
                
            }
            _logFileUri = null;

            try
            {
                _graphicsApplication.InitializeAndStart();
            }
            catch (Exception ex)
            {
                Android.Widget.Toast.MakeText(this, ex.Message, Android.Widget.ToastLength.Long).Show();
                _graphicsApplication.Dispose();
                _graphicsApplication = null;
            }
        }
        private void SetupLogging()
        {
            if (_graphicsApplication != null)
                _graphicsApplication.Dispose();

            StartActivityForResult(new Intent(Intent.ActionCreateDocument)
                .AddCategory(Intent.CategoryOpenable)
                .SetType("text/csv")
                .PutExtra(Intent.ExtraTitle, "Log.csv"), CREATE_LOG_FILE);
        }

        private void _graphicsView_ViewInitialized(object sender, ViewInitializedEventArgs e)
        {
            if (_autoStartApp && sender is IGraphicsView view)
            {
                StartApp(new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, view, HelloTriangle.HelloTriangleApp.Factory), "HelloTriangle");
                //StartApp(new VertexAttributes.VertexAttributesApp(_graphicsManagement, view), "VertexAttributes");
                //StartApp(new PushValues.PushValuesApp(_graphicsManagement, view), "PushValues");
                //StartApp(new GraphicsApplication<Normals.NormalsApp>(_graphicsManagement, view, Normals.NormalsApp.Factory), "Normals");
                //StartApp(new GraphicsApplication<NormalsThreads.NormalsThreadsApp>(_graphicsManagement, view, NormalsThreads.NormalsThreadsApp.Factory), "NormalsThreads");
                //StartApp(new GraphicsApplication<PostProcess.PostProcessApp>(_graphicsManagement, view, PostProcess.PostProcessApp.Factory), "PostProcess");
                //StartApp(new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, view), "SimpleRaytrace");
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SetGraphicsAPI(GraphicsAPI.Vulkan);

            _graphicsView = FindViewById<XamAndroidGraphicsView>(Resource.Id.graphicsView);
            _graphicsView.ViewInitialized += _graphicsView_ViewInitialized;
            //Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            _autoStartApp = false;
            if (requestCode == CREATE_LOG_FILE && resultCode == Result.Ok && data != null && data.Data != null)
                _logFileUri = data.Data;
            else _logFileUri = null;
            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (_graphicsApplication != null)
                    _graphicsApplication.Dispose();
                if (_graphicsManagement != null)
                    _graphicsManagement.Dispose();

                _isDisposed = true;
            }
            
            base.Dispose(disposing);
        }

        #endregion

        #region Public Methods

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            MenuCompat.SetGroupDividerEnabled(menu, true);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            switch (id)
            {
                case Resource.Id.action_vulkan: SetGraphicsAPI(GraphicsAPI.Vulkan); break;
                case Resource.Id.action_gles30: SetGraphicsAPI(GraphicsAPI.OpenGLES30); break;

                case Resource.Id.action_app_hellotriangle:
                    StartApp(new GraphicsApplication<HelloTriangle.HelloTriangleApp>(_graphicsManagement, _graphicsView, HelloTriangle.HelloTriangleApp.Factory), "HelloTriangle");
                    break;
                case Resource.Id.action_app_vertexattributes:
                    StartApp(new VertexAttributes.VertexAttributesApp(_graphicsManagement, _graphicsView), "VertexAttributes");
                    break;
                case Resource.Id.action_app_pushvalues:
                    StartApp(new PushValues.PushValuesApp(_graphicsManagement, _graphicsView), "PushValues");
                    break;
                case Resource.Id.action_app_normals:
                    StartApp(new GraphicsApplication<Normals.NormalsApp>(_graphicsManagement, _graphicsView, Normals.NormalsApp.Factory), "Normals");
                    break;
                case Resource.Id.action_app_normalsthreads:
                    StartApp(new GraphicsApplication<NormalsThreads.NormalsThreadsApp>(_graphicsManagement, _graphicsView, NormalsThreads.NormalsThreadsApp.Factory), "NormalsThreads");
                    break;
                case Resource.Id.action_app_postprocess:
                    StartApp(new GraphicsApplication<PostProcess.PostProcessApp>(_graphicsManagement, _graphicsView, PostProcess.PostProcessApp.Factory), "PostProcess");
                    break;
                case Resource.Id.action_app_simpleraytrace:
                    StartApp(new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, _graphicsView), "SimpleRaytrace");
                    break;
                case Resource.Id.action_app_models:
                    StartApp(new GraphicsApplication<Models.ModelsApp>(_graphicsManagement, _graphicsView, Models.ModelsApp.Factory), "Models");
                    break;
                case Resource.Id.action_app_deferred:
                    StartApp(new GraphicsApplication<Deferred.DeferredApp>(_graphicsManagement, _graphicsView, Deferred.DeferredApp.Factory), "Deferred");
                    break;

                case Resource.Id.action_logging: SetupLogging(); break;
                case Resource.Id.action_vsync:
                    _graphicsView.VSyncRequest = !_graphicsView.VSyncRequest;
                    Android.Widget.Toast.MakeText(this, $"VSync {_graphicsView.VSyncRequest}", Android.Widget.ToastLength.Short).Show();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        #endregion

    }
}
