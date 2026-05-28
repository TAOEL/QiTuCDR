namespace QiTuCDR.Core.Host
{
    public sealed class NullCorelHostContext : ICorelHostContext
    {
        public dynamic? Application => null;
        public bool HasOpenDocument => false;
    }
}
