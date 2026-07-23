using ErkekTavlasi.Game;
using System.Collections.Generic;

namespace ErkekTavlasi.AI
{
    public static class AiMoveGenerator
    {
        public static List<MoveOption> GetTurnOptions(GameState state)
        {
            List<MoveOption> options = new List<MoveOption>();
            if (state.DiceRemaining.Count == 0)
            {
                return options;
            }

            if (state.GetBarCount(state.CurrentPlayer) > 0)
            {
                AddOptionsFrom(state, BackgammonMove.FromBar, options);
                return options;
            }

            for (int point = 0; point < state.Points.Length; point++)
            {
                PointStack stack = state.Points[point];
                if (stack.Owner == state.CurrentPlayer && stack.Count > 0)
                {
                    AddOptionsFrom(state, point, options);
                }
            }

            return options;
        }

        public static void ApplyOption(GameState state, MoveOption option)
        {
            for (int i = 0; i < option.Steps.Count; i++)
            {
                state.ApplyMove(option.Steps[i]);
            }
        }

        private static void AddOptionsFrom(GameState state, int fromPoint, List<MoveOption> options)
        {
            List<MoveOption> pointOptions = MoveOptionFinder.GetOptions(state, fromPoint);
            for (int i = 0; i < pointOptions.Count; i++)
            {
                options.Add(pointOptions[i]);
            }
        }
    }
}
