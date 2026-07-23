namespace ErkekTavlasi.Game
{
    public class BackgammonMove
    {
        public const int FromBar = -1;
        public const int ToOffBoard = -2;

        public int FromPoint;
        public int ToPoint;
        public int Die;
        public bool HitsOpponent;
        public bool BearsOff;

        public BackgammonMove Clone()
        {
            return new BackgammonMove
            {
                FromPoint = FromPoint,
                ToPoint = ToPoint,
                Die = Die,
                HitsOpponent = HitsOpponent,
                BearsOff = BearsOff
            };
        }
    }
}
