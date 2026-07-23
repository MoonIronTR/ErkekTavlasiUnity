namespace ErkekTavlasi.Game
{
    public class PointStack
    {
        public PlayerColor Owner;
        public int Count;

        public PointStack()
        {
            Owner = PlayerColor.None;
            Count = 0;
        }

        public PointStack Clone()
        {
            return new PointStack
            {
                Owner = Owner,
                Count = Count
            };
        }
    }
}
