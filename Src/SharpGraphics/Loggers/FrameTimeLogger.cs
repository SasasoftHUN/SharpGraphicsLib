using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.Loggers
{
    public class FrameTimeLogger : IFrameLogger, IDisposable
    {
        public class LogFrameInterval
        {
            public ulong startFrame;
            public ulong endFrame;

            public LogFrameInterval(ulong startFrame, ulong endFrame)
            {
                this.startFrame = startFrame;
                this.endFrame = endFrame;
            }
        }
        public class LogTimeInterval
        {
            public TimeSpan startTime;
            public TimeSpan endTime;

            public LogTimeInterval(TimeSpan startTime, TimeSpan endTime)
            {
                this.startTime = startTime;
                this.endTime = endTime;
            }
        }

        #region Fields

        private readonly ILogWriter _logWriter;

        private readonly LogFrameInterval? _frameInterval;
        private readonly LogTimeInterval? _timeInterval;

        private readonly List<double> _frameTimes;

        protected readonly string logFilename = $"Log-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", System.Globalization.CultureInfo.InvariantCulture)}.csv";

        private bool _isDisposed = false;

        #endregion

        #region Properties

        public string? LogName { get; set; }
        public string LogFilename => LogName == null ? logFilename : $"{LogName}-{logFilename}";

        #endregion

        #region Constructors

        public FrameTimeLogger(ILogWriter logWriter, int capacityHint = 0)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException("logWriter");
            _frameTimes = capacityHint > 0 ? new List<double>(capacityHint) : new List<double>();
        }

        public FrameTimeLogger(ILogWriter logWriter, LogFrameInterval frameInterval, int capacityHint = 0) : this(logWriter, capacityHint)
        {
            _frameInterval = frameInterval;
        }
        public FrameTimeLogger(ILogWriter logWriter, LogTimeInterval timeInterval, int capacityHint = 0) : this(logWriter, capacityHint)
        {
            _timeInterval = timeInterval;
        }

        ~FrameTimeLogger()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        private void WriteToFile()
        {
            _logWriter.Initialize(LogFilename);
            StringBuilder sb = new StringBuilder();
            foreach (double frameTime in _frameTimes)
                sb.AppendLine(frameTime.ToString("F3"));
            _logWriter.WriteLog(sb);
        }

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects)
                }*/

                WriteToFile();
                _logWriter?.Dispose();

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Log(Timers timers)
        {
            if (_frameInterval != null && (_frameInterval.startFrame > timers.Frame || _frameInterval.endFrame < timers.Frame))
                return;
            if (_timeInterval != null && (_timeInterval.startTime > timers.Time || _timeInterval.endTime < timers.Time))
                return;

            _frameTimes.Add(timers.FrameTime.TotalMilliseconds);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
