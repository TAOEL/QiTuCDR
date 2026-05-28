using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Host.Environment;
using QiTuCDR.Infrastructure.COM;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class CorelDocumentAdapterFactoryTests
    {
        [TestMethod]
        public void CreateReturnsDynamicAdapterByDefault()
        {
            var factory = new CorelDocumentAdapterFactory();

            var adapter = factory.Create(null, new InlineComDispatcher(), new PluginConfig(), new MemoryLogger());

            Assert.IsInstanceOfType(adapter, typeof(DynamicCorelDocumentAdapter));
        }

        [TestMethod]
        public void CreateReturnsDynamicAdapterWhenTypedInteropIsPreferredButNotCompiled()
        {
            var factory = new CorelDocumentAdapterFactory();

            var adapter = factory.Create(null, new InlineComDispatcher(), new PluginConfig
            {
                PreferTypedCorelInterop = true
            }, new MemoryLogger());

            Assert.IsInstanceOfType(adapter, typeof(DynamicCorelDocumentAdapter));
        }

        private sealed class InlineComDispatcher : IComDispatcher
        {
            public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(action());
            }

            public Task InvokeAsync(Action action, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                action();
                return Task.CompletedTask;
            }
        }
    }
}
