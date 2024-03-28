using System;

using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.Wearable.Activity;
using SharpGraphics.GraphicsViews.XamAndroid;
using SharpGraphics.GraphicsViews;
using SharpGraphics.Factory;

namespace SharpGraphics.Apps.XamWearOS
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : WearableActivity
    {

        #region Fields

        private bool _isDisposed = false;

        private XamAndroidGraphicsView _graphicsView;

        private GraphicsManagement _graphicsManagement;
        private GraphicsApplicationBase _graphicsApplication;

        #endregion

        #region Private Methods

        private void _graphicsView_ViewInitialized(object sender, ViewInitializedEventArgs e)
        {
            if (_graphicsManagement == null)
            {
                _graphicsManagement = GraphicsFactory.CreateForGraphics(GraphicsAPI.Vulkan, OperatingSystem.Android);
                //_graphicsManagement = GraphicsFactory.CreateForGraphics(GraphicsAPI.OpenGLES30, OperatingSystem.Android);
            }

            if (sender is IGraphicsView view)
            {
                //_graphicsApplication = new HelloTriangle.HelloTriangleApp(_graphicsManagement, view);
                //_graphicsApplication = new VertexAttributes.VertexAttributesApp(_graphicsManagement, view);
                //_graphicsApplication = new PushValues.PushValuesApp(_graphicsManagement, view);
                //_graphicsApplication = new Normals.NormalsApp(_graphicsManagement, view);
                _graphicsApplication = new SimpleRaytrace.SimpleRaytraceApp(_graphicsManagement, view);

                _graphicsApplication.InitializeAndStart();
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);

            _graphicsView = FindViewById<XamAndroidGraphicsView>(Resource.Id.graphicsView);
            _graphicsView.ViewInitialized += _graphicsView_ViewInitialized;

            SetAmbientEnabled();
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

    }
}


