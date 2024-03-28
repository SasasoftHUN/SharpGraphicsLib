using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class DebugUtils
    {

        [Conditional("DEBUG")]
        public static void ThrowIfNull(object obj, string argumentName, [CallerMemberName] string callerName = "", [CallerLineNumber] int callerLine = -1, [CallerFilePath] string callerFile = "")
        {
            if (obj == null)
                throw new ArgumentNullException(argumentName, $"{argumentName} is null at {callerName} ({callerLine}>{callerFile})!");
        }

    }
}
