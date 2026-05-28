using System;
using System.Threading;
using System.Threading.Tasks;

namespace QiTuCDR.Infrastructure.COM
{
    public interface IComDispatcher
    {
        Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token);
        Task InvokeAsync(Action action, CancellationToken token);
    }
}
