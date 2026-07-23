using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ErkekTavlasi.AI
{
    public class MonteCarloMemory
    {
        private readonly Dictionary<string, MemoryEntry> entries = new Dictionary<string, MemoryEntry>();

        public int EntryCount
        {
            get { return entries.Count; }
        }

        public string FilePath { get; private set; }

        public MonteCarloMemory(string filePath)
        {
            FilePath = filePath;
        }

        public MemoryEntry Get(string positionHash, string moveKey)
        {
            MemoryEntry entry;
            entries.TryGetValue(MakeKey(positionHash, moveKey), out entry);
            return entry;
        }

        public void Record(string positionHash, string moveKey, bool won)
        {
            string key = MakeKey(positionHash, moveKey);
            MemoryEntry entry;
            if (!entries.TryGetValue(key, out entry))
            {
                entry = new MemoryEntry
                {
                    PositionHash = positionHash,
                    MoveKey = moveKey
                };
                entries[key] = entry;
            }

            entry.Visits++;
            if (won)
            {
                entry.Wins++;
            }
        }

        public void Load()
        {
            entries.Clear();
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(FilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length < 4)
                {
                    continue;
                }

                MemoryEntry entry = new MemoryEntry
                {
                    PositionHash = parts[0],
                    MoveKey = parts[1],
                    Visits = ParseInt(parts[2]),
                    Wins = ParseInt(parts[3])
                };
                entries[MakeKey(entry.PositionHash, entry.MoveKey)] = entry;
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
                writer.WriteLine("# positionHash\tmoveKey\tvisits\twins");
                foreach (MemoryEntry entry in entries.Values)
                {
                    writer.Write(entry.PositionHash);
                    writer.Write('\t');
                    writer.Write(entry.MoveKey);
                    writer.Write('\t');
                    writer.Write(entry.Visits.ToString(CultureInfo.InvariantCulture));
                    writer.Write('\t');
                    writer.WriteLine(entry.Wins.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static int ParseInt(string value)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static string MakeKey(string positionHash, string moveKey)
        {
            return positionHash + "\n" + moveKey;
        }
    }

    public class MemoryEntry
    {
        public string PositionHash;
        public string MoveKey;
        public int Visits;
        public int Wins;

        public float WinRate
        {
            get
            {
                if (Visits <= 0)
                {
                    return 0.5f;
                }

                return (float)Wins / Visits;
            }
        }
    }
}
