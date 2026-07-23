using System.Collections.Generic;

namespace ErkekTavlasi.Game
{
    public static class MoveOptionFinder
    {
        public static List<MoveOption> GetOptions(GameState state, int selectedPoint)
        {
            List<MoveOption> options = new List<MoveOption>();
            if (selectedPoint == int.MinValue)
            {
                return options;
            }

            BuildOptions(state.Clone(), selectedPoint, new List<BackgammonMove>(), options);
            return PreferLongestOptions(options);
        }

        private static void BuildOptions(GameState state, int fromPoint, List<BackgammonMove> steps, List<MoveOption> options)
        {
            List<BackgammonMove> moves = state.GetLegalMovesFrom(fromPoint);
            for (int i = 0; i < moves.Count; i++)
            {
                BackgammonMove move = moves[i].Clone();
                GameState nextState = state.Clone();
                nextState.ApplyMove(move);

                List<BackgammonMove> nextSteps = new List<BackgammonMove>(steps);
                nextSteps.Add(move);

                MoveOption option = new MoveOption
                {
                    TargetPoint = move.ToPoint,
                    BearsOff = move.BearsOff,
                    Steps = nextSteps
                };
                options.Add(option);

                if (!move.BearsOff && nextState.DiceRemaining.Count > 0)
                {
                    BuildOptions(nextState, move.ToPoint, nextSteps, options);
                }
            }
        }

        private static List<MoveOption> PreferLongestOptions(List<MoveOption> options)
        {
            Dictionary<string, MoveOption> best = new Dictionary<string, MoveOption>();
            for (int i = 0; i < options.Count; i++)
            {
                string key;
                if (options[i].BearsOff)
                {
                    key = "off";
                }
                else
                {
                    key = options[i].TargetPoint.ToString();
                }
                if (!best.ContainsKey(key) || options[i].Steps.Count > best[key].Steps.Count)
                {
                    best[key] = options[i];
                }
            }

            return new List<MoveOption>(best.Values);
        }
    }
}
