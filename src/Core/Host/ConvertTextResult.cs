namespace QiTuCDR.Core.Host
{
    public sealed class ConvertTextResult
    {
        public ConvertTextResult(int converted, int skipped, int total)
        {
            Converted = converted;
            Skipped = skipped;
            Total = total;
        }

        public int Converted { get; }
        public int Skipped { get; }
        public int Total { get; }
    }
}
