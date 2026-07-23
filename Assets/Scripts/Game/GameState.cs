using System;
using System.Collections.Generic;

namespace ErkekTavlasi.Game
{
    public class GameState
    {
        public PointStack[] Points;
        public PlayerColor CurrentPlayer;
        public List<int> DiceRemaining;
        public int WhiteBar;
        public int BlackBar;
        public int WhiteOff;
        public int BlackOff;

        public GameState()
        {
            Points = new PointStack[24];
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i] = new PointStack();
            }

            DiceRemaining = new List<int>();
            StartNewGame();
        }

        public void StartNewGame()
        {
            ClearBoard();

            PutCheckers(23, PlayerColor.White, 2);
            PutCheckers(12, PlayerColor.White, 5);
            PutCheckers(7, PlayerColor.White, 3);
            PutCheckers(5, PlayerColor.White, 5);

            PutCheckers(0, PlayerColor.Black, 2);
            PutCheckers(11, PlayerColor.Black, 5);
            PutCheckers(16, PlayerColor.Black, 3);
            PutCheckers(18, PlayerColor.Black, 5);

            WhiteBar = 0;
            BlackBar = 0;
            WhiteOff = 0;
            BlackOff = 0;
            CurrentPlayer = PlayerColor.White;
            DiceRemaining.Clear();
        }

        public GameState Clone()
        {
            GameState copy = new GameState();
            for (int i = 0; i < Points.Length; i++)
            {
                copy.Points[i] = Points[i].Clone();
            }

            copy.CurrentPlayer = CurrentPlayer;
            copy.DiceRemaining = new List<int>(DiceRemaining);
            copy.WhiteBar = WhiteBar;
            copy.BlackBar = BlackBar;
            copy.WhiteOff = WhiteOff;
            copy.BlackOff = BlackOff;
            return copy;
        }

        public void RollDice(Random random)
        {
            int first = random.Next(1, 7);
            int second = random.Next(1, 7);
            SetDice(first, second);
        }

        public void SetOpeningRoll(int whiteRoll, int blackRoll)
        {
            if (whiteRoll == blackRoll)
            {
                DiceRemaining.Clear();
                return;
            }

            if (whiteRoll > blackRoll)
            {
                CurrentPlayer = PlayerColor.White;
            }
            else
            {
                CurrentPlayer = PlayerColor.Black;
            }
            DiceRemaining.Clear();
        }

        public void RollOpeningDice(Random random)
        {
            int whiteRoll;
            int blackRoll;
            do
            {
                whiteRoll = random.Next(1, 7);
                blackRoll = random.Next(1, 7);
            }
            while (whiteRoll == blackRoll);

            SetOpeningRoll(whiteRoll, blackRoll);
        }

        public void SetDice(int first, int second)
        {
            DiceRemaining.Clear();
            if (first == second)
            {
                for (int i = 0; i < 4; i++)
                {
                    DiceRemaining.Add(first);
                }
            }
            else
            {
                DiceRemaining.Add(first);
                DiceRemaining.Add(second);
            }
        }

        public List<BackgammonMove> GetLegalMoves()
        {
            List<BackgammonMove> moves = new List<BackgammonMove>();
            for (int i = 0; i < DiceRemaining.Count; i++)
            {
                AddLegalMovesForDie(moves, DiceRemaining[i]);
            }

            return moves;
        }

        public List<BackgammonMove> GetLegalMovesFrom(int fromPoint)
        {
            List<BackgammonMove> allMoves = GetLegalMoves();
            List<BackgammonMove> result = new List<BackgammonMove>();

            for (int i = 0; i < allMoves.Count; i++)
            {
                if (allMoves[i].FromPoint == fromPoint)
                {
                    result.Add(allMoves[i]);
                }
            }

            return result;
        }

        public void ApplyMove(BackgammonMove move)
        {
            RemoveChecker(move.FromPoint);

            if (move.BearsOff)
            {
                if (CurrentPlayer == PlayerColor.White)
                {
                    WhiteOff++;
                }
                else
                {
                    BlackOff++;
                }
            }
            else
            {
                PointStack target = Points[move.ToPoint];
                PlayerColor opponent = GetOpponent(CurrentPlayer);

                if (target.Owner == opponent && target.Count == 1)
                {
                    target.Owner = PlayerColor.None;
                    target.Count = 0;

                    if (opponent == PlayerColor.White)
                    {
                        WhiteBar++;
                    }
                    else
                    {
                        BlackBar++;
                    }
                }

                AddChecker(move.ToPoint, CurrentPlayer);
            }

            RemoveOneDie(move.Die);
        }

        public bool IsGameOver()
        {
            return WhiteOff == 15 || BlackOff == 15;
        }

        public PlayerColor GetWinner()
        {
            if (WhiteOff == 15)
            {
                return PlayerColor.White;
            }

            if (BlackOff == 15)
            {
                return PlayerColor.Black;
            }

            return PlayerColor.None;
        }

        public void EndTurn()
        {
            DiceRemaining.Clear();
            CurrentPlayer = GetOpponent(CurrentPlayer);
        }

        public int GetBarCount(PlayerColor player)
        {
            if (player == PlayerColor.White)
            {
                return WhiteBar;
            }

            return BlackBar;
        }

        public int GetOffCount(PlayerColor player)
        {
            if (player == PlayerColor.White)
            {
                return WhiteOff;
            }

            return BlackOff;
        }

        public static PlayerColor GetOpponent(PlayerColor player)
        {
            if (player == PlayerColor.White)
            {
                return PlayerColor.Black;
            }

            return PlayerColor.White;
        }

        private void AddLegalMovesForDie(List<BackgammonMove> moves, int die)
        {
            if (GetBarCount(CurrentPlayer) > 0)
            {
                BackgammonMove barMove = CreateMove(BackgammonMove.FromBar, die);
                if (barMove != null)
                {
                    moves.Add(barMove);
                }

                return;
            }

            for (int point = 0; point < Points.Length; point++)
            {
                if (Points[point].Owner == CurrentPlayer && Points[point].Count > 0)
                {
                    BackgammonMove move = CreateMove(point, die);
                    if (move != null)
                    {
                        moves.Add(move);
                    }
                }
            }
        }

        private BackgammonMove CreateMove(int fromPoint, int die)
        {
            int toPoint = GetTargetPoint(fromPoint, die);

            if (toPoint == BackgammonMove.ToOffBoard)
            {
                if (CanBearOffFrom(fromPoint, die))
                {
                    return new BackgammonMove
                    {
                        FromPoint = fromPoint,
                        ToPoint = BackgammonMove.ToOffBoard,
                        Die = die,
                        BearsOff = true
                    };
                }

                return null;
            }

            if (toPoint < 0 || toPoint >= Points.Length)
            {
                return null;
            }

            PointStack target = Points[toPoint];
            PlayerColor opponent = GetOpponent(CurrentPlayer);
            if (target.Owner == opponent && target.Count >= 2)
            {
                return null;
            }

            return new BackgammonMove
            {
                FromPoint = fromPoint,
                ToPoint = toPoint,
                Die = die,
                HitsOpponent = target.Owner == opponent && target.Count == 1
            };
        }

        private int GetTargetPoint(int fromPoint, int die)
        {
            if (fromPoint == BackgammonMove.FromBar)
            {
                if (CurrentPlayer == PlayerColor.White)
                {
                    return 24 - die;
                }

                return die - 1;
            }

            if (CurrentPlayer == PlayerColor.White)
            {
                int target = fromPoint - die;
                if (target < 0)
                {
                    return BackgammonMove.ToOffBoard;
                }

                return target;
            }

            int blackTarget = fromPoint + die;
            if (blackTarget > 23)
            {
                return BackgammonMove.ToOffBoard;
            }

            return blackTarget;
        }

        private bool CanBearOffFrom(int fromPoint, int die)
        {
            if (!AllCheckersAreHome(CurrentPlayer))
            {
                return false;
            }

            if (CurrentPlayer == PlayerColor.White)
            {
                int exactTarget = fromPoint - die;
                if (exactTarget == -1)
                {
                    return true;
                }

                for (int point = fromPoint + 1; point <= 5; point++)
                {
                    if (Points[point].Owner == PlayerColor.White && Points[point].Count > 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            int blackExactTarget = fromPoint + die;
            if (blackExactTarget == 24)
            {
                return true;
            }

            for (int point = 18; point < fromPoint; point++)
            {
                if (Points[point].Owner == PlayerColor.Black && Points[point].Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllCheckersAreHome(PlayerColor player)
        {
            if (GetBarCount(player) > 0)
            {
                return false;
            }

            if (player == PlayerColor.White)
            {
                for (int point = 6; point < Points.Length; point++)
                {
                    if (Points[point].Owner == PlayerColor.White && Points[point].Count > 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            for (int point = 0; point < 18; point++)
            {
                if (Points[point].Owner == PlayerColor.Black && Points[point].Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveChecker(int point)
        {
            if (point == BackgammonMove.FromBar)
            {
                if (CurrentPlayer == PlayerColor.White)
                {
                    WhiteBar--;
                }
                else
                {
                    BlackBar--;
                }

                return;
            }

            Points[point].Count--;
            if (Points[point].Count == 0)
            {
                Points[point].Owner = PlayerColor.None;
            }
        }

        private void AddChecker(int point, PlayerColor player)
        {
            Points[point].Owner = player;
            Points[point].Count++;
        }

        private void RemoveOneDie(int die)
        {
            for (int i = 0; i < DiceRemaining.Count; i++)
            {
                if (DiceRemaining[i] == die)
                {
                    DiceRemaining.RemoveAt(i);
                    return;
                }
            }
        }

        private void ClearBoard()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i].Owner = PlayerColor.None;
                Points[i].Count = 0;
            }
        }

        private void PutCheckers(int point, PlayerColor player, int count)
        {
            Points[point].Owner = player;
            Points[point].Count = count;
        }
    }
}
