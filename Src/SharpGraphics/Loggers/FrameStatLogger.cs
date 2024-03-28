using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.Loggers
{
    public class FrameStatLogger : IFrameLogger, IDisposable
    {

        [Flags]
        public enum AdditionalLogOptions
        {
            None = 0,
            GC = 1,
            Process = 2,
        }

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

        private const string LOG_HEADER = "Time;FPS;Frame;Frame Time (ms);Update Time (ms);Render CPU Time (ms);Render GPU Time (ms)";

        private readonly StringBuilder _logBuilder = new StringBuilder();
        private readonly ILogWriter _logWriter;

        private readonly Process? _process;

        private readonly bool _logGC = false;
        private readonly bool _logProcess = false;

        private readonly LogFrameInterval? _frameInterval;
        private readonly LogTimeInterval? _timeInterval;

        protected readonly string logFilename = $"Log-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", System.Globalization.CultureInfo.InvariantCulture)}.csv";
        protected bool _isHeaderInitialized = false;
        protected bool _isHeaderWritten = false;

        private bool _isDisposed = false;

        #endregion

        #region Properties

        public string? LogName { get; set; }
        public string LogFilename => LogName == null ? logFilename : $"{LogName}-{logFilename}";

        #endregion

        #region Constructors

        public FrameStatLogger(ILogWriter logWriter, AdditionalLogOptions logOptions = AdditionalLogOptions.None)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException("logWriter");
            
            if (!logOptions.HasFlag(AdditionalLogOptions.None))
            {
                _logGC = logOptions.HasFlag(AdditionalLogOptions.GC);

                if (logOptions.HasFlag(AdditionalLogOptions.Process))
                    _process = Process.GetCurrentProcess();

                if (_process != null)
                {
                    _logProcess = logOptions.HasFlag(AdditionalLogOptions.Process);
                }
            }
        }

        public FrameStatLogger(ILogWriter logWriter, LogFrameInterval frameInterval, AdditionalLogOptions logOptions = AdditionalLogOptions.None) : this(logWriter, logOptions)
        {
            _frameInterval = frameInterval;
        }
        public FrameStatLogger(ILogWriter logWriter, LogTimeInterval timeInterval, AdditionalLogOptions logOptions = AdditionalLogOptions.None) : this(logWriter, logOptions)
        {
            _timeInterval = timeInterval;
        }

        ~FrameStatLogger()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        private void InitializeHeader(Timers timers)
        {
            _logWriter.Initialize(LogFilename);
            
            _logBuilder.AppendLine(LogFilename);

            _logBuilder.Append(LOG_HEADER);

            foreach (string timer in timers.CustomCPUTimers)
            {
                _logBuilder.Append(";CPU ");
                _logBuilder.Append(timer);
                _logBuilder.Append(" Time (ms)");
            }
            /*foreach (string timer in timers.CustomGPUTimers)
            {
                _logBuilder.Append(";GPU ");
                _logBuilder.Append(timer);
                _logBuilder.Append(" Time (ms)");
            }*/

            if (_logGC)
            {
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    _logBuilder.Append(";GC Gen ");
                    _logBuilder.Append(i);
                    _logBuilder.Append(" Collect");
                }

                _logBuilder.Append(";GC Managed Memory");
            }

            if (_logProcess)
                _logBuilder.Append(";Nonpaged System Memory Size;Paged System Memory Size;Paged Memory Size;Private Memory Size;Virtual Memory Size;Working Set;Privileged Processor Time (ms);User Processor Time (ms);Total Processor Time (ms);Threads");

            _logBuilder.AppendLine();
            _logWriter.WriteLog(_logBuilder);
            _logBuilder.Clear();

            _isHeaderInitialized = true;
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

                _process?.Dispose();
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
            if (!_isHeaderInitialized) //Late initialization for custom timers to be in the list
                InitializeHeader(timers);

            _logBuilder.Append(timers.Time.ToString("c"));
            _logBuilder.Append(';');
            _logBuilder.Append(timers.FPS);
            _logBuilder.Append(';');
            _logBuilder.Append(timers.Frame);
            _logBuilder.Append(';');
            _logBuilder.Append(timers.FrameTime.TotalMilliseconds.ToString("F3"));
            _logBuilder.Append(';');

            _logBuilder.Append(timers.UpdateTime.TotalMilliseconds.ToString("F3"));
            _logBuilder.Append(';');
            _logBuilder.Append(timers.RenderCPUTime.TotalMilliseconds.ToString("F3"));
            _logBuilder.Append(';');
            _logBuilder.Append(timers.RenderGPUTime.TotalMilliseconds.ToString("F3"));

            foreach (string timer in timers.CustomCPUTimers)
            {
                _logBuilder.Append(';');
                _logBuilder.Append(timers.GetCustomCPUTimer(timer).TotalMilliseconds.ToString("F3"));
            }
            /*foreach (string timer in timers.CustomGPUTimers)
            {
                _logBuilder.Append(';');
                _logBuilder.Append(timers.GetCustomGPUTimer(timer).TotalMilliseconds.ToString("F3"));
            }*/

            if (_logGC)
            {
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    _logBuilder.Append(';');
                    _logBuilder.Append(GC.CollectionCount(i));
                }

                _logBuilder.Append(';');
                _logBuilder.Append(GC.GetTotalMemory(false));
            }

            if (_logProcess && _process != null)
            {
                _logBuilder.Append(';');
                _logBuilder.Append(_process.NonpagedSystemMemorySize64);
                _logBuilder.Append(';');
                _logBuilder.Append(_process.PagedSystemMemorySize64);
                _logBuilder.Append(';');
                _logBuilder.Append(_process.PagedMemorySize64);
                _logBuilder.Append(';');
                _logBuilder.Append(_process.PrivateMemorySize64);
                _logBuilder.Append(';');
                _logBuilder.Append(_process.VirtualMemorySize64);
                _logBuilder.Append(';');
                _logBuilder.Append(_process.WorkingSet64);

                _logBuilder.Append(';');
                _logBuilder.Append(_process.PrivilegedProcessorTime.TotalMilliseconds.ToString("F3"));
                _logBuilder.Append(';');
                _logBuilder.Append(_process.UserProcessorTime.TotalMilliseconds.ToString("F3"));
                _logBuilder.Append(';');
                _logBuilder.Append(_process.TotalProcessorTime.TotalMilliseconds.ToString("F3"));

                _logBuilder.Append(';');
                _logBuilder.Append(_process.Threads.Count);
            }


            _logBuilder.AppendLine();

            _logWriter.WriteLog(_logBuilder);
            _logBuilder.Clear();
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
