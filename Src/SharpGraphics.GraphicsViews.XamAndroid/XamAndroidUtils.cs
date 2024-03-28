using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGraphics.GraphicsViews.XamAndroid
{
    public static class XamAndroidUtils
    {

        public static void ToTouchMovement(this Android.Views.MotionEvent motion, int pointerIndex, Vector2? pointerStartPosition, out SharpGraphics.GraphicsViews.InputMovement2D movement, out SharpGraphics.GraphicsViews.InputMovement2D startMovement)
        {
            Vector2 position = new System.Numerics.Vector2(motion.GetX(pointerIndex), motion.GetY(pointerIndex));
            if (motion.HistorySize > 0)
            {
                Vector2 previousPosition = new System.Numerics.Vector2(motion.GetHistoricalX(pointerIndex, 0), motion.GetHistoricalY(pointerIndex, 0));
                movement = new InputMovement2D(position, position - previousPosition);
            }
            else movement = new InputMovement2D(position);

            if (pointerStartPosition.HasValue)
                startMovement = new InputMovement2D(position, position - pointerStartPosition.Value);
            else startMovement = new InputMovement2D(position);
        }
        public static SharpGraphics.GraphicsViews.InputMovement2D ToInputMovement(this Android.Views.MotionEvent motion, int pointerIndex)
        {
            Vector2 position = new System.Numerics.Vector2(motion.GetX(pointerIndex), motion.GetY(pointerIndex));
            if (motion.HistorySize > 0)
            {
                Vector2 previousPosition = new System.Numerics.Vector2(motion.GetHistoricalX(pointerIndex, 0), motion.GetHistoricalY(pointerIndex, 0));
                return new InputMovement2D(position, position - previousPosition);
            }
            else return new InputMovement2D(position);
        }
            
        public static SharpGraphics.GraphicsViews.MouseButton ToMouseButton(this MotionEventButtonState button)
            => button switch
            {
                MotionEventButtonState.Primary => SharpGraphics.GraphicsViews.MouseButton.Left,
                MotionEventButtonState.Secondary => SharpGraphics.GraphicsViews.MouseButton.Right,
                MotionEventButtonState.Tertiary => SharpGraphics.GraphicsViews.MouseButton.Middle,
                MotionEventButtonState.Forward => SharpGraphics.GraphicsViews.MouseButton.Extra1,
                MotionEventButtonState.Back => SharpGraphics.GraphicsViews.MouseButton.Extra2,
                _ => SharpGraphics.GraphicsViews.MouseButton.Unspecified,
            };


        public static bool ToUserInterfaceEvent(this Android.Views.View.TouchEventArgs touchEvent, int pointerIndex, Vector2? pointerStartPosition, out SharpGraphics.GraphicsViews.UserInputEventArgs ev, out Vector2? position)
        {
            MotionEventToolType toolType = touchEvent.Event.GetToolType(pointerIndex);
            if (toolType != MotionEventToolType.Mouse)
            {
                switch (touchEvent.Event.ActionMasked)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.PointerDown:
                        if (!pointerStartPosition.HasValue)
                        {
                            ev = new SharpGraphics.GraphicsViews.TouchDetectEventArgs(touchEvent.Event.GetPointerId(pointerIndex), true);
                            position = new Vector2(touchEvent.Event.GetX(pointerIndex), touchEvent.Event.GetY(pointerIndex));
                            return true;
                        }
                        break;
                    case MotionEventActions.Up:
                    case MotionEventActions.PointerUp:
                        if (pointerStartPosition.HasValue)
                        {
                            ev = new SharpGraphics.GraphicsViews.TouchDetectEventArgs(touchEvent.Event.GetPointerId(pointerIndex), false);
                            position = default(Vector2?);
                            return true;
                        }
                        break;
                    case MotionEventActions.Move:
                        {
                            ToTouchMovement(touchEvent.Event, pointerIndex, pointerStartPosition, out InputMovement2D movement, out InputMovement2D startMovement);
                            ev = new SharpGraphics.GraphicsViews.TouchMoveEventArgs(touchEvent.Event.GetPointerId(pointerIndex), movement, startMovement);
                            position = pointerStartPosition;
                            return true;
                        }
                }

                ev = null;
                position = default(Vector2?);
                return false;
            }
            else
            {
                switch (touchEvent.Event.Action)
                {
                    case MotionEventActions.Move:
                        ev = new SharpGraphics.GraphicsViews.MouseMoveEventArgs(touchEvent.Event.ToInputMovement(pointerIndex));
                        position = default(Vector2?);
                        return true;
                    default:
                        ev = null;
                        position = default(Vector2?);
                        return false;
                }
            }
        }

        public static bool ToUserInterfaceEvent(this Android.Views.View.GenericMotionEventArgs motionEvent, int pointerIndex, out SharpGraphics.GraphicsViews.UserInputEventArgs ev)
        {
            MotionEventToolType toolType = motionEvent.Event.GetToolType(pointerIndex);
            if (toolType == MotionEventToolType.Mouse)
            {
                switch (motionEvent.Event.ActionMasked)
                {
                    case MotionEventActions.ButtonPress:
                        ev = new SharpGraphics.GraphicsViews.MouseButtonEventArgs(motionEvent.Event.ActionButton.ToMouseButton(), true);
                        return true;
                    case MotionEventActions.ButtonRelease:
                        ev = new SharpGraphics.GraphicsViews.MouseButtonEventArgs(motionEvent.Event.ActionButton.ToMouseButton(), false);
                        return true;
                    case MotionEventActions.HoverMove:
                        ev = new SharpGraphics.GraphicsViews.MouseMoveEventArgs(motionEvent.Event.ToInputMovement(pointerIndex));
                        return true;
                    default:
                        ev = null;
                        return false;
                }
            }
            else
            {
                ev = null;
                return false;
            }
        }

    }
}