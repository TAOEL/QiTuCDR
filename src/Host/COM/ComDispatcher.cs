using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using QiTuCDR.Infrastructure.COM;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.COM
{
    public sealed class ComDispatcher : IComDispatcher
    {
        private readonly Dispatcher _dispatcher;
        private readonly ILogger _logger;

        public ComDispatcher(Dispatcher dispatcher, ILogger logger)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger;
        }

        public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(token);
            }

            if (_dispatcher.CheckAccess())
            {
                return ExecuteOnCurrentThread(action, token);
            }

            return _dispatcher.InvokeAsync(() => ExecuteOnCurrentThread(action, token), DispatcherPriority.Send, token).Task.Unwrap();
        }

        public Task InvokeAsync(Action action, CancellationToken token)
        {
            return InvokeAsync<object?>(() =>
            {
                action();
                return null;
            }, token);
        }

        private Task<T> ExecuteOnCurrentThread<T>(Func<T> action, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(action());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("COM dispatcher action failed.", ex);
                throw;
            }
        }
    }
}
