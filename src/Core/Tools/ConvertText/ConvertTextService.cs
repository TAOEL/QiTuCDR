using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Core.Validators;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.ConvertText
{
    public sealed class ConvertTextService : IToolService
    {
        private readonly ICorelDocumentAdapter _documentAdapter;
        private readonly ISelectionSnapshotService _selectionSnapshotService;
        private readonly ToolRequestValidator _validator;
        private readonly EventBus _eventBus;
        private readonly PluginConfig _config;
        private readonly ILogger _logger;

        public ConvertTextService(
            ICorelDocumentAdapter documentAdapter,
            ISelectionSnapshotService selectionSnapshotService,
            ToolRequestValidator validator,
            EventBus eventBus,
            PluginConfig config,
            ILogger logger)
        {
            _documentAdapter = documentAdapter;
            _selectionSnapshotService = selectionSnapshotService;
            _validator = validator;
            _eventBus = eventBus;
            _config = config;
            _logger = logger;
        }

        public async Task<ResponseDto> ExecuteAsync(RequestDto dto, CancellationToken token)
        {
            var validationError = _validator.ValidateRangePayload(dto);
            if (validationError != null)
            {
                return ResponseDto.Fail(dto.RequestId, validationError, "Invalid convert text payload.");
            }

            if (!_documentAdapter.HasOpenDocument)
            {
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.NoDocument, "No CorelDRAW document is open.");
            }

            var range = dto.Payload.Value<string>("range") ?? ToolRange.Selection.ToString();
            var includeHidden = dto.Payload.Value<bool?>("includeHidden") ?? false;
            SelectionSnapshot? snapshot = null;

            if (Enum.TryParse(range, true, out ToolRange parsedRange) && parsedRange == ToolRange.Selection)
            {
                snapshot = await _selectionSnapshotService.CaptureAsync(token).ConfigureAwait(false);
                if (snapshot.ShapeIds.Count == 0)
                {
                    return ResponseDto.Fail(dto.RequestId, ErrorCodes.EmptySelection, "No selected shapes were found.");
                }

                if (!await _documentAdapter.CanResolveSelectionSnapshotAsync(snapshot, token).ConfigureAwait(false))
                {
                    return ResponseDto.Fail(dto.RequestId, ErrorCodes.EmptySelection, "Selected shapes could not be resolved.");
                }
            }

            try
            {
                var result = await _documentAdapter.ConvertTextAsync(
                    parsedRange,
                    snapshot,
                    includeHidden,
                    _config.BatchSize,
                    PublishProgress,
                    token).ConfigureAwait(false);

                PublishCompleted(result);
                return ResponseDto.Ok(dto.RequestId, new JObject
                {
                    ["converted"] = result.Converted,
                    ["skipped"] = result.Skipped,
                    ["total"] = result.Total
                }, "Convert text completed.");
            }
            catch (OperationCanceledException)
            {
                PublishFailed(ErrorCodes.TaskCancelled, "Convert text was cancelled.");
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.TaskCancelled, "Convert text was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Error("Convert text failed.", ex);
                PublishFailed(ErrorCodes.ComException, "CorelDRAW convert text operation failed.");
                return ResponseDto.Fail(dto.RequestId, ErrorCodes.ComException, "CorelDRAW convert text operation failed.");
            }
        }

        private void PublishProgress(ConvertTextResult result)
        {
            _eventBus.Publish(EventDto.Create(EventTypes.TaskProgress, new JObject
            {
                ["action"] = Actions.ConvertText,
                ["converted"] = result.Converted,
                ["skipped"] = result.Skipped,
                ["total"] = result.Total
            }));
        }

        private void PublishCompleted(ConvertTextResult result)
        {
            _eventBus.Publish(EventDto.Create(EventTypes.TaskCompleted, new JObject
            {
                ["action"] = Actions.ConvertText,
                ["converted"] = result.Converted,
                ["skipped"] = result.Skipped,
                ["total"] = result.Total
            }));
        }

        private void PublishFailed(string errorCode, string message)
        {
            _eventBus.Publish(EventDto.Create(EventTypes.TaskFailed, new JObject
            {
                ["action"] = Actions.ConvertText,
                ["errorCode"] = errorCode,
                ["message"] = message
            }));
        }
    }
}
