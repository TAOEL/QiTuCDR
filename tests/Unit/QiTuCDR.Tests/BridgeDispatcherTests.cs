using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Core.Bridge;
using QiTuCDR.Core.Tools;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Shared;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class BridgeDispatcherTests
    {
        [TestMethod]
        public async Task DispatchesEchoWithoutReadyState()
        {
            var dispatcher = new BridgeDispatcher(new[] { new EchoCommand() }, new PluginStateMachine(), new MemoryLogger());

            var response = await dispatcher.DispatchAsync(new RequestDto
            {
                RequestId = "r1",
                Action = Actions.Echo,
                Payload = new JObject { ["hello"] = "world" }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual("QiTuCDR bridge online", response.Payload.Value<string>("native"));
        }

        [TestMethod]
        public async Task UnknownActionReturnsInvalidPayload()
        {
            var dispatcher = new BridgeDispatcher(new[] { new EchoCommand() }, new PluginStateMachine(), new MemoryLogger());

            var response = await dispatcher.DispatchAsync(new RequestDto
            {
                RequestId = "r2",
                Action = "missing"
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.InvalidPayload, response.ErrorCode);
        }

        [TestMethod]
        public async Task CreatesBusinessTokenOnlyAfterEnteringBusy()
        {
            var stateMachine = new PluginStateMachine();
            stateMachine.TransitionTo(PluginState.Preheating);
            stateMachine.TransitionTo(PluginState.Ready);
            var command = new TokenProbeCommand(requiresReadyState: true);
            var dispatcher = new BridgeDispatcher(new[] { command }, stateMachine, new MemoryLogger());
            var factoryCalls = 0;

            var response = await dispatcher.DispatchAsync(
                new RequestDto { RequestId = "r3", Action = command.Action },
                () =>
                {
                    factoryCalls++;
                    return new CancellationTokenSource().Token;
                },
                CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(1, factoryCalls);
            Assert.IsTrue(command.WasExecuted);
            Assert.AreEqual(PluginState.Ready, stateMachine.Current);
        }

        [TestMethod]
        public async Task BusyBusinessRequestDoesNotCreateNewTaskToken()
        {
            var stateMachine = new PluginStateMachine();
            stateMachine.TransitionTo(PluginState.Preheating);
            stateMachine.TransitionTo(PluginState.Ready);
            Assert.IsTrue(stateMachine.TryEnterBusy());

            var command = new TokenProbeCommand(requiresReadyState: true);
            var dispatcher = new BridgeDispatcher(new[] { command }, stateMachine, new MemoryLogger());
            var factoryCalls = 0;

            var response = await dispatcher.DispatchAsync(
                new RequestDto { RequestId = "r4", Action = command.Action },
                () =>
                {
                    factoryCalls++;
                    return CancellationToken.None;
                },
                CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.StateForbidden, response.ErrorCode);
            Assert.AreEqual(0, factoryCalls);
            Assert.IsFalse(command.WasExecuted);
        }

        private sealed class TokenProbeCommand : IBridgeCommand
        {
            public TokenProbeCommand(bool requiresReadyState)
            {
                RequiresReadyState = requiresReadyState;
            }

            public string Action => "tokenProbe";
            public bool RequiresReadyState { get; }
            public bool WasExecuted { get; private set; }

            public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
            {
                WasExecuted = true;
                return Task.FromResult(ResponseDto.Ok(request.RequestId, new JObject
                {
                    ["canBeCancelled"] = token.CanBeCanceled
                }));
            }
        }
    }

    internal sealed class MemoryLogger : ILogger
    {
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Error(string message, System.Exception? exception = null) { }
    }
}
