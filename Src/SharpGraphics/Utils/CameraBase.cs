using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Utils
{
    public abstract class CameraBase
    {

        #region Fields

        private IUserInputSource? _userInput;

        protected float _speed = 16f;
        protected bool _fast;
        protected float _moveForward;
        protected float _moveRight;
        protected bool _mouseButtonHold;
        protected float _mouseSensibility = 0.005f;
        protected float _touchSensibility = 0.0025f;
        protected int _touchCount = 0;

        protected bool _uvChanged = false;

        protected Vector3 _eye;
        protected Vector3 _at;
        protected Vector3 _up;
        protected Vector3 _forward;
        protected Vector3 _right;

        protected float _u;
        protected float _v;

        protected float _eyeToAtDistance;

        protected float _fov;
        protected float _nearClipDistance;
        protected float _farClipDistance;

        protected Matrix4x4 _viewMatrix;
        protected Matrix4x4 _projectionMatrix;
        protected Matrix4x4 _viewProjectionMatrix;

        protected MouseButton _lookButton = MouseButton.Right;

        #endregion

        #region Properties

        public float Speed { get => _speed; set => _speed = value; }
        public float MouseSensitivity { get => _mouseSensibility; set => _mouseSensibility = value; }
        public float TouchSensitivity { get => _touchSensibility; set => _touchSensibility = value; }

        public Vector3 Eye
        {
            get => _eye;
            set => SetView(value, _at, _up);
        }
        public Vector3 At
        {
            get => _at;
            set => SetView(_eye, value, _up);
        }
        public Vector3 Up
        {
            get => _up;
            set => SetView(_eye, _at, value);
        }

        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjectionMatrix => _projectionMatrix;
        public Matrix4x4 ViewProjectionMatrix => _viewProjectionMatrix;

        public MouseButton LookButton
        {
            get => _lookButton;
            set => _lookButton = value;
        }

        public IUserInputSource? UserInput
        {
            get => _userInput;
            set
            {
                if (_userInput != value)
                {
                    if (_userInput != null)
                        _userInput.UserInput -= _userInput_UserInput;

                    _userInput = value;

                    if (_userInput != null)
                        _userInput.UserInput += _userInput_UserInput;
                }
            }
        }

        #endregion

        #region Constructors

        protected CameraBase(Vector3 eye, Vector3 at, Vector3 up, float fov, float aspect, float nearClipDistance, float farClipDistance)
        {
            SetView(eye, at, up);
            SetProjection(fov, aspect, nearClipDistance, farClipDistance);
        }

        #endregion

        #region Private Methods

        private void UpdateUV(float uDifference, float vDifference)
        {
            _uvChanged = 0f != uDifference || 0f != vDifference;
            UpdateUVInternal(uDifference, vDifference);
        }


        private void _userInput_UserInput(object? sender, UserInputEventArgs e)
        {
            switch (e)
            {
                case KeyboardEventArgs keyboardEvent:
                    if (keyboardEvent.IsPressed)
                        KeyboardDown(keyboardEvent.Key);
                    else KeyboardUp(keyboardEvent.Key);
                    break;

                case MouseButtonEventArgs mouseButtonEvent:
                    if (mouseButtonEvent.IsPressed)
                        MouseButtonDown(mouseButtonEvent.Button);
                    else MouseButtonUp(mouseButtonEvent.Button);
                    break;

                case MouseMoveEventArgs mouseMoveEvent: MouseMove(mouseMoveEvent.Movement.Difference); break;

                case TouchDetectEventArgs touchDetectEvent:
                    if (touchDetectEvent.IsPressed)
                        ++_touchCount;
                    else
                    {
                        --_touchCount;
                        _moveForward = 0f;
                        _moveRight = 0f;
                    }
                    break;

                case TouchMoveEventArgs touchMoveEvent: TouchMove(touchMoveEvent); break;
            }
        }

        #endregion

        #region Protected Methods

        protected Vector3 ToDescartes(float u, float v)
            => new Vector3(
                (float)(Math.Cos(u) * Math.Sin(v)),
                (float)Math.Cos(v),
                (float)(Math.Sin(u) * Math.Sin(v))
                );

        protected abstract bool UpdateInternal(float deltaTime);
        protected abstract void UpdateUVInternal(float uDifference, float vDifference);

        #endregion

        #region Public Methods

        public virtual void SetView(in Vector3 eye, in Vector3 at, in Vector3 up)
        {
            _eye = eye;
            _at = at;
            _up = up;

            _forward = Vector3.Normalize(_at - _eye);
            _right = Vector3.Normalize(Vector3.Cross(_forward, _up));

            _eyeToAtDistance = Vector3.Distance(_eye, _at);

            _u = (float)Math.Atan2(_forward.Z, _forward.X);
            _v = (float)Math.Acos(_forward.Y);

            _viewMatrix = Matrix4x4.CreateLookAt(_eye, _at, _up);
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        }
        public virtual void SetProjection(float fov, float aspect, float nearClipDistance, float farClipDistance)
        {
            _fov = fov;
            _nearClipDistance = nearClipDistance;
            _farClipDistance = farClipDistance;

            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, nearClipDistance, farClipDistance);
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        }

        public void Update(float deltaTime)
        {
            if (UpdateInternal(deltaTime) || _uvChanged)
            {
                _viewMatrix = Matrix4x4.CreateLookAt(_eye, _at, _up);
                _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
            }
            _uvChanged = false;
        }

        public virtual void Resize(Vector2Int size)
            => SetProjection(_fov, size.x / (float)size.y, _nearClipDistance, _farClipDistance);
        public virtual void Resize(Vector2UInt size)
            => SetProjection(_fov, size.x / (float)size.y, _nearClipDistance, _farClipDistance);
        public virtual void Resize(int width, int height)
            => SetProjection(_fov, width / (float)height, _nearClipDistance, _farClipDistance);
        public virtual void Resize(uint width, uint height)
            => SetProjection(_fov, width / (float)height, _nearClipDistance, _farClipDistance);

        public virtual void KeyboardDown(KeyboardKey key)
        {
            switch (key)
            {
                case KeyboardKey.LeftShift:
                case KeyboardKey.RightShift:
                    if (!_fast)
                    {
                        _fast = true;
                        _speed *= 4f;
                    }
                    break;

                case KeyboardKey.W:
                case KeyboardKey.UpArrow:
                    _moveForward = 1f;
                    break;
                case KeyboardKey.S:
                case KeyboardKey.DownArrow:
                    _moveForward = -1f;
                    break;
                case KeyboardKey.A:
                case KeyboardKey.LeftArrow:
                    _moveRight = -1f;
                    break;
                case KeyboardKey.D:
                case KeyboardKey.RightArrow:
                    _moveRight = 1f;
                    break;
            }
        }
        public virtual void KeyboardUp(KeyboardKey key)
        {
            switch (key)
            {
                case KeyboardKey.LeftShift:
                case KeyboardKey.RightShift:
                    if (_fast)
                    {
                        _fast = false;
                        _speed *= 0.25f;
                    }
                    break;

                case KeyboardKey.W:
                case KeyboardKey.UpArrow:
                case KeyboardKey.S:
                case KeyboardKey.DownArrow:
                    _moveForward = 0f;
                    break;
                case KeyboardKey.A:
                case KeyboardKey.LeftArrow:
                case KeyboardKey.D:
                case KeyboardKey.RightArrow:
                    _moveRight = 0f;
                    break;
            }
        }
        public virtual void MouseButtonDown(MouseButton button) { if (button == _lookButton) _mouseButtonHold = true; }
        public virtual void MouseButtonUp(MouseButton button) { if (button == _lookButton) _mouseButtonHold = false; }
        public virtual void MouseMove(Vector2 difference)
        {
            if (_mouseButtonHold)
                UpdateUV(difference.X * _mouseSensibility, difference.Y * _mouseSensibility);
        }

        public virtual void TouchMove(TouchMoveEventArgs touch)
        {
            if (_touchCount == 1)
            {
                Vector2 difference = touch.Movement.Difference;
                UpdateUV(difference.X * _touchSensibility, difference.Y * _touchSensibility);
            }
            else if (_touchCount == 2)
            {
                Vector2 difference = touch.StartMovement.Difference;
                _moveForward = difference.Y * -_touchSensibility;
                _moveRight = difference.X * _touchSensibility;
            }
        }

        #endregion


    }
}
