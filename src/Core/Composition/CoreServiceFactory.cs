using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Core.Tools;
using QiTuCDR.Core.Tools.Center;
using QiTuCDR.Core.Tools.Cleanup;
using QiTuCDR.Core.Tools.ConvertText;
using QiTuCDR.Core.Tools.Normalize;
using QiTuCDR.Core.Validators;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Infrastructure.Tasks;

namespace QiTuCDR.Core.Composition
{
    public sealed class CoreServiceFactory
    {
        public CoreServiceFactory(
            ICorelDocumentAdapter documentAdapter,
            PluginStateMachine stateMachine,
            TaskCancellationHub cancellationHub,
            EventBus eventBus,
            PluginConfig config,
            ILogger logger)
        {
            DocumentAdapter = documentAdapter;
            StateMachine = stateMachine;
            CancellationHub = cancellationHub;
            EventBus = eventBus;
            Config = config;
            Logger = logger;
        }

        public ICorelDocumentAdapter DocumentAdapter { get; }
        public PluginStateMachine StateMachine { get; }
        public TaskCancellationHub CancellationHub { get; }
        public EventBus EventBus { get; }
        public PluginConfig Config { get; }
        public ILogger Logger { get; }

        public IBridgeCommand[] CreateCommands()
        {
            var validator = new ToolRequestValidator();
            var selectionSnapshotService = new SelectionSnapshotService(DocumentAdapter);
            var convertTextService = new ConvertTextService(DocumentAdapter, selectionSnapshotService, validator, EventBus, Config, Logger);
            var centerObjectsService = new CenterObjectsService(DocumentAdapter, selectionSnapshotService, EventBus, Logger);
            var cleanupService = new CleanupRedundantService(DocumentAdapter, EventBus, Logger);
            var normalizeService = new NormalizeSizeService(DocumentAdapter, selectionSnapshotService, EventBus, Logger);

            return new IBridgeCommand[]
            {
                new EchoCommand(),
                new GetStateCommand(StateMachine),
                new CancelCurrentTaskCommand(CancellationHub),
                new ConvertTextCommand(convertTextService),
                new CenterObjectsCommand(centerObjectsService),
                new CleanupRedundantCommand(cleanupService),
                new NormalizeSizeCommand(normalizeService)
            };
        }
    }
}
