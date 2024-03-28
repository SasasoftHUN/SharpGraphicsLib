using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{

    public enum KeyboardKey
    {
        Unspecified = 0,
        
        Backspace = 8,
        Tab = 9,

        //Clear = 12,
        Enter = 13,
        LeftShift = 14,
        RightShift = 15,
        LeftControl = 17,
        RightControl = 18,
        LeftAlt = 9,
        RightAlt = 20,
        Pause = 24,
        Escape = 27,

        Spacebar = 32,
        PageUp = 33,
        PageDown = 34,

        End = 35,
        Home = 36,

        LeftArrow = 37,
        UpArrow = 38,
        RightArrow = 39,
        DownArrow = 40,

        //Select = 41,
        //Print = 42,
        //Execute = 43,

        PrintScreen = 44,
        Insert = 45,
        Delete = 46,
        Help = 47,

        /// <summary>
        /// The Multiply key (the multiplication key on the numeric keypad).
        /// </summary>
        Multiply = 42,
        /// <summary>
        /// The Add key (the addition key on the numeric keypad).
        /// </summary>
        Add = 43,
        /// <summary>
        /// The Separator key.
        /// </summary>
        //Separator = 108,
        /// <summary>
        /// The Subtract key (the subtraction key on the numeric keypad).
        /// </summary>
        Subtract = 45,
        /// <summary>
        /// The Decimal key (the decimal key on the numeric keypad).
        /// </summary>
        Decimal = 44,
        /// <summary>
        /// The Divide key (the division key on the numeric keypad).
        /// </summary>
        Divide = 47,

        Digit0 = 48,
        Digit1 = 49,
        Digit2 = 50,
        Digit3 = 51,
        Digit4 = 52,
        Digit5 = 53,
        Digit6 = 54,
        Digit7 = 55,
        Digit8 = 56,
        Digit9 = 57,

        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,

        /// <summary>
        /// The left Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        LeftWindows = 91,
        /// <summary>
        /// The right Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        RightWindows = 92,
        /// <summary>
        /// The Application key (Microsoft Natural Keyboard).
        /// </summary>
        Applications = 93,
        /// <summary>
        /// The Computer Sleep key.
        /// </summary>
        Sleep = 95,

        NumPad0 = 96,
        NumPad1 = 97,
        NumPad2 = 98,
        NumPad3 = 99,
        NumPad4 = 100,
        NumPad5 = 101,
        NumPad6 = 102,
        NumPad7 = 103,
        NumPad8 = 104,
        NumPad9 = 105,

        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        F13 = 124,
        F14 = 125,
        F15 = 126,
        F16 = 127,
        F17 = 128,
        F18 = 129,
        F19 = 130,
        F20 = 131,
        F21 = 132,
        F22 = 133,
        F23 = 134,
        F24 = 135,

        /// <summary>
        /// The Browser Back key (Windows 2000 or later).
        /// </summary>
        BrowserBack = 166,
        /// <summary>
        /// The Browser Forward key (Windows 2000 or later).
        /// </summary>
        BrowserForward = 167,
        /// <summary>
        /// The Browser Refresh key (Windows 2000 or later).
        /// </summary>
        BrowserRefresh = 168,
        /// <summary>
        /// The Browser Stop key (Windows 2000 or later).
        /// </summary>
        BrowserStop = 169,
        /// <summary>
        /// The Browser Search key (Windows 2000 or later).
        /// </summary>
        BrowserSearch = 170,
        /// <summary>
        /// The Browser Favorites key (Windows 2000 or later).
        /// </summary>
        BrowserFavorites = 171,
        /// <summary>
        /// The Browser Home key (Windows 2000 or later).
        /// </summary>
        BrowserHome = 172,

        /// <summary>
        /// The Volume Mute key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        VolumeMute = 173,
        /// <summary>
        /// The Volume Down key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        VolumeDown = 174,
        /// <summary>
        /// The Volume Up key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        VolumeUp = 175,
        /// <summary>
        /// The Media Next Track key (Windows 2000 or later).
        /// </summary>
        MediaNext = 176,
        /// <summary>
        /// The Media Previous Track key (Windows 2000 or later).
        /// </summary>
        MediaPrevious = 177,
        /// <summary>
        /// The Media Stop key (Windows 2000 or later).
        /// </summary>
        MediaStop = 178,
        /// <summary>
        /// The Media Play/Pause key (Windows 2000 or later).
        /// </summary>
        MediaPlay = 179,

        /// <summary>
        /// The Start Mail key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        LaunchMail = 180,
        /// <summary>
        /// The Select Media key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        LaunchMediaSelect = 181,
        /// <summary>
        /// The Start Application 1 key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        LaunchApp1 = 182,
        /// <summary>
        /// The Start Application 2 key (Microsoft Natural Keyboard, Windows 2000 or later).
        /// </summary>
        LaunchApp2 = 183,

        /// <summary>
        /// The OEM 1 key (OEM specific).
        /// </summary>
        //Oem1 = 186,
        /// <summary>
        /// The OEM Plus key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        //OemPlus = 187,
        /// <summary>
        /// The OEM Comma key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        //OemComma = 188,
        /// <summary>
        /// The OEM Minus key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        //OemMinus = 189,
        /// <summary>
        /// The OEM Period key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        //OemPeriod = 190,
        /// <summary>
        /// The OEM 2 key (OEM specific).
        /// </summary>
        //Oem2 = 191,
        /// <summary>
        /// The OEM 3 key (OEM specific).
        /// </summary>
        //Oem3 = 192,
        /// <summary>
        /// The OEM 4 key (OEM specific).
        /// </summary>
        //Oem4 = 219,
        /// <summary>
        /// The OEM 5 (OEM specific).
        /// </summary>
        //Oem5 = 220,
        /// <summary>
        /// The OEM 6 key (OEM specific).
        /// </summary>
        //Oem6 = 221,
        /// <summary>
        /// The OEM 7 key (OEM specific).
        /// </summary>
        //Oem7 = 222,
        /// <summary>
        /// The OEM 8 key (OEM specific).
        /// </summary>
        //Oem8 = 223,
        /// <summary>
        /// The OEM 102 key (OEM specific).
        /// </summary>
        //Oem102 = 226,

        /// <summary>
        /// The IME PROCESS key.
        /// </summary>
        //Process = 229,
        /// <summary>
        /// The PACKET key (used to pass Unicode characters with keystrokes).
        /// </summary>
        //Packet = 231,
        /// <summary>
        /// The ATTN key.
        /// </summary>
        //Attention = 246,
        /// <summary>
        /// The CRSEL (CURSOR SELECT) key.
        /// </summary>
        //CrSel = 247,
        /// <summary>
        /// The EXSEL (EXTEND SELECTION) key.
        /// </summary>
        //ExSel = 248,
        /// <summary>
        /// The ERASE EOF key.
        /// </summary>
        //EraseEndOfFile = 249,

        /// <summary>
        /// The PLAY key.
        /// </summary>
        //Play = 250,
        /// <summary>
        /// The ZOOM key.
        /// </summary>
        //Zoom = 251,
        /// <summary>
        /// A constant reserved for future use.
        /// </summary>
        //NoName = 252,
        /// <summary>
        /// The PA1 key.
        /// </summary>
        //Pa1 = 253,
        /// <summary>
        /// The CLEAR key (OEM specific).
        /// </summary>
        //OemClear = 254
    }

}
