using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathsClass
{
    // Persistance simple via PlayerPrefs (multi-plateforme).
    // Stocke les settings et les top scores par mode.
    public static class SaveManager
    {
        const string KEY_SETTINGS = "mc.settings.v1";
        const string KEY_SCORES   = "mc.scores.v1";
        const string KEY_STATS    = "mc.stats.v1";

        public static event Action<Settings> OnSettingsChanged;

        // ---------- Settings ----------
        public static Settings LoadSettings()
        {
            string json = PlayerPrefs.GetString(KEY_SETTINGS, "");
            if (string.IsNullOrEmpty(json)) return Settings.Default;
            try { return JsonUtility.FromJson<Settings>(json) ?? Settings.Default; }
            catch { return Settings.Default; }
        }

        public static void SaveSettings(Settings s)
        {
            PlayerPrefs.SetString(KEY_SETTINGS, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke(s);
        }

        // ---------- Scores ----------
        [Serializable]
        public class ScoreEntry
        {
            public string playerName = "Joueur";
            public int score;
            public string mode;     // GameMode.ToString()
            public long timestamp;  // Unix seconds
        }

        [Serializable] class ScoreList { public List<ScoreEntry> entries = new List<ScoreEntry>(); }

        public static List<ScoreEntry> LoadScores()
        {
            string json = PlayerPrefs.GetString(KEY_SCORES, "");
            if (string.IsNullOrEmpty(json)) return new List<ScoreEntry>();
            try { return JsonUtility.FromJson<ScoreList>(json)?.entries ?? new List<ScoreEntry>(); }
            catch { return new List<ScoreEntry>(); }
        }

        public static void AddScore(int score, string mode, string playerName = "Joueur")
        {
            var list = LoadScores();
            list.Add(new ScoreEntry
            {
                playerName = playerName,
                score = score,
                mode = mode,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            list.Sort((a, b) => b.score.CompareTo(a.score));
            if (list.Count > 50) list.RemoveRange(50, list.Count - 50);
            PlayerPrefs.SetString(KEY_SCORES, JsonUtility.ToJson(new ScoreList { entries = list }));
            PlayerPrefs.Save();
        }

        public static int BestScore(string mode = null)
        {
            int best = 0;
            foreach (var e in LoadScores())
                if ((mode == null || e.mode == mode) && e.score > best) best = e.score;
            return best;
        }

        // ---------- Stats globales ----------
        [Serializable]
        public class Stats
        {
            public int gamesPlayed;
            public int totalCorrect;
            public int totalAnswered;
            public int bestScoreOverall;
        }

        public static Stats LoadStats()
        {
            string json = PlayerPrefs.GetString(KEY_STATS, "");
            if (string.IsNullOrEmpty(json)) return new Stats();
            try { return JsonUtility.FromJson<Stats>(json) ?? new Stats(); }
            catch { return new Stats(); }
        }

        public static void RecordRun(int finalScore, int correct, int answered)
        {
            var s = LoadStats();
            s.gamesPlayed++;
            s.totalCorrect += correct;
            s.totalAnswered += answered;
            if (finalScore > s.bestScoreOverall) s.bestScoreOverall = finalScore;
            PlayerPrefs.SetString(KEY_STATS, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        public static float SuccessRate()
        {
            var s = LoadStats();
            if (s.totalAnswered <= 0) return 0f;
            return (float)s.totalCorrect / s.totalAnswered;
        }
    }
}
