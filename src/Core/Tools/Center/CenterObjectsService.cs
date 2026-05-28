using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Center
{
    public sealed class CenterObjectsService : IToolService
    {
        private readonly ICorelDocumentAdapter _documentAdapter;
        private readonly ISelectionSnapshotService _selectionSnapshotService;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;

        public CenterObjectsService(ICorelDocumentAdapter documentAdapter, ISelectionSnapshotService selectionSnapshotService, EventBus eventBus, ILogger logger)
        {
            _documentAdapter = documentAdapter;
            _selectionSnapshotService = selectionSnapshotService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<ResponseDto> ExecuteAsync(RequestDto dto, CancellationToken token)
        {
            if (!_documentAdapter.HasOpenDocument)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.NoDocument, "No CorelDRAW document is open.");
            }

            var snapshot = await _selectionSnapshotService.CaptureAsync(token).ConfigureAwait(false);
            if (snapshot.ShapeIds.Count == 0)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.EmptySelection, "No selected shapes were found.");
            }

            if (!await _documentAdapter.CanResolveSelectionSnapshotAsync(snapshot, token).ConfigureAwait(false))
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.EmptySelection, "Selected shapes could not be resolved.");
            }

            var mode = dto.Payload.Value<string>("mode") ?? "group";
            if (!IsAllowedMode(mode))
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.InvalidPayload, "Invalid center mode.");
            }

            try
            {
                var centered = await _documentAdapter.CenterObjectsAsync(snapshot, mode, token).ConfigureAwait(false);

                _eventBus.Publish(EventDto.Create(EventTypes.TaskCompleted, new JObject
                {
                    ["action"] = Actions.CenterObjects,
                    ["centered"] = centered,
                    ["mode"] = mode
                }));

                return ResponseDto.Ok(dto.RequestId, new JObject { ["centered"] = centered });
            }
            catch (System.OperationCanceledException)
            {
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.CenterObjects,
                    ["errorCode"] = ErrorCodes.TaskCancelled,
                    ["message"] = "Center objects was cancelled."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.TaskCancelled, "Center objects was cancelled.");
            }
            catch (System.Exception ex)
            {
                _logger.Error("Center objects failed.", ex);
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.CenterObjects,
                    ["errorCode"] = ErrorCodes.ComException,
                    ["message"] = "CorelDRAW center operation failed."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.ComException, "CorelDRAW center operation failed.");
            }
        }

        private static bool IsAllowedMode(string mode)
        {
            return mode == "group" || mode == "individual";
        }
    }
}
