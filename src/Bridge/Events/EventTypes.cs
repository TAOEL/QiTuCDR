namespace QiTuCDR.Bridge.Events
{
    public static class EventTypes
    {
        public const string TaskProgress = "task.progress";
        public const string TaskCompleted = "task.completed";
        public const string TaskFailed = "task.failed";
        public const string StateChanged = "plugin.stateChanged";
        public const string Recovery = "plugin.recovery";
        public const string DocumentChanged = "host.documentChanged";
        public const string SelectionChanged = "host.selectionChanged";
    }
}
