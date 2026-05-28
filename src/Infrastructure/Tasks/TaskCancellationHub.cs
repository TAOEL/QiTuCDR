using System;
using System.Threading;

namespace QiTuCDR.Infrastructure.Tasks
{
    public sealed class TaskCancellationHub : IDisposable
    {
        private readonly object _sync = new object();
        private CancellationTokenSource _current = new CancellationTokenSource();
        private bool _disposed;

        public CancellationToken CurrentToken
        {
            get
            {
                lock (_sync)
                {
                    ThrowIfDisposed();
                    return _current.Token;
                }
            }
        }

        public CancellationToken ResetForNewTask(TimeSpan timeout)
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                _current.Dispose();
                _current = new CancellationTokenSource(timeout);
                return _current.Token;
            }
        }

        public void CancelCurrentTask()
        {
            lock (_sync)
            {
                if (!_disposed && !_current.IsCancellationRequested)
                {
                    _current.Cancel();
                }
            }
        }

        public void CancelAll() => CancelCurrentTask();

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _current.Cancel();
                _current.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TaskCancellationHub));
            }
        }
    }
}
