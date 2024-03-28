using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public static class InputUtils
    {

        public static ISet<KeyboardKey> CreateSetOfAllKeys() => new SortedSet<KeyboardKey>(Enum.GetValues(typeof(KeyboardKey)).Cast<KeyboardKey>());
        public static ISet<MouseButton> CreateSetOfAllButtons() => new SortedSet<MouseButton>(Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>());

    }
}
