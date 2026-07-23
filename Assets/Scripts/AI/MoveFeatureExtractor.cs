using ErkekTavlasi.Game;

namespace ErkekTavlasi.AI
{
    public static class MoveFeatureExtractor
    {
        public static MoveFeatureVector Extract(GameState before, MoveOption option)
        {
            PlayerColor player = before.CurrentPlayer;
            PlayerColor opponent = GameState.GetOpponent(player);
            GameState after = before.Clone();
            AiMoveGenerator.ApplyOption(after, option);

            MoveFeatureVector vector = new MoveFeatureVector();
            float[] values = vector.Values;
            int index = 0;
            values[index] = 1f;
            index++;

            AddBoardBuckets(values, ref index, before, player, player);
            AddBoardBuckets(values, ref index, before, player, opponent);
            AddBoardBuckets(values, ref index, after, player, player);
            AddBoardBuckets(values, ref index, after, player, opponent);
            AddDeltaBuckets(values, ref index, before, after, player, player);
            AddDeltaBuckets(values, ref index, before, after, player, opponent);
            AddDiceCounts(values, ref index, option);
            AddFromSlot(values, ref index, player, option);
            AddToSlot(values, ref index, player, option);

            values[index] = option.Steps.Count / 4f;
            index++;
            values[index] = before.GetBarCount(player) / 15f;
            index++;
            values[index] = before.GetBarCount(opponent) / 15f;
            index++;
            values[index] = before.GetOffCount(player) / 15f;
            index++;
            values[index] = before.GetOffCount(opponent) / 15f;
            index++;
            values[index] = after.GetBarCount(player) / 15f;
            index++;
            values[index] = after.GetBarCount(opponent) / 15f;
            index++;
            values[index] = after.GetOffCount(player) / 15f;
            index++;
            values[index] = after.GetOffCount(opponent) / 15f;
            return vector;
        }

        private static void AddBoardBuckets(float[] values, ref int index, GameState state, PlayerColor perspective, PlayerColor owner)
        {
            for (int perspectivePoint = 0; perspectivePoint < MoveFeatureVector.PointCount; perspectivePoint++)
            {
                int boardPoint = ToBoardPoint(perspective, perspectivePoint);
                int count = 0;
                if (state.Points[boardPoint].Owner == owner)
                {
                    count = state.Points[boardPoint].Count;
                }

                AddCountBucket(values, ref index, count);
            }
        }

        private static void AddDeltaBuckets(float[] values, ref int index, GameState before, GameState after, PlayerColor perspective, PlayerColor owner)
        {
            for (int perspectivePoint = 0; perspectivePoint < MoveFeatureVector.PointCount; perspectivePoint++)
            {
                int boardPoint = ToBoardPoint(perspective, perspectivePoint);
                int beforeCount = 0;
                int afterCount = 0;
                if (before.Points[boardPoint].Owner == owner)
                {
                    beforeCount = before.Points[boardPoint].Count;
                }

                if (after.Points[boardPoint].Owner == owner)
                {
                    afterCount = after.Points[boardPoint].Count;
                }

                AddDeltaBucket(values, ref index, afterCount - beforeCount);
            }
        }

        private static void AddCountBucket(float[] values, ref int index, int count)
        {
            int bucket = count;
            if (bucket < 0)
            {
                bucket = 0;
            }
            else if (bucket >= MoveFeatureVector.CountBucketCount)
            {
                bucket = MoveFeatureVector.CountBucketCount - 1;
            }

            for (int i = 0; i < MoveFeatureVector.CountBucketCount; i++)
            {
                if (i == bucket)
                {
                    values[index] = 1f;
                }
                else
                {
                    values[index] = 0f;
                }
                index++;
            }
        }

        private static void AddDeltaBucket(float[] values, ref int index, int delta)
        {
            int bucket = delta + 5;
            if (bucket < 0)
            {
                bucket = 0;
            }
            else if (bucket >= MoveFeatureVector.DeltaBucketCount)
            {
                bucket = MoveFeatureVector.DeltaBucketCount - 1;
            }

            for (int i = 0; i < MoveFeatureVector.DeltaBucketCount; i++)
            {
                if (i == bucket)
                {
                    values[index] = 1f;
                }
                else
                {
                    values[index] = 0f;
                }
                index++;
            }
        }

        private static void AddDiceCounts(float[] values, ref int index, MoveOption option)
        {
            for (int die = 1; die <= 6; die++)
            {
                int count = 0;
                for (int i = 0; i < option.Steps.Count; i++)
                {
                    if (option.Steps[i].Die == die)
                    {
                        count++;
                    }
                }

                values[index] = count / 4f;
                index++;
            }
        }

        private static void AddFromSlot(float[] values, ref int index, PlayerColor perspective, MoveOption option)
        {
            int slot = 0;
            if (option.Steps.Count > 0 && option.Steps[0].FromPoint != BackgammonMove.FromBar)
            {
                slot = 1 + ToPerspectivePoint(perspective, option.Steps[0].FromPoint);
            }

            AddSlot(values, ref index, slot, MoveFeatureVector.FromSlotCount);
        }

        private static void AddToSlot(float[] values, ref int index, PlayerColor perspective, MoveOption option)
        {
            int slot = 0;
            if (!option.BearsOff)
            {
                slot = 1 + ToPerspectivePoint(perspective, option.TargetPoint);
            }

            AddSlot(values, ref index, slot, MoveFeatureVector.ToSlotCount);
        }

        private static void AddSlot(float[] values, ref int index, int activeSlot, int slotCount)
        {
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (slot == activeSlot)
                {
                    values[index] = 1f;
                }
                else
                {
                    values[index] = 0f;
                }
                index++;
            }
        }

        private static int ToBoardPoint(PlayerColor perspective, int perspectivePoint)
        {
            if (perspective == PlayerColor.White)
            {
                return perspectivePoint;
            }

            return 23 - perspectivePoint;
        }

        private static int ToPerspectivePoint(PlayerColor perspective, int boardPoint)
        {
            if (perspective == PlayerColor.White)
            {
                return boardPoint;
            }

            return 23 - boardPoint;
        }
    }
}
