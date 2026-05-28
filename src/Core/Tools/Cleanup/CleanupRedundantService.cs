using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Cleanup
{
    public sealed class CleanupRedundantService : IToolService
    {
        private readonly ICorelDocumentAdapter _documentAdapter;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;

        public CleanupRedundantService(ICorelDocumentAdapter documentAdapter, EventBus eventBus, ILogger logger)
        {
            _documentAdapter = documentAdapter;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<ResponseDto> ExecuteAsync(RequestDto dto, CancellationToken token)
        {
            if (!_documentAdapter.HasOpenDocument)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.NoDocument, "No CorelDRAW document is open.");
            }

            if (dto.Payload.Value<bool?>("confirmed") != true)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.InvalidPayload, "Cleanup requires explicit confirmation.");
            }

            try
            {
                var removed = await _documentAdapter.CleanupRedundantAsync(token).ConfigureAwait(false);

                _eventBus.Publish(EventDto.Create(EventTypes.TaskCompleted, new JObject
                {
                    ["action"] = Actions.CleanupRedundant,
                    ["removed"] = removed
                }));

                return ResponseDto.Ok(dto.RequestId, new JObject { ["removed"] = removed });
            }
            catch (System.OperationCanceledException)
            {
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.CleanupRedundant,
                    ["errorCode"] = ErrorCodes.TaskCancelled,
                    ["message"] = "Cleanup redundant was cancelled."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.TaskCancelled, "Cleanup redundant was cancelled.");
            }
            catch (System.Exception ex)
            {
                _logger.Error("Cleanup redundant failed.", ex);
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.CleanupRedundant,
                    ["errorCode"] = ErrorCodes.ComException,
                    ["message"] = "CorelDRAW cleanup operation failed."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.ComException, "CorelDRAW cleanup operation failed.");
            }
        }
    }
}
