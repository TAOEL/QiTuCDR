using QiTuCDR.Core.Host;

namespace QiTuCDR.Host.Environment
{
    public sealed class CorelHostContext : ICorelHostContext
    {
        public CorelHostContext(dynamic? application)
        {
            Application = application;
        }

        public dynamic? Application { get; }

        public bool HasOpenDocument
        {
            get
            {
                try
                {
                    return Application?.ActiveDocument != null;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
