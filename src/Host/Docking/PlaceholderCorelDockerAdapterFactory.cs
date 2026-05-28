namespace QiTuCDR.Host.Docking
{
    public sealed class PlaceholderCorelDockerAdapterFactory : ICorelDockerAdapterFactory
    {
        public ICorelDockerAdapter Create()
        {
            return new PlaceholderCorelDockerAdapter();
        }
    }
}
