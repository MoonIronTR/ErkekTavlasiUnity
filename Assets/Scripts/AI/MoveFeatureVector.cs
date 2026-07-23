namespace ErkekTavlasi.AI
{
    public class MoveFeatureVector
    {
        public const int PointCount = 24;
        public const int CountBucketCount = 6;
        public const int DeltaBucketCount = 11;
        public const int FromSlotCount = 25;
        public const int ToSlotCount = 25;

        public static readonly string[] Names = CreateNames();

        public readonly float[] Values;

        public MoveFeatureVector()
        {
            Values = new float[Names.Length];
        }

        private static string[] CreateNames()
        {
            System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
            names.Add("bias");
            AddBoardNames(names, "beforeOwn", CountBucketCount);
            AddBoardNames(names, "beforeOpponent", CountBucketCount);
            AddBoardNames(names, "afterOwn", CountBucketCount);
            AddBoardNames(names, "afterOpponent", CountBucketCount);
            AddBoardNames(names, "deltaOwn", DeltaBucketCount);
            AddBoardNames(names, "deltaOpponent", DeltaBucketCount);

            for (int die = 1; die <= 6; die++)
            {
                names.Add("dice" + die);
            }

            names.Add("fromBar");
            for (int point = 0; point < PointCount; point++)
            {
                names.Add("fromPoint" + point.ToString("00"));
            }

            names.Add("toOff");
            for (int point = 0; point < PointCount; point++)
            {
                names.Add("toPoint" + point.ToString("00"));
            }

            names.Add("stepCount");
            names.Add("ownBarBefore");
            names.Add("opponentBarBefore");
            names.Add("ownOffBefore");
            names.Add("opponentOffBefore");
            names.Add("ownBarAfter");
            names.Add("opponentBarAfter");
            names.Add("ownOffAfter");
            names.Add("opponentOffAfter");
            return names.ToArray();
        }

        private static void AddBoardNames(System.Collections.Generic.List<string> names, string prefix, int bucketCount)
        {
            for (int point = 0; point < PointCount; point++)
            {
                for (int bucket = 0; bucket < bucketCount; bucket++)
                {
                    names.Add(prefix + "Point" + point.ToString("00") + "Count" + bucket);
                }
            }
        }
    }
}
