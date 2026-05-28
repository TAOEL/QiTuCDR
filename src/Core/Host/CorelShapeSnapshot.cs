namespace QiTuCDR.Core.Host
{
    public sealed class CorelShapeSnapshot
    {
        public CorelShapeSnapshot(int id, bool isLocked, bool isHidden)
        {
            Id = id;
            IsLocked = isLocked;
            IsHidden = isHidden;
        }

        public int Id { get; }
        public bool IsLocked { get; }
        public bool IsHidden { get; }
    }
}
