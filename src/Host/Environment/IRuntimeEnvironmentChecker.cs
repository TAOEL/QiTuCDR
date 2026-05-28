namespace QiTuCDR.Host.Environment
{
    public interface IRuntimeEnvironmentChecker
    {
        RuntimeCheckResult Check(object? corelApplication);
    }
}
