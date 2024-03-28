using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SharpGraphics.GraphicsViews.WPF
{
    public static class WPFUtils
    {

        public static KeyboardKey ToKeyboardKey(this Key key)
            => key switch
            {
                Key.None => KeyboardKey.Unspecified,
                //Key.Cancel => 1,
                //Key.Back => 2,

                Key.Tab => KeyboardKey.Tab,
                //Key.LineFeed => 4,
                //Key.Clear => 5,
                //Key.Return => 6,
                Key.Enter => KeyboardKey.Enter,
                Key.Pause => KeyboardKey.Pause,

                //Key.Capital => 8,
                //Key.CapsLock => 8,

                //Key.KanaMode => 9,
                //Key.HangulMode => 9,
                //Key.JunjaMode => 10,
                //Key.FinalMode => 11,
                //Key.HanjaMode => 12,
                //Key.KanjiMode => 12,

                Key.Escape => KeyboardKey.Escape,

                //Key.ImeConvert => 14,
                //Key.ImeNonConvert => 0xF,
                //Key.ImeAccept => 0x10,
                //Key.ImeModeChange => 17,

                Key.Space => KeyboardKey.Spacebar,
                //Key.Prior => 19,
                Key.PageUp => KeyboardKey.PageUp,
                //Key.Next => 20,
                Key.PageDown => KeyboardKey.PageDown,
                Key.End => KeyboardKey.End,
                Key.Home => KeyboardKey.Home,

                Key.Left => KeyboardKey.LeftArrow,
                Key.Up => KeyboardKey.UpArrow,
                Key.Right => KeyboardKey.RightArrow,
                Key.Down => KeyboardKey.DownArrow,
                //Key.Select => 27,
                //Key.Print => 28,
                //Key.Execute => 29,
                //Key.Snapshot => 30,
                Key.PrintScreen => KeyboardKey.PrintScreen,
                Key.Insert => KeyboardKey.Insert,
                Key.Delete => KeyboardKey.Delete,
                Key.Help => KeyboardKey.Help,

                Key.D0 => KeyboardKey.Digit0,
                Key.D1 => KeyboardKey.Digit1,
                Key.D2 => KeyboardKey.Digit2,
                Key.D3 => KeyboardKey.Digit3,
                Key.D4 => KeyboardKey.Digit4,
                Key.D5 => KeyboardKey.Digit5,
                Key.D6 => KeyboardKey.Digit6,
                Key.D7 => KeyboardKey.Digit7,
                Key.D8 => KeyboardKey.Digit8,
                Key.D9 => KeyboardKey.Digit9,

                Key.A => KeyboardKey.A,
                Key.B => KeyboardKey.B,
                Key.C => KeyboardKey.C,
                Key.D => KeyboardKey.D,
                Key.E => KeyboardKey.E,
                Key.F => KeyboardKey.F,
                Key.G => KeyboardKey.G,
                Key.H => KeyboardKey.H,
                Key.I => KeyboardKey.I,
                Key.J => KeyboardKey.J,
                Key.K => KeyboardKey.K,
                Key.L => KeyboardKey.L,
                Key.M => KeyboardKey.M,
                Key.N => KeyboardKey.N,
                Key.O => KeyboardKey.O,
                Key.P => KeyboardKey.P,
                Key.Q => KeyboardKey.Q,
                Key.R => KeyboardKey.R,
                Key.S => KeyboardKey.S,
                Key.T => KeyboardKey.T,
                Key.U => KeyboardKey.U,
                Key.V => KeyboardKey.V,
                Key.W => KeyboardKey.W,
                Key.X => KeyboardKey.X,
                Key.Y => KeyboardKey.Y,
                Key.Z => KeyboardKey.Z,

                Key.LWin => KeyboardKey.LeftWindows,
                Key.RWin => KeyboardKey.RightWindows,
                Key.Apps => KeyboardKey.Applications,
                Key.Sleep => KeyboardKey.Sleep,

                Key.NumPad0 => KeyboardKey.NumPad0,
                Key.NumPad1 => KeyboardKey.NumPad1,
                Key.NumPad2 => KeyboardKey.NumPad2,
                Key.NumPad3 => KeyboardKey.NumPad3,
                Key.NumPad4 => KeyboardKey.NumPad4,
                Key.NumPad5 => KeyboardKey.NumPad5,
                Key.NumPad6 => KeyboardKey.NumPad6,
                Key.NumPad7 => KeyboardKey.NumPad7,
                Key.NumPad8 => KeyboardKey.NumPad8,
                Key.NumPad9 => KeyboardKey.NumPad9,

                Key.Multiply => KeyboardKey.Multiply,
                Key.Add => KeyboardKey.Add,
                //Key.Separator => 86,
                Key.Subtract => KeyboardKey.Subtract,
                Key.Decimal => KeyboardKey.Decimal,
                Key.Divide => KeyboardKey.Divide,

                Key.F1 => KeyboardKey.F1,
                Key.F2 => KeyboardKey.F2,
                Key.F3 => KeyboardKey.F3,
                Key.F4 => KeyboardKey.F4,
                Key.F5 => KeyboardKey.F5,
                Key.F6 => KeyboardKey.F6,
                Key.F7 => KeyboardKey.F7,
                Key.F8 => KeyboardKey.F8,
                Key.F9 => KeyboardKey.F9,
                Key.F10 => KeyboardKey.F10,
                Key.F11 => KeyboardKey.F11,
                Key.F12 => KeyboardKey.F12,
                Key.F13 => KeyboardKey.F13,
                Key.F14 => KeyboardKey.F14,
                Key.F15 => KeyboardKey.F15,
                Key.F16 => KeyboardKey.F16,
                Key.F17 => KeyboardKey.F17,
                Key.F18 => KeyboardKey.F18,
                Key.F19 => KeyboardKey.F19,
                Key.F20 => KeyboardKey.F20,
                Key.F21 => KeyboardKey.F21,
                Key.F22 => KeyboardKey.F22,
                Key.F23 => KeyboardKey.F23,
                Key.F24 => KeyboardKey.F24,

                //Key.NumLock => 114,
                //Key.Scroll => 115,
                Key.LeftShift => KeyboardKey.LeftShift,
                Key.RightShift => KeyboardKey.RightShift,
                Key.LeftCtrl => KeyboardKey.LeftControl,
                Key.RightCtrl => KeyboardKey.RightControl,
                Key.LeftAlt => KeyboardKey.LeftAlt,
                Key.RightAlt => KeyboardKey.RightAlt,

                Key.BrowserBack => KeyboardKey.BrowserBack,
                Key.BrowserForward => KeyboardKey.BrowserForward,
                Key.BrowserRefresh => KeyboardKey.BrowserRefresh,
                Key.BrowserStop => KeyboardKey.BrowserStop,
                Key.BrowserSearch => KeyboardKey.BrowserSearch,
                Key.BrowserFavorites => KeyboardKey.BrowserFavorites,
                Key.BrowserHome => KeyboardKey.BrowserHome,

                Key.VolumeMute => KeyboardKey.VolumeMute,
                Key.VolumeDown => KeyboardKey.VolumeDown,
                Key.VolumeUp => KeyboardKey.VolumeUp,
                Key.MediaNextTrack => KeyboardKey.MediaNext,
                Key.MediaPreviousTrack => KeyboardKey.MediaPrevious,
                Key.MediaStop => KeyboardKey.MediaStop,
                Key.MediaPlayPause => KeyboardKey.MediaPlay,

                Key.LaunchMail => KeyboardKey.LaunchMail,
                Key.SelectMedia => KeyboardKey.LaunchMediaSelect,
                Key.LaunchApplication1 => KeyboardKey.LaunchApp1,
                Key.LaunchApplication2 => KeyboardKey.LaunchApp2,

                //Key.Oem1 => 140,
                //Key.OemSemicolon => 140,
                //Key.OemPlus => 141,
                //Key.OemComma => 142,
                //Key.OemMinus => 143,
                //Key.OemPeriod => 144,
                //Key.Oem2 => 145,
                //Key.OemQuestion => 145,
                //Key.Oem3 => 146,
                //Key.OemTilde => 146,
                //Key.AbntC1 => 147,
                //Key.AbntC2 => 148,
                //Key.Oem4 => 149,
                //Key.OemOpenBrackets => 149,
                //Key.Oem5 => 150,
                //Key.OemPipe => 150,
                //Key.Oem6 => 151,
                //Key.OemCloseBrackets => 151,
                //Key.Oem7 => 152,
                //Key.OemQuotes => 152,
                //Key.Oem8 => 153,
                //Key.Oem102 => 154,
                //Key.OemBackslash => 154,
                //Key.ImeProcessed => 155,
                //Key.System => 156,
                //Key.OemAttn => 157,
                //Key.DbeAlphanumeric => 157,
                //Key.OemFinish => 158,
                //Key.DbeKatakana => 158,
                //Key.OemCopy => 159,
                //Key.DbeHiragana => 159,
                //Key.OemAuto => 160,
                //Key.DbeSbcsChar => 160,
                //Key.OemEnlw => 161,
                //Key.DbeDbcsChar => 161,
                //Key.OemBackTab => 162,
                //Key.DbeRoman => 162,
                //Key.Attn => 163,
                //Key.DbeNoRoman => 163,
                //Key.CrSel => 164,
                //Key.DbeEnterWordRegisterMode => 164,
                //Key.ExSel => 165,
                //Key.DbeEnterImeConfigureMode => 165,
                //Key.EraseEof => 166,
                //Key.DbeFlushString => 166,
                //Key.Play => 167,
                //Key.DbeCodeInput => 167,
                //Key.Zoom => 168,
                //Key.DbeNoCodeInput => 168,
                //Key.NoName => 169,
                //Key.DbeDetermineString => 169,
                //Key.Pa1 => 170,
                //Key.DbeEnterDialogConversionMode => 170,
                //Key.OemClear => 171,
                //Key.DeadCharProcessed => 172


                _ => KeyboardKey.Unspecified,
            };

        public static SharpGraphics.GraphicsViews.MouseButton ToMouseButton(this System.Windows.Input.MouseButton button)
            => button switch
            {
                System.Windows.Input.MouseButton.Left => SharpGraphics.GraphicsViews.MouseButton.Left,
                System.Windows.Input.MouseButton.Middle => SharpGraphics.GraphicsViews.MouseButton.Middle,
                System.Windows.Input.MouseButton.Right => SharpGraphics.GraphicsViews.MouseButton.Right,

                System.Windows.Input.MouseButton.XButton1 => SharpGraphics.GraphicsViews.MouseButton.Extra1,
                System.Windows.Input.MouseButton.XButton2 => SharpGraphics.GraphicsViews.MouseButton.Extra2,

                _ => SharpGraphics.GraphicsViews.MouseButton.Unspecified,
            };

        public static SharpGraphics.GraphicsViews.MouseButton ToMouseButton(this System.Windows.Forms.MouseButtons button)
            => button switch
            {
                System.Windows.Forms.MouseButtons.Left => SharpGraphics.GraphicsViews.MouseButton.Left,
                System.Windows.Forms.MouseButtons.Middle => SharpGraphics.GraphicsViews.MouseButton.Middle,
                System.Windows.Forms.MouseButtons.Right => SharpGraphics.GraphicsViews.MouseButton.Right,

                System.Windows.Forms.MouseButtons.XButton1 => SharpGraphics.GraphicsViews.MouseButton.Extra1,
                System.Windows.Forms.MouseButtons.XButton2 => SharpGraphics.GraphicsViews.MouseButton.Extra2,

                _ => SharpGraphics.GraphicsViews.MouseButton.Unspecified,
            };


        public static SharpGraphics.GraphicsViews.KeyboardEventArgs ToUserInterfaceEvent(this System.Windows.Input.KeyEventArgs keyEvent) =>
            new SharpGraphics.GraphicsViews.KeyboardEventArgs(keyEvent.Key.ToKeyboardKey(), !keyEvent.IsUp);
        public static SharpGraphics.GraphicsViews.MouseButtonEventArgs ToUserInterfaceEvent(this System.Windows.Input.MouseButtonEventArgs buttonEvent) =>
            new SharpGraphics.GraphicsViews.MouseButtonEventArgs(buttonEvent.ChangedButton.ToMouseButton(), buttonEvent.ButtonState == MouseButtonState.Pressed);

        public static SharpGraphics.GraphicsViews.MouseButtonEventArgs ToUserInterfaceEvent(this System.Windows.Forms.MouseEventArgs mouseEvent, bool isPressed) =>
            new SharpGraphics.GraphicsViews.MouseButtonEventArgs(mouseEvent.Button.ToMouseButton(), isPressed);

    }
}
