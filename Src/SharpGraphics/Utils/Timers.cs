using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics.Utils
{
    public sealed class Timers
    {

        private readonly struct CustomCPUTimer
        {
            public readonly TimeSpan startTime;
            public readonly bool isRunning;
            public readonly TimeSpan lastMeasuredTime;

            public CustomCPUTimer(TimeSpan startTime)
            {
                this.startTime = startTime;
                isRunning = true;
                lastMeasuredTime = TimeSpan.Zero;
            }
            public CustomCPUTimer(in CustomCPUTimer timer, TimeSpan currentTime)
            {
                if (timer.isRunning)
                {
                    startTime = timer.startTime;
                    isRunning = false;
                    lastMeasuredTime = currentTime - timer.startTime;
                }
                else
                {
                    startTime = currentTime;
                    isRunning = true;
                    lastMeasuredTime = timer.lastMeasuredTime;
                }
            }
        }

        #region Fields

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _lastTime = 0.0;
        private double _lastFPSTime = 0.0;
        private TimeSpan _renderCPUStart = TimeSpan.Zero;
        private uint _currentFPS = 0;

        private readonly ConcurrentDictionary<string, CustomCPUTimer> _customCPUTimers = new ConcurrentDictionary<string, CustomCPUTimer>();

        private readonly StringBuilder _sb = new StringBuilder();

        #endregion

        #region Properties

        public TimeSpan Time { get; private set; } = TimeSpan.Zero;
        public long TimeMilliseconds { get; private set; } = 0L;
        public float TimeSeconds { get; private set; } = 0f;
        public double TimeSecondsAsDouble { get; private set; } = 0.0;
        public float DeltaTime { get; private set; } = 0f;

        public ulong Frame { get; private set; } = 0;
        //public ulong GPUFrame { get; private set; } = 0;
        //public uint CPUAheadGPUFrames { get => Convert.ToUInt32(Frame - GPUFrame); }
        public TimeSpan UpdateTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan RenderCPUTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan RenderGPUTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan FrameTime { get; private set; } = TimeSpan.Zero;
        public float FPS { get; private set; } = 0;
        public IEnumerable<string> CustomCPUTimers { get => _customCPUTimers.Keys; }

        public string ReadableTimes
        {
            get
            {
                _sb.Clear();

                _sb.Append("UpdateTime: ");
                _sb.Append(UpdateTime);
                _sb.Append('\n');
                _sb.Append("RenderCPUTime: ");
                _sb.Append(RenderCPUTime);
                _sb.Append('\n');
                _sb.Append("RenderGPUTime: ");
                _sb.Append(RenderGPUTime);
                _sb.Append('\n');
                _sb.Append("FrameTime: ");
                _sb.Append(FrameTime);
                _sb.Append('\n');
                _sb.Append("FPS: ");
                _sb.Append(FPS);
                _sb.Append("\n\n");

                foreach (KeyValuePair<string, CustomCPUTimer> timer in _customCPUTimers)
                {
                    _sb.Append(timer.Key);
                    _sb.Append(": ");
                    _sb.Append(timer.Value.lastMeasuredTime);
                    _sb.Append('\n');
                }

                return _sb.ToString();
            }
        }

        #endregion

        #region Constructors

        public Timers() { }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
        }

        public void BeforeUpdate()
        {
            FrameTime = _stopwatch.Elapsed - Time;
            ++Frame;

            Time = _stopwatch.Elapsed;
            TimeMilliseconds = (long)Time.TotalMilliseconds;
            TimeSeconds = TimeMilliseconds * 0.001f;

            double timeSecondsAsDouble = TimeMilliseconds * 0.001;
            TimeSecondsAsDouble = timeSecondsAsDouble;
            DeltaTime = Convert.ToSingle(timeSecondsAsDouble - _lastTime);

            //FPS
            ++_currentFPS;
            double fpsDifference = timeSecondsAsDouble - _lastFPSTime;
            if (fpsDifference > 1.0)
            {
                FPS = Convert.ToSingle(Math.Round(_currentFPS / fpsDifference, 1));
                _currentFPS = 0;
                _lastFPSTime = timeSecondsAsDouble;
            }
        }
        public void AfterUpdate()
        {
            _lastTime = _stopwatch.ElapsedMilliseconds * 0.001;
            UpdateTime = _stopwatch.Elapsed - Time;
        }

        public void BeforeRender()
        {
            _renderCPUStart = _stopwatch.Elapsed;
        }
        public void AfterRender()
        {
            RenderCPUTime = _stopwatch.Elapsed - _renderCPUStart;
        }

        public void StartCustomCPUTimer(string timer)
        {
            if (_customCPUTimers.TryGetValue(timer, out CustomCPUTimer cpuTimer))
            {
                if (!cpuTimer.isRunning)
                    _customCPUTimers[timer] = new CustomCPUTimer(cpuTimer, _stopwatch.Elapsed);
            }
            else _customCPUTimers[timer] = new CustomCPUTimer(_stopwatch.Elapsed);
        }
        public void StopCustomCPUTimer(string timer)
        {
            if (_customCPUTimers.TryGetValue(timer, out CustomCPUTimer cpuTimer) && cpuTimer.isRunning)
                _customCPUTimers[timer] = new CustomCPUTimer(cpuTimer, _stopwatch.Elapsed);
        }
        public TimeSpan GetCustomCPUTimer(string timer)
            => _customCPUTimers.TryGetValue(timer, out CustomCPUTimer cpuTimer) ? cpuTimer.lastMeasuredTime : TimeSpan.Zero;

        #endregion

    }
}
