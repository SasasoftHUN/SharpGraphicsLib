using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace SharpGraphics.OpenGL.Commands
{

    public interface IGLCommand
    {
        void Execute();
    }

    public class GLWaitableCommand : IGLCommand, IDisposable
    {

        #region Fields

        private bool _isDisposed;
        private readonly EventWaitHandle _waitHandle;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        public GLWaitableCommand(bool initiallySignaled)
            => _waitHandle = new EventWaitHandle(initiallySignaled, EventResetMode.ManualReset);

        #endregion

        #region Protected Methods

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _waitHandle.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public bool Wait()
        {
            if (_waitHandle != null)
            {
                _waitHandle.WaitOne();
                _waitHandle.Reset();
                return true;
            }
            else return false;
        }
        public bool Wait(int milliseconds)
        {
            if (_waitHandle != null)
            {
                if (!_waitHandle.WaitOne(milliseconds))
                    return false;
                else
                {
                    _waitHandle.Reset();
                    return true;
                }
            }
            else return false;
        }
        public bool Wait(TimeSpan time)
        {
            if (_waitHandle != null)
            {
                if (!_waitHandle.WaitOne(time))
                    return false;
                else
                {
                    _waitHandle.Reset();
                    return true;
                }
            }
            else return false;
        }

        public virtual void Execute()
        {
            if (_waitHandle != null)
                _waitHandle.Set();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override string ToString() => "Waitable";

        #endregion

    }

    public abstract class GLCommandWithResult<T> : GLWaitableCommand where T : unmanaged
    {

        public T? Result { get; protected set; }

        protected GLCommandWithResult() : base(false) { }

        public override string ToString() => $"Command With Result of {typeof(T).FullName}";

    }

}
