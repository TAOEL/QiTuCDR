namespace QiTuCDR.Core.Host
{
    public interface ICorelHostContext
    {
        dynamic? Application { get; }
        bool HasOpenDocument { get; }
    }
}
