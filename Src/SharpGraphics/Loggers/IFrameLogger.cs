using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Loggers
{
    public interface IFrameLogger
    {
        void Log(Timers timers);
    }
}
