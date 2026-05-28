using System;
using System.Threading;
using System.Windows.Threading;

namespace QiTuCDR.Host.Lifecycle
{
    internal static class DispatcherProvider
    {
        public static Dispatcher Current
        {
            get
            {
                if (Dispatcher.FromThread(Thread.CurrentThread) is Dispatcher dispatcher)
                {
                    return dispatcher;
                }

                throw new InvalidOperationException("No WPF dispatcher is available on the current thread.");
            }
        }
    }
}
