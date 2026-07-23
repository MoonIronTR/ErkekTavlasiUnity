using ErkekTavlasi.Game;
using System.Text;

namespace ErkekTavlasi.AI
{
    public static class PositionHasher
    {
        public static string Hash(GameState state)
        {
            StringBuilder builder = new StringBuilder(180);
            if (state.CurrentPlayer == PlayerColor.White)
            {
                builder.Append('W');
            }
            else
            {
                builder.Append('B');
            }
            builder.Append("|D:");
            for (int i = 0; i < state.DiceRemaining.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(state.DiceRemaining[i]);
            }

            builder.Append("|BAR:");
            builder.Append(state.WhiteBar);
            builder.Append(',');
            builder.Append(state.BlackBar);
            builder.Append("|OFF:");
            builder.Append(state.WhiteOff);
            builder.Append(',');
            builder.Append(state.BlackOff);
            builder.Append("|P:");

            for (int point = 0; point < state.Points.Length; point++)
            {
                PointStack stack = state.Points[point];
                char owner;
                if (stack.Owner == PlayerColor.White)
                {
                    owner = 'W';
                }
                else if (stack.Owner == PlayerColor.Black)
                {
                    owner = 'B';
                }
                else
                {
                    owner = '-';
                }
                builder.Append(owner);
                builder.Append(stack.Count);
                builder.Append(';');
            }

            return builder.ToString();
        }
    }
}
