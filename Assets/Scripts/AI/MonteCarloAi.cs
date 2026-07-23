using ErkekTavlasi.Game;
using System;
using System.Collections.Generic;

namespace ErkekTavlasi.AI
{
    public class MonteCarloAi
    {
        public int RolloutsPerMove = 80;
        public int TrainingRolloutsPerDecision = 0;
        public int MaxPlayoutTurns = 420;
        public float Exploration = 0.04f;
        public float MemoryWeight = 0.35f;
        public float LearnedFeatureWeight = 0.55f;
        public float FeatureLearningRate = 0.0016f;
        public float FeatureL2 = 0.0004f;
        public float RandomTrainingMoveChance = 0.22f;
        public float RolloutRandomMoveChance = 0.35f;
        public float TrainingTemperature = 1.15f;
        public float TrainingPolicyNoise = 0.28f;
        public float TrainingUnvisitedBonus = 0.18f;

        private readonly Random random;
        private readonly MonteCarloMemory memory;
        private readonly MonteCarloWeights weights;

        public MonteCarloAi(MonteCarloMemory memory, MonteCarloWeights weights, int seed)
        {
            this.memory = memory;
            this.weights = weights;
            random = new Random(seed);
        }

        public MoveOption ChooseMove(GameState state, bool useFullRollouts)
        {
            List<MoveOption> options = AiMoveGenerator.GetTurnOptions(state);
            if (options.Count == 0)
            {
                return null;
            }

            int rollouts;
            if (useFullRollouts)
            {
                rollouts = RolloutsPerMove;
            }
            else
            {
                rollouts = TrainingRolloutsPerDecision;
            }
            PlayerColor aiPlayer = state.CurrentPlayer;
            string positionHash = PositionHasher.Hash(state);
            MoveOption best = options[0];
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < options.Count; i++)
            {
                MoveOption option = options[i];
                string moveKey = AiMoveKey.FromOption(option);
                MemoryEntry entry = memory.Get(positionHash, moveKey);
                if (rollouts <= 0)
                {
                    float trainingMemoryRate;
                    float unvisitedBonus;
                    if (entry == null)
                    {
                        trainingMemoryRate = 0.5f;
                        unvisitedBonus = 0.14f;
                    }
                    else
                    {
                        trainingMemoryRate = entry.WinRate;
                        unvisitedBonus = 0.08f / (float)Math.Sqrt(entry.Visits + 1);
                    }
                    float trainingScore = trainingMemoryRate
                        + unvisitedBonus
                        + LearnedFeatureScore(state, option)
                        + ExplorationNoise();
                    if (trainingScore > bestScore)
                    {
                        bestScore = trainingScore;
                        best = option;
                    }

                    continue;
                }

                int wins = 0;

                for (int rollout = 0; rollout < rollouts; rollout++)
                {
                    GameState simulation = state.Clone();
                    AiMoveGenerator.ApplyOption(simulation, option);
                    FinishDecisionTurnIfNeeded(simulation);
                    PlayerColor winner = PlayRandomGame(simulation);
                    if (winner == aiPlayer)
                    {
                        wins++;
                    }
                }

                float rolloutRate;
                if (rollouts <= 0)
                {
                    rolloutRate = 0.5f;
                }
                else
                {
                    rolloutRate = (float)wins / rollouts;
                }

                float memoryRate;
                float memoryConfidence;
                if (entry == null)
                {
                    memoryRate = 0.5f;
                    memoryConfidence = 0f;
                }
                else
                {
                    memoryRate = entry.WinRate;
                    memoryConfidence = Math.Min(1f, entry.Visits / 80f);
                }
                float score = rolloutRate * (1f - MemoryWeight * memoryConfidence)
                    + memoryRate * (MemoryWeight * memoryConfidence)
                    + LearnedFeatureScore(state, option)
                    + ExplorationNoise();

                if (score > bestScore)
                {
                    bestScore = score;
                    best = option;
                }
            }

            return best;
        }

        public TrainingReport TrainGames(int gameCount)
        {
            TrainingReport report = new TrainingReport();
            for (int i = 0; i < gameCount; i++)
            {
                GameTrainingResult result = TrainOneGame(true, PlayerColor.None);
                report.Games++;
                if (result.Winner == PlayerColor.White)
                {
                    report.WhiteWins++;
                }
                else if (result.Winner == PlayerColor.Black)
                {
                    report.BlackWins++;
                }
            }

            return report;
        }

        public TrainingReport TrainBo5Matches(int matchCount)
        {
            TrainingReport report = new TrainingReport();
            for (int match = 0; match < matchCount; match++)
            {
                int whiteScore = 0;
                int blackScore = 0;
                PlayerColor nextStarter = PlayerColor.None;

                while (whiteScore < 3 && blackScore < 3)
                {
                    GameTrainingResult result = TrainOneGame(nextStarter == PlayerColor.None, nextStarter);
                    nextStarter = result.Winner;
                    int points;
                    if (result.Mars)
                    {
                        points = 2;
                    }
                    else
                    {
                        points = 1;
                    }
                    if (result.Winner == PlayerColor.White)
                    {
                        whiteScore = Math.Min(3, whiteScore + points);
                        report.WhiteWins++;
                    }
                    else if (result.Winner == PlayerColor.Black)
                    {
                        blackScore = Math.Min(3, blackScore + points);
                        report.BlackWins++;
                    }

                    report.Games++;
                }

                report.Matches++;
            }

            return report;
        }

        public GameTrainingResult TrainSingleBo5Game(bool useOpeningRoll, PlayerColor forcedStarter)
        {
            return TrainOneGame(useOpeningRoll, forcedStarter);
        }

        private GameTrainingResult TrainOneGame(bool useOpeningRoll, PlayerColor forcedStarter)
        {
            GameState state = new GameState();
            if (useOpeningRoll)
            {
                state.RollOpeningDice(random);
            }
            else if (forcedStarter != PlayerColor.None)
            {
                state.CurrentPlayer = forcedStarter;
                state.DiceRemaining.Clear();
            }

            List<DecisionTrace> traces = new List<DecisionTrace>();
            int guard = 0;

            while (!state.IsGameOver() && guard < MaxPlayoutTurns)
            {
                guard++;
                if (state.DiceRemaining.Count == 0)
                {
                    state.RollDice(random);
                }

                List<MoveOption> options = AiMoveGenerator.GetTurnOptions(state);
                if (options.Count == 0)
                {
                    state.EndTurn();
                    continue;
                }

                string positionHash = PositionHasher.Hash(state);
                MoveOption option = ChooseSelfPlayMove(state, options, positionHash);
                if (option == null)
                {
                    state.EndTurn();
                    continue;
                }

                traces.Add(new DecisionTrace
                {
                    Player = state.CurrentPlayer,
                    PositionHash = positionHash,
                    MoveKey = AiMoveKey.FromOption(option),
                    Features = MoveFeatureExtractor.Extract(state, option)
                });

                AiMoveGenerator.ApplyOption(state, option);
                FinishDecisionTurnIfNeeded(state);
            }

            PlayerColor winner = state.GetWinner();
            if (winner == PlayerColor.None)
            {
                winner = EstimateLeader(state);
            }

            for (int i = 0; i < traces.Count; i++)
            {
                DecisionTrace trace = traces[i];
                memory.Record(trace.PositionHash, trace.MoveKey, trace.Player == winner);
                if (weights != null && trace.Features != null)
                {
                    float target;
                    if (trace.Player == winner)
                    {
                        target = 1f;
                    }
                    else
                    {
                        target = -1f;
                    }
                    weights.Update(trace.Features, target, FeatureLearningRate, FeatureL2);
                }
            }

            return new GameTrainingResult
            {
                Winner = winner,
                Mars = IsMars(state, winner)
            };
        }

        private PlayerColor PlayRandomGame(GameState state)
        {
            int guard = 0;
            while (!state.IsGameOver() && guard < MaxPlayoutTurns)
            {
                guard++;
                if (state.DiceRemaining.Count == 0)
                {
                    state.RollDice(random);
                }

                List<MoveOption> options = AiMoveGenerator.GetTurnOptions(state);
                if (options.Count == 0)
                {
                    state.EndTurn();
                    continue;
                }

                MoveOption option = ChooseRolloutMove(state, options);
                AiMoveGenerator.ApplyOption(state, option);
                FinishDecisionTurnIfNeeded(state);
            }

            PlayerColor winner = state.GetWinner();
            if (winner == PlayerColor.None)
            {
                return EstimateLeader(state);
            }

            return winner;
        }

        private static void FinishDecisionTurnIfNeeded(GameState state)
        {
            if (!state.IsGameOver() && (state.DiceRemaining.Count == 0 || state.GetLegalMoves().Count == 0))
            {
                state.EndTurn();
            }
        }

        private PlayerColor EstimateLeader(GameState state)
        {
            if (state.WhiteOff != state.BlackOff)
            {
                if (state.WhiteOff > state.BlackOff)
                {
                    return PlayerColor.White;
                }

                return PlayerColor.Black;
            }

            int whiteDistance = DistanceToBearOff(state, PlayerColor.White);
            int blackDistance = DistanceToBearOff(state, PlayerColor.Black);
            if (whiteDistance == blackDistance)
            {
                if (random.Next(2) == 0)
                {
                    return PlayerColor.White;
                }

                return PlayerColor.Black;
            }

            if (whiteDistance < blackDistance)
            {
                return PlayerColor.White;
            }

            return PlayerColor.Black;
        }

        private static int DistanceToBearOff(GameState state, PlayerColor player)
        {
            int distance = state.GetBarCount(player) * 25;
            for (int point = 0; point < state.Points.Length; point++)
            {
                PointStack stack = state.Points[point];
                if (stack.Owner != player)
                {
                    continue;
                }

                int steps;
                if (player == PlayerColor.White)
                {
                    steps = point + 1;
                }
                else
                {
                    steps = 24 - point;
                }
                distance += steps * stack.Count;
            }

            return distance;
        }

        private float ExplorationNoise()
        {
            return (float)(random.NextDouble() * Exploration);
        }

        private MoveOption ChooseSelfPlayMove(GameState state, List<MoveOption> options, string positionHash)
        {
            if (options.Count == 0)
            {
                return null;
            }

            if (random.NextDouble() < RandomTrainingMoveChance)
            {
                return options[random.Next(options.Count)];
            }

            float[] scores = new float[options.Count];
            float maxScore = float.NegativeInfinity;
            for (int i = 0; i < options.Count; i++)
            {
                MoveOption option = options[i];
                string moveKey = AiMoveKey.FromOption(option);
                MemoryEntry entry = memory.Get(positionHash, moveKey);
                float score = LearnedFeatureScore(state, option);

                if (entry == null)
                {
                    score += 0.5f + TrainingUnvisitedBonus;
                }
                else
                {
                    score += entry.WinRate;
                    score += TrainingUnvisitedBonus / (float)Math.Sqrt(entry.Visits + 1);
                }

                score += (float)((random.NextDouble() * 2.0 - 1.0) * TrainingPolicyNoise);
                scores[i] = score;
                if (score > maxScore)
                {
                    maxScore = score;
                }
            }

            return SampleByTemperature(options, scores, maxScore, TrainingTemperature);
        }

        private MoveOption ChooseRolloutMove(GameState state, List<MoveOption> options)
        {
            if (options.Count == 0)
            {
                return null;
            }

            if (random.NextDouble() < RolloutRandomMoveChance)
            {
                return options[random.Next(options.Count)];
            }

            string positionHash = PositionHasher.Hash(state);
            MoveOption best = options[0];
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < options.Count; i++)
            {
                MoveOption option = options[i];
                string moveKey = AiMoveKey.FromOption(option);
                MemoryEntry entry = memory.Get(positionHash, moveKey);
                float score = LearnedFeatureScore(state, option);
                if (entry == null)
                {
                    score += 0.5f;
                }
                else
                {
                    score += entry.WinRate;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = option;
                }
            }

            return best;
        }

        private MoveOption SampleByTemperature(List<MoveOption> options, float[] scores, float maxScore, float temperature)
        {
            if (temperature <= 0.01f)
            {
                int bestIndex = 0;
                for (int i = 1; i < scores.Length; i++)
                {
                    if (scores[i] > scores[bestIndex])
                    {
                        bestIndex = i;
                    }
                }

                return options[bestIndex];
            }

            double total = 0.0;
            double[] weightsByOption = new double[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                double weight = Math.Exp((scores[i] - maxScore) / temperature);
                weightsByOption[i] = weight;
                total += weight;
            }

            if (total <= 0.0)
            {
                return options[random.Next(options.Count)];
            }

            double pick = random.NextDouble() * total;
            double running = 0.0;
            for (int i = 0; i < weightsByOption.Length; i++)
            {
                running += weightsByOption[i];
                if (pick <= running)
                {
                    return options[i];
                }
            }

            return options[options.Count - 1];
        }

        private float LearnedFeatureScore(GameState state, MoveOption option)
        {
            if (weights == null || LearnedFeatureWeight <= 0f)
            {
                return 0f;
            }

            MoveFeatureVector features = MoveFeatureExtractor.Extract(state, option);
            return (float)Math.Tanh(weights.Score(features)) * LearnedFeatureWeight;
        }

        private static bool IsMars(GameState state, PlayerColor winner)
        {
            if (winner == PlayerColor.White)
            {
                return state.BlackOff == 0;
            }

            if (winner == PlayerColor.Black)
            {
                return state.WhiteOff == 0;
            }

            return false;
        }

        private class DecisionTrace
        {
            public PlayerColor Player;
            public string PositionHash;
            public string MoveKey;
            public MoveFeatureVector Features;
        }
    }

    public struct TrainingReport
    {
        public int Matches;
        public int Games;
        public int WhiteWins;
        public int BlackWins;
    }

    public struct GameTrainingResult
    {
        public PlayerColor Winner;
        public bool Mars;
    }
}
