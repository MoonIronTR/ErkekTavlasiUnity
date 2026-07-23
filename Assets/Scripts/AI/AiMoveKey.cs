using ErkekTavlasi.Game;
using System.Text;

namespace ErkekTavlasi.AI
{
    public static class AiMoveKey
    {
        public static string FromOption(MoveOption option)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < option.Steps.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                BackgammonMove move = option.Steps[i];
                builder.Append(move.FromPoint);
                builder.Append('/');
                builder.Append(move.ToPoint);
                builder.Append('/');
                builder.Append(move.Die);
            }

            return builder.ToString();
        }
    }
}
