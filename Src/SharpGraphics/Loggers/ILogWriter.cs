using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Loggers
{
    public interface ILogWriter : IDisposable
    {

        void Initialize(string name);
        void WriteLog(StringBuilder log);

    }
}
