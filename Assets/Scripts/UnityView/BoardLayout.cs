using ErkekTavlasi.Game;
using UnityEngine;

namespace ErkekTavlasi.UnityView
{
    public static class BoardLayout
    {
        public const float PointWidth = 0.74f;
        public const float BoardLength = 13.4f;
        public const float BoardDepth = 10.5f;
        public const float BoardHeight = 0.34f;
        public const float LeftHalfStart = -4.85f;
        public const float RightHalfStart = 0.42f;
        public const float BottomBaseZ = -4.40f;
        public const float TopBaseZ = 4.40f;
        public const float CheckerRadius = 0.34f;
        public const float CheckerHeight = 0.16f;
        public const float CenterBarX = -0.08f;
        public const float BearOffX = 5.64f;

        public static int PointToColumn(int point)
        {
            if (point <= 11)
            {
                return 11 - point;
            }

            return point - 12;
        }

        public static float PointX(int point)
        {
            int column = PointToColumn(point);
            if (column < 6)
            {
                return LeftHalfStart + column * PointWidth + PointWidth * 0.5f;
            }

            return RightHalfStart + (column - 6) * PointWidth + PointWidth * 0.5f;
        }

        public static float PointLeft(int point)
        {
            return PointX(point) - PointWidth * 0.5f;
        }

        public static float PointRight(int point)
        {
            return PointX(point) + PointWidth * 0.5f;
        }

        public static Vector3 CheckerPosition(int point, int stackIndex)
        {
            bool bottom = point <= 11;
            int row = stackIndex % 7;
            int layer = stackIndex / 7;
            float x = PointX(point);
            float z = bottom ? BottomBaseZ + 0.42f + row * 0.58f : TopBaseZ - 0.42f - row * 0.58f;
            float y = BoardHeight + CheckerHeight * 0.5f + layer * (CheckerHeight + 0.02f);
            return new Vector3(x, y, z);
        }

        public static Vector3 BarPosition(PlayerColor color, int index)
        {
            float z = color == PlayerColor.White ? -0.65f : 0.65f;
            float y = 0.71f + CheckerHeight * 0.5f + index * (CheckerHeight + 0.02f);
            return new Vector3(CenterBarX, y, z);
        }

        public static Vector3 BearOffPosition(PlayerColor color, int index)
        {
            int pile = index % 3;
            int layer = index / 3;
            float x = BearOffX;
            float zOffset = pile * 0.78f;
            float z = color == PlayerColor.White ? -4.08f + zOffset : 4.08f - zOffset;
            float y = BoardHeight + CheckerHeight * 0.5f + layer * (CheckerHeight + 0.025f);
            return new Vector3(x, y, z);
        }

        public static Vector3 TargetPosition(BackgammonMove move, GameState state)
        {
            if (move.BearsOff)
            {
                return BearOffPosition(state.CurrentPlayer, state.GetOffCount(state.CurrentPlayer));
            }

            int index = state.Points[move.ToPoint].Count;
            if (move.HitsOpponent)
            {
                index = 0;
            }

            return CheckerPosition(move.ToPoint, index);
        }
    }
}
