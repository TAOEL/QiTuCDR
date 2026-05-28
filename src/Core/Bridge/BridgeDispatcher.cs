using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Bridge
{
    public sealed class BridgeDispatcher
    {
        private readonly Dictionary<string, IBridgeCommand> _commands;
        private readonly PluginStateMachine _stateMachine;
        private readonly ILogger _logger;

        public BridgeDispatcher(
            IEnumerable<IBridgeCommand> commands,
            PluginStateMachine stateMachine,
            ILogger logger)
        {
            _commands = commands.ToDictionary(x => x.Action, StringComparer.OrdinalIgnoreCase);
            _stateMachine = stateMachine;
            _logger = logger;
        }

        public async Task<ResponseDto> DispatchAsync(RequestDto request, CancellationToken token)
        {
            return await DispatchAsync(request, () => token, token).ConfigureAwait(false);
        }

        public async Task<ResponseDto> DispatchAsync(RequestDto request, Func<CancellationToken> businessTokenFactory, CancellationToken nonBusinessToken)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.Action))
            {
                return ResponseDto.Fail(request.RequestId, ErrorCodes.InvalidPayload, "RequestId and action are required.");
            }

            if (!_commands.TryGetValue(request.Action, out var command))
            {
                return ResponseDto.Fail(request.RequestId, ErrorCodes.InvalidPayload, $"Unknown action: {request.Action}");
            }

            if (command.RequiresReadyState && !_stateMachine.TryEnterBusy())
            {
                return ResponseDto.Fail(request.RequestId, ErrorCodes.StateForbidden, $"Plugin state does not allow business execution: {_stateMachine.Current}");
            }

            var token = command.RequiresReadyState ? businessTokenFactory() : nonBusinessToken;

            try
            {
                return await command.ExecuteAsync(request, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return ResponseDto.Fail(request.RequestId, ErrorCodes.TaskCancelled, "Task was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Command failed: {request.Action}", ex);
                return ResponseDto.Fail(request.RequestId, ErrorCodes.Unknown, "Command execution failed.");
            }
            finally
            {
                if (command.RequiresReadyState)
                {
                    _stateMachine.CompleteBusy();
                }
            }
        }
    }
}
