using System.Collections.Generic;

namespace MathsClass
{
    public enum GameMode
    {
        Classique,
        Detente,
        Speedrun,
        Infini,
        Survie
    }

    // Paramètres par mode. Centralise la différenciation gameplay.
    public class ModeConfig
    {
        public GameMode mode;
        public string displayName;
        public string description;
        public int startingLives = 3;
        public float startTime = 12f;
        public float timeOnWrong = -2f;
        public float timeoutResetTo = 10f;
        public float timeMultiplier = 1f;     // chrono ×N (mode détente = 1.5)
        public bool unlimitedLives = false;
        public bool oneLifeOnly = false;      // survie
        public bool noTimer = false;          // détente pure (option)
        public int paliersPerStep = 5;        // tier+1 tous les N corrects
        public float scoreMultiplier = 1f;
        public int victoryScore = 0;          // 0 = pas de victoire, sinon score à atteindre pour gagner
    }

    public static class ModeRegistry
    {
        public static readonly Dictionary<GameMode, ModeConfig> All = new Dictionary<GameMode, ModeConfig>
        {
            { GameMode.Classique, new ModeConfig {
                mode = GameMode.Classique,
                displayName = "CLASSIQUE",
                description = "3 vies, chrono. Atteins 50 points pour gagner.",
                startingLives = 3, startTime = 12f, timeOnWrong = -2f, paliersPerStep = 5,
                victoryScore = 50
            }},
            { GameMode.Detente, new ModeConfig {
                mode = GameMode.Detente,
                displayName = "DÉTENTE",
                description = "Chrono ×1.5. Atteins 30 points pour gagner.",
                startingLives = 3, startTime = 12f, timeMultiplier = 1.5f,
                timeOnWrong = -1f, paliersPerStep = 7, scoreMultiplier = 0.7f,
                victoryScore = 30
            }},
            { GameMode.Speedrun, new ModeConfig {
                mode = GameMode.Speedrun,
                displayName = "SPEEDRUN",
                description = "Chrono court. Atteins 40 points pour gagner.",
                startingLives = 3, startTime = 8f, timeMultiplier = 0.8f,
                timeOnWrong = -3f, paliersPerStep = 3, scoreMultiplier = 1.3f,
                victoryScore = 40
            }},
            { GameMode.Infini, new ModeConfig {
                mode = GameMode.Infini,
                displayName = "INFINI",
                description = "Pas de game over. Pur entraînement.",
                startingLives = 99, startTime = 15f, unlimitedLives = true,
                timeOnWrong = -1f, paliersPerStep = 5
            }},
            { GameMode.Survie, new ModeConfig {
                mode = GameMode.Survie,
                displayName = "SURVIE",
                description = "1 vie. Une erreur, c'est fini.",
                startingLives = 1, startTime = 12f, oneLifeOnly = true,
                timeOnWrong = -2f, paliersPerStep = 5, scoreMultiplier = 2f
            }}
        };

        public static ModeConfig Get(GameMode m) => All.TryGetValue(m, out var c) ? c : All[GameMode.Classique];
    }
}
