namespace QiTuCDR.Host.Docking
{
    public sealed class CorelDockerAdapterFactory : ICorelDockerAdapterFactory
    {
        public ICorelDockerAdapter Create()
        {
            return new CorelDockerAdapter();
        }
    }
}
