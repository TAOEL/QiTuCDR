namespace QiTuCDR.Host.Environment
{
    public sealed class RuntimeCheckItem
    {
        public RuntimeCheckItem(string name, bool passed, RuntimeCheckSeverity severity, string message)
        {
            Name = name;
            Passed = passed;
            Severity = severity;
            Message = message;
        }

        public string Name { get; }
        public bool Passed { get; }
        public RuntimeCheckSeverity Severity { get; }
        public string Message { get; }
    }
}
