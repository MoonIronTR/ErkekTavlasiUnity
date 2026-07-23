using System.Globalization;
using System.IO;

namespace ErkekTavlasi.AI
{
    public class MonteCarloWeights
    {
        public readonly float[] Values = new float[MoveFeatureVector.Names.Length];
        public string FilePath { get; private set; }

        public MonteCarloWeights(string filePath)
        {
            FilePath = filePath;
        }

        public float Score(MoveFeatureVector features)
        {
            float score = 0f;
            for (int i = 0; i < Values.Length && i < features.Values.Length; i++)
            {
                score += Values[i] * features.Values[i];
            }

            return score;
        }

        public void Update(MoveFeatureVector features, float target, float learningRate, float l2)
        {
            float prediction = Score(features);
            float error = target - prediction;
            for (int i = 0; i < Values.Length && i < features.Values.Length; i++)
            {
                Values[i] += learningRate * (error * features.Values[i] - l2 * Values[i]);
                if (Values[i] > 3f)
                {
                    Values[i] = 3f;
                }
                else if (Values[i] < -3f)
                {
                    Values[i] = -3f;
                }
            }
        }

        public void Load()
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = 0f;
            }

            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(FilePath);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length < 2)
                {
                    continue;
                }

                int index = FindFeatureIndex(parts[0]);
                if (index >= 0)
                {
                    Values[index] = ParseFloat(parts[1]);
                }
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(FilePath, false))
            {
                writer.WriteLine("# feature\tweight");
                for (int i = 0; i < Values.Length; i++)
                {
                    writer.Write(MoveFeatureVector.Names[i]);
                    writer.Write('\t');
                    writer.WriteLine(Values[i].ToString("R", CultureInfo.InvariantCulture));
                }
            }
        }

        private static int FindFeatureIndex(string name)
        {
            for (int i = 0; i < MoveFeatureVector.Names.Length; i++)
            {
                if (MoveFeatureVector.Names[i] == name)
                {
                    return i;
                }
            }

            return -1;
        }

        private static float ParseFloat(string value)
        {
            float parsed;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0f;
        }
    }
}
