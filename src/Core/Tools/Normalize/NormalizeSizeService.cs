using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Normalize
{
    public sealed class NormalizeSizeService : IToolService
    {
        private readonly ICorelDocumentAdapter _documentAdapter;
        private readonly ISelectionSnapshotService _selectionSnapshotService;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;

        public NormalizeSizeService(ICorelDocumentAdapter documentAdapter, ISelectionSnapshotService selectionSnapshotService, EventBus eventBus, ILogger logger)
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

            var width = dto.Payload.Value<double?>("width");
            var height = dto.Payload.Value<double?>("height");
            var outlineWidth = dto.Payload.Value<double?>("outlineWidth");
            if (width == null && height == null && outlineWidth == null)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.InvalidPayload, "Width, height or outline width is required.");
            }

            if (!IsValidPositiveSize(width) || !IsValidPositiveSize(height) || !IsValidOutlineWidth(outlineWidth))
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.InvalidPayload, "Normalize size values are out of range.");
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

            try
            {
                var normalized = await _documentAdapter.NormalizeSizeAsync(snapshot, new NormalizeSizeOptions
                {
                    Width = width,
                    Height = height,
                    OutlineWidth = outlineWidth,
                    LockRatio = dto.Payload.Value<bool?>("lockRatio") ?? false
                }, token).ConfigureAwait(false);

                _eventBus.Publish(EventDto.Create(EventTypes.TaskCompleted, new JObject
                {
                    ["action"] = Actions.NormalizeSize,
                    ["normalized"] = normalized
                }));

                return ResponseDto.Ok(dto.RequestId, new JObject { ["normalized"] = normalized });
            }
            catch (System.OperationCanceledException)
            {
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.NormalizeSize,
                    ["errorCode"] = ErrorCodes.TaskCancelled,
                    ["message"] = "Normalize size was cancelled."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.TaskCancelled, "Normalize size was cancelled.");
            }
            catch (System.Exception ex)
            {
                _logger.Error("Normalize size failed.", ex);
                _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
                {
                    ["action"] = Actions.NormalizeSize,
                    ["errorCode"] = ErrorCodes.ComException,
                    ["message"] = "CorelDRAW normalize operation failed."
                }));
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.ComException, "CorelDRAW normalize operation failed.");
            }
        }

        private static bool IsValidPositiveSize(double? value)
        {
            return value == null || (!double.IsNaN(value.Value) && !double.IsInfinity(value.Value) && value.Value > 0);
        }

        private static bool IsValidOutlineWidth(double? value)
        {
            return value == null || (!double.IsNaN(value.Value) && !double.IsInfinity(value.Value) && value.Value >= 0);
        }
    }
}
