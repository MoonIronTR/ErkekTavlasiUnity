using System.Collections.Generic;

namespace ErkekTavlasi.Game
{
    public class MoveOption
    {
        public int TargetPoint;
        public bool BearsOff;
        public List<BackgammonMove> Steps = new List<BackgammonMove>();
    }
}
