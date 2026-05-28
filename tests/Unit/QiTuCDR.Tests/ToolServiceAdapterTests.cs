using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Core.Tools.Center;
using QiTuCDR.Core.Tools.Cleanup;
using QiTuCDR.Core.Tools.ConvertText;
using QiTuCDR.Core.Tools.Normalize;
using QiTuCDR.Core.Validators;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Shared;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class ToolServiceAdapterTests
    {
        [TestMethod]
        public async Task ConvertTextUsesDocumentAdapterAndReturnsCounts()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var service = new ConvertTextService(
                adapter,
                new SelectionSnapshotService(adapter),
                new ToolRequestValidator(),
                new EventBus(),
                new PluginConfig(),
                new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "convert-1",
                Action = Actions.ConvertText,
                Payload = new JObject
                {
                    ["range"] = "Selection",
                    ["includeHidden"] = false
                }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(2, response.Payload.Value<int>("converted"));
            Assert.AreEqual(1, response.Payload.Value<int>("skipped"));
            Assert.AreEqual(3, response.Payload.Value<int>("total"));
            Assert.AreEqual(1, adapter.ConvertCalls);
        }

        [TestMethod]
        public async Task ConvertTextReturnsNoDocumentBeforeCallingAdapterWork()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: false);
            var service = new ConvertTextService(
                adapter,
                new SelectionSnapshotService(adapter),
                new ToolRequestValidator(),
                new EventBus(),
                new PluginConfig(),
                new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "convert-2",
                Action = Actions.ConvertText,
                Payload = new JObject { ["range"] = "Selection" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.NoDocument, response.ErrorCode);
            Assert.AreEqual(0, adapter.ConvertCalls);
        }

        [TestMethod]
        public async Task ConvertTextReturnsEmptySelectionWhenSnapshotCannotResolve()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                CanResolveSnapshot = false
            };
            var service = new ConvertTextService(
                adapter,
                new SelectionSnapshotService(adapter),
                new ToolRequestValidator(),
                new EventBus(),
                new PluginConfig(),
                new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "convert-3",
                Action = Actions.ConvertText,
                Payload = new JObject { ["range"] = "Selection" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.EmptySelection, response.ErrorCode);
            Assert.AreEqual(0, adapter.ConvertCalls);
        }

        [TestMethod]
        public async Task AdapterCanReportSnapshotResolution()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var canResolve = await adapter.CanResolveSelectionSnapshotAsync(new SelectionSnapshot(new[] { 10 }), CancellationToken.None);
            var cannotResolve = await adapter.CanResolveSelectionSnapshotAsync(new SelectionSnapshot(Array.Empty<int>()), CancellationToken.None);

            Assert.IsTrue(canResolve);
            Assert.IsFalse(cannotResolve);
        }

        [TestMethod]
        public async Task ConvertTextPublishesProgressAndCompletedEvents()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new ConvertTextService(
                adapter,
                new SelectionSnapshotService(adapter),
                new ToolRequestValidator(),
                eventBus,
                new PluginConfig(),
                new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "convert-4",
                Action = Actions.ConvertText,
                Payload = new JObject { ["range"] = "Selection" }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(EventTypes.TaskProgress, events[0].Event);
            Assert.AreEqual(EventTypes.TaskCompleted, events[1].Event);
            Assert.AreEqual(Actions.ConvertText, events[1].Payload.Value<string>("action"));
            Assert.AreEqual(2, events[1].Payload.Value<int>("converted"));
        }

        [TestMethod]
        public async Task ConvertTextPublishesFailedEventWhenCancelled()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                ThrowOnConvert = new OperationCanceledException()
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new ConvertTextService(
                adapter,
                new SelectionSnapshotService(adapter),
                new ToolRequestValidator(),
                eventBus,
                new PluginConfig(),
                new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "convert-5",
                Action = Actions.ConvertText,
                Payload = new JObject { ["range"] = "Selection" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.TaskCancelled, response.ErrorCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventTypes.TaskFailed, events[0].Event);
            Assert.AreEqual(ErrorCodes.TaskCancelled, events[0].Payload.Value<string>("errorCode"));
        }

        [TestMethod]
        public async Task NormalizeSizeAllowsOutlineOnlyAndPublishesCompletedEvent()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new NormalizeSizeService(adapter, new SelectionSnapshotService(adapter), eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "normalize-1",
                Action = Actions.NormalizeSize,
                Payload = new JObject
                {
                    ["outlineWidth"] = 0.2,
                    ["lockRatio"] = true
                }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(3, response.Payload.Value<int>("normalized"));
            Assert.AreEqual(1, adapter.NormalizeCalls);
            Assert.IsNull(adapter.LastNormalizeOptions!.Width);
            Assert.AreEqual(0.2, adapter.LastNormalizeOptions.OutlineWidth);
            Assert.IsTrue(adapter.LastNormalizeOptions.LockRatio);
            Assert.AreEqual(EventTypes.TaskCompleted, events[0].Event);
            Assert.AreEqual(Actions.NormalizeSize, events[0].Payload.Value<string>("action"));
        }

        [TestMethod]
        public async Task NormalizeSizeRejectsInvalidNumericPayloadBeforeAdapterWork()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var service = new NormalizeSizeService(adapter, new SelectionSnapshotService(adapter), new EventBus(), new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "normalize-2",
                Action = Actions.NormalizeSize,
                Payload = new JObject
                {
                    ["width"] = -10,
                    ["height"] = 20
                }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.InvalidPayload, response.ErrorCode);
            Assert.AreEqual(0, adapter.NormalizeCalls);
        }

        [TestMethod]
        public async Task NormalizeSizeReturnsEmptySelectionWhenSnapshotCannotResolve()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                CanResolveSnapshot = false
            };
            var service = new NormalizeSizeService(adapter, new SelectionSnapshotService(adapter), new EventBus(), new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "normalize-3",
                Action = Actions.NormalizeSize,
                Payload = new JObject { ["width"] = 10 }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.EmptySelection, response.ErrorCode);
            Assert.AreEqual(0, adapter.NormalizeCalls);
        }

        [TestMethod]
        public async Task NormalizeSizePublishesFailedEventWhenCancelled()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                ThrowOnNormalize = new OperationCanceledException()
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new NormalizeSizeService(adapter, new SelectionSnapshotService(adapter), eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "normalize-4",
                Action = Actions.NormalizeSize,
                Payload = new JObject { ["width"] = 10 }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.TaskCancelled, response.ErrorCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventTypes.TaskFailed, events[0].Event);
            Assert.AreEqual(ErrorCodes.TaskCancelled, events[0].Payload.Value<string>("errorCode"));
        }

        [TestMethod]
        public async Task CenterObjectsPublishesCompletedEvent()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new CenterObjectsService(adapter, new SelectionSnapshotService(adapter), eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "center-1",
                Action = Actions.CenterObjects,
                Payload = new JObject { ["mode"] = "individual" }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(3, response.Payload.Value<int>("centered"));
            Assert.AreEqual(1, adapter.CenterCalls);
            Assert.AreEqual("individual", adapter.LastCenterMode);
            Assert.AreEqual(EventTypes.TaskCompleted, events[0].Event);
            Assert.AreEqual(Actions.CenterObjects, events[0].Payload.Value<string>("action"));
        }

        [TestMethod]
        public async Task CenterObjectsRejectsInvalidModeBeforeAdapterWork()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var service = new CenterObjectsService(adapter, new SelectionSnapshotService(adapter), new EventBus(), new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "center-2",
                Action = Actions.CenterObjects,
                Payload = new JObject { ["mode"] = "page" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.InvalidPayload, response.ErrorCode);
            Assert.AreEqual(0, adapter.CenterCalls);
        }

        [TestMethod]
        public async Task CenterObjectsReturnsEmptySelectionWhenSnapshotCannotResolve()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                CanResolveSnapshot = false
            };
            var service = new CenterObjectsService(adapter, new SelectionSnapshotService(adapter), new EventBus(), new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "center-3",
                Action = Actions.CenterObjects,
                Payload = new JObject { ["mode"] = "group" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.EmptySelection, response.ErrorCode);
            Assert.AreEqual(0, adapter.CenterCalls);
        }

        [TestMethod]
        public async Task CenterObjectsPublishesFailedEventWhenCancelled()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                ThrowOnCenter = new OperationCanceledException()
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new CenterObjectsService(adapter, new SelectionSnapshotService(adapter), eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "center-4",
                Action = Actions.CenterObjects,
                Payload = new JObject { ["mode"] = "group" }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.TaskCancelled, response.ErrorCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventTypes.TaskFailed, events[0].Event);
            Assert.AreEqual(ErrorCodes.TaskCancelled, events[0].Payload.Value<string>("errorCode"));
        }

        [TestMethod]
        public async Task CleanupRedundantRequiresConfirmationBeforeAdapterWork()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true);
            var service = new CleanupRedundantService(adapter, new EventBus(), new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "cleanup-1",
                Action = Actions.CleanupRedundant,
                Payload = new JObject { ["confirmed"] = false }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.InvalidPayload, response.ErrorCode);
            Assert.AreEqual(0, adapter.CleanupCalls);
        }

        [TestMethod]
        public async Task CleanupRedundantPublishesCompletedEvent()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                CleanupRemoved = 4
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new CleanupRedundantService(adapter, eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "cleanup-2",
                Action = Actions.CleanupRedundant,
                Payload = new JObject { ["confirmed"] = true }
            }, CancellationToken.None);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(4, response.Payload.Value<int>("removed"));
            Assert.AreEqual(1, adapter.CleanupCalls);
            Assert.AreEqual(EventTypes.TaskCompleted, events[0].Event);
            Assert.AreEqual(Actions.CleanupRedundant, events[0].Payload.Value<string>("action"));
            Assert.AreEqual(4, events[0].Payload.Value<int>("removed"));
        }

        [TestMethod]
        public async Task CleanupRedundantPublishesFailedEventWhenCancelled()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                ThrowOnCleanup = new OperationCanceledException()
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new CleanupRedundantService(adapter, eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "cleanup-3",
                Action = Actions.CleanupRedundant,
                Payload = new JObject { ["confirmed"] = true }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.TaskCancelled, response.ErrorCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventTypes.TaskFailed, events[0].Event);
            Assert.AreEqual(ErrorCodes.TaskCancelled, events[0].Payload.Value<string>("errorCode"));
        }

        [TestMethod]
        public async Task CleanupRedundantPublishesFailedEventWhenComFails()
        {
            var adapter = new FakeCorelDocumentAdapter(hasOpenDocument: true)
            {
                ThrowOnCleanup = new InvalidOperationException("COM failed.")
            };
            var eventBus = new EventBus();
            var events = new List<EventDto>();
            eventBus.EventPublished += (_, e) => events.Add(e);
            var service = new CleanupRedundantService(adapter, eventBus, new MemoryLogger());

            var response = await service.ExecuteAsync(new RequestDto
            {
                RequestId = "cleanup-4",
                Action = Actions.CleanupRedundant,
                Payload = new JObject { ["confirmed"] = true }
            }, CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.ComException, response.ErrorCode);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventTypes.TaskFailed, events[0].Event);
            Assert.AreEqual(ErrorCodes.ComException, events[0].Payload.Value<string>("errorCode"));
        }

        private sealed class FakeCorelDocumentAdapter : ICorelDocumentAdapter
        {
            public FakeCorelDocumentAdapter(bool hasOpenDocument)
            {
                HasOpenDocument = hasOpenDocument;
            }

            public bool HasOpenDocument { get; }
            public int ConvertCalls { get; private set; }
            public int CenterCalls { get; private set; }
            public int NormalizeCalls { get; private set; }
            public int CleanupCalls { get; private set; }
            public int CleanupRemoved { get; set; }
            public bool CanResolveSnapshot { get; set; } = true;
            public Exception? ThrowOnConvert { get; set; }
            public Exception? ThrowOnCenter { get; set; }
            public Exception? ThrowOnNormalize { get; set; }
            public Exception? ThrowOnCleanup { get; set; }
            public string? LastCenterMode { get; private set; }
            public NormalizeSizeOptions? LastNormalizeOptions { get; private set; }

            public Task<SelectionSnapshot> CaptureSelectionSnapshotAsync(CancellationToken token)
            {
                return Task.FromResult(new SelectionSnapshot(new[] { 1, 2, 3 }));
            }

            public Task<IReadOnlyList<CorelShapeSnapshot>> GetShapeSnapshotsAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, CancellationToken token)
            {
                IReadOnlyList<CorelShapeSnapshot> shapes = new[]
                {
                    new CorelShapeSnapshot(1, false, false),
                    new CorelShapeSnapshot(2, false, false),
                    new CorelShapeSnapshot(3, true, false)
                };
                return Task.FromResult(shapes);
            }

            public Task<bool> CanResolveSelectionSnapshotAsync(SelectionSnapshot selectionSnapshot, CancellationToken token)
            {
                return Task.FromResult(CanResolveSnapshot && selectionSnapshot.ShapeIds.Count > 0);
            }

            public Task<ConvertTextResult> ConvertTextAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, int batchSize, Action<ConvertTextResult> progress, CancellationToken token)
            {
                ConvertCalls++;
                if (ThrowOnConvert != null)
                {
                    throw ThrowOnConvert;
                }

                var result = new ConvertTextResult(2, 1, 3);
                progress(result);
                return Task.FromResult(result);
            }

            public Task<int> CenterObjectsAsync(SelectionSnapshot selectionSnapshot, string mode, CancellationToken token)
            {
                CenterCalls++;
                LastCenterMode = mode;
                if (ThrowOnCenter != null)
                {
                    throw ThrowOnCenter;
                }

                return Task.FromResult(selectionSnapshot.ShapeIds.Count);
            }

            public Task<int> NormalizeSizeAsync(SelectionSnapshot selectionSnapshot, NormalizeSizeOptions options, CancellationToken token)
            {
                NormalizeCalls++;
                LastNormalizeOptions = options;
                if (ThrowOnNormalize != null)
                {
                    throw ThrowOnNormalize;
                }

                return Task.FromResult(selectionSnapshot.ShapeIds.Count);
            }

            public Task<int> CleanupRedundantAsync(CancellationToken token)
            {
                CleanupCalls++;
                if (ThrowOnCleanup != null)
                {
                    throw ThrowOnCleanup;
                }

                return Task.FromResult(CleanupRemoved);
            }
        }
    }
}
