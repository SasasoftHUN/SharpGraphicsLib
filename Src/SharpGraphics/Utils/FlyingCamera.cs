using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Utils
{
    public class FlyingCamera : CameraBase
    {

        #region Constructors

        public FlyingCamera(Vector3 eye, Vector3 at, Vector3 up, float fov, float aspect, float nearClipDistance, float farClipDistance) :
            base(eye, at, up, fov, aspect, nearClipDistance, farClipDistance)
        {

        }

        #endregion

        #region Protected Methods

        protected override bool UpdateInternal(float deltaTime)
        {
            Vector3 difference = (_moveForward * _forward + _moveRight * _right) * _speed * deltaTime;

            if (Vector3.Zero != difference)
            {
                _eye += difference;
                _at += difference;
                return true;
            }
            else return false;
        }

        protected override void UpdateUVInternal(float uDifference, float vDifference)
        {
            _u += uDifference;
            _v = Math.Min(3.1f, Math.Max(0.1f, _v + vDifference));

            _at = _eye + _eyeToAtDistance * ToDescartes(_u, _v);

            _forward = Vector3.Normalize(_at - _eye);
            _right = Vector3.Normalize(Vector3.Cross(_forward, _up));
        }

        #endregion

    }
}
