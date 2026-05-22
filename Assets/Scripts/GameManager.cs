using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MathsClass
{
    // Orchestrateur principal :
    // - state machine (Menu / Playing / Paused / GameOver)
    // - intègre ModeConfig (par mode), TileManager, HUDController, UIManager, AudioManager, FXManager
    // - persiste scores et stats via SaveManager
    // - annonce les calculs via AccessibilityManager (TTS + sous-titres)
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public enum GameState { Menu, Playing, Paused, GameOver }

        [Header("Refs scène 3D")]
        public TMP_Text blackboardText;
        public Transform playerSpawn;
        public PlayerController player;
        public TileManager tileManager;

        [Header("Managers")]
        public AudioManager audioMgr;
        public FXManager fxMgr;
        public HUDController hud;

        [Header("HUD canvases")]
        public CanvasGroup hudGroup;

        [Header("Tuning global")]
        public int comboThreshold = 3;
        public int bonusLifeEvery = 10;
        public float interRoundDelay = 0.4f;

        // ----- État runtime -----
        GameState state = GameState.Menu;
        ModeConfig modeCfg;
        int score;
        int lives;
        int lifeCap;        // plafond de vies = nombre de cœurs affichables dans le HUD
        int correctCount;
        int answeredCount;
        int combo;
        int currentAnswer;
        float timeLeft;
        bool tickPlayedThisSecond;

        // Pour l'écran Game Over
        public int lastScore { get; private set; }
        public GameMode lastMode { get; private set; } = GameMode.Classique;
        public bool lastWasVictory { get; private set; }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            EnterMenu();
        }

        void Update()
        {
            if (state == GameState.Playing)
            {
                if (!modeCfg.noTimer)
                {
                    timeLeft -= Time.deltaTime;
                    if (hud) hud.SetTimer(timeLeft);
                    if (timeLeft <= 3f && timeLeft > 0f)
                    {
                        if (!tickPlayedThisSecond) { audioMgr?.PlayTick(); tickPlayedThisSecond = true; }
                        if (timeLeft - Mathf.Floor(timeLeft) > 0.5f) tickPlayedThisSecond = false;
                    }
                    if (timeLeft <= 0) StartCoroutine(HandleTimeout());
                }
            }

            if (state == GameState.Playing || state == GameState.Paused)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    TogglePause();
            }
        }

        // ----- Transitions d'écran -----
        public void EnterMenu()
        {
            state = GameState.Menu;
            Time.timeScale = 1f;
            if (hudGroup) { hudGroup.alpha = 0; hudGroup.blocksRaycasts = false; }
            if (player)
            {
                player.inputEnabled = false;
                player.UnlockCursor();
                if (playerSpawn) player.Teleport(playerSpawn.position, playerSpawn.rotation);
            }
            audioMgr?.SetMusicTier(0);
            UIManager.Instance?.Show(ScreenId.MainMenu, pushHistory: false);
        }

        public void StartGameWithMode(GameMode mode)
        {
            modeCfg = ModeRegistry.Get(mode);
            lastMode = mode;
            state = GameState.Playing;
            Time.timeScale = 1f;

            score = 0;
            answeredCount = 0;
            correctCount = 0;
            combo = 0;
            lives = modeCfg.startingLives;
            timeLeft = modeCfg.startTime * modeCfg.timeMultiplier;

            // Plafond de vies = nombre de cœurs réellement affichables dans le HUD.
            // Sans ça, une vie bonus peut dépasser le nombre de cœurs : on perd alors
            // une vie « invisible » et aucun cœur ne disparaît à l'écran.
            int heartSlots = (hud != null && hud.hearts != null) ? hud.hearts.Length : 3;
            lifeCap = Mathf.Min(modeCfg.startingLives, heartSlots);

            if (hudGroup) { hudGroup.alpha = 1; hudGroup.blocksRaycasts = true; }
            if (hud)
            {
                hud.maxHearts = lifeCap;
                hud.SetScore(0);
                hud.SetLives(lives, hud.maxHearts);
                hud.SetCombo(0, comboThreshold);
                hud.SetTimer(timeLeft);
                hud.SetPalier(1);
            }

            tileManager?.ResetAll();
            if (AccessibilityManager.Instance && tileManager)
                tileManager.ApplyColorblind(AccessibilityManager.Instance.isColorblind);

            if (player)
            {
                if (playerSpawn) player.Teleport(playerSpawn.position, playerSpawn.rotation);
                player.inputEnabled = true;
                player.LockCursor();
            }

            UIManager.Instance?.HideAll();
            audioMgr?.SetMusicTier(1);
            audioMgr?.PlayStart();
            NewRound();
        }

        public void RestartCurrentMode() => StartGameWithMode(lastMode);

        public void QuitToMenu()
        {
            // Si une partie était en cours, l'enregistrer comme abandonnée (pas de score sauvé)
            if (state == GameState.Playing || state == GameState.Paused)
            {
                if (player) { player.inputEnabled = false; player.UnlockCursor(); }
            }
            EnterMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void TogglePause()
        {
            if (state == GameState.Playing) Pause();
            else if (state == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (state != GameState.Playing) return;
            state = GameState.Paused;
            Time.timeScale = 0f;
            UIManager.Instance?.Show(ScreenId.Pause, pushHistory: false);
            if (player) { player.inputEnabled = false; player.UnlockCursor(); }
        }

        public void Resume()
        {
            if (state != GameState.Paused) return;
            state = GameState.Playing;
            Time.timeScale = 1f;
            UIManager.Instance?.Hide(ScreenId.Pause);
            if (player) { player.inputEnabled = true; player.LockCursor(); }
        }

        // ----- Boucle de jeu -----
        int GetTier()
        {
            if (modeCfg == null) return 1;
            return Mathf.Clamp(1 + correctCount / modeCfg.paliersPerStep, 1, 7);
        }

        MathGenerator.Tier MapTier(int tier) => tier switch
        {
            1 => MathGenerator.Tier.Easy,
            2 => MathGenerator.Tier.Sub,
            3 => MathGenerator.Tier.Mul,
            4 => MathGenerator.Tier.Mixed,
            5 => MathGenerator.Tier.Div,
            6 => MathGenerator.Tier.Chaos,
            _ => MathGenerator.Tier.Hell
        };

        float TimeBonusForTier(int tier) => Mathf.Max(2f, 6f - tier);

        void NewRound()
        {
            int tier = GetTier();
            var q = MathGenerator.Generate(MapTier(tier));
            currentAnswer = q.answer;
            if (blackboardText) blackboardText.text = q.display + " = ?";
            tileManager?.ResetAll();
            if (player && playerSpawn) player.Teleport(playerSpawn.position, playerSpawn.rotation);
            if (hud) { hud.SetPalier(tier); }
            audioMgr?.SetMusicTier(tier);
            // TTS / sous-titres : annonce le calcul
            AccessibilityManager.Instance?.Announce(q.display + " ? ");
        }

        public void OnTileStepped(int number)
        {
            if (state != GameState.Playing) return;
            answeredCount++;
            if (number == currentAnswer) StartCoroutine(HandleCorrect(number));
            else StartCoroutine(HandleWrong(number));
        }

        IEnumerator HandleCorrect(int number)
        {
            state = GameState.GameOver; // freeze pour transition
            combo++;
            int tier = GetTier();
            int gain = combo >= comboThreshold ? 2 : 1;
            int gained = Mathf.RoundToInt(gain * (modeCfg?.scoreMultiplier ?? 1f));
            score += gained;
            correctCount++;
            if (modeCfg != null && !modeCfg.noTimer)
                timeLeft += TimeBonusForTier(tier);
            // bonus vie tous les N points
            if (bonusLifeEvery > 0 && score > 0 && score % bonusLifeEvery == 0 && lives < lifeCap && !modeCfg.unlimitedLives)
                lives++;

            if (hud)
            {
                hud.SetScore(score);
                hud.SetLives(lives, hud.maxHearts);
                hud.SetCombo(combo, comboThreshold);
                hud.ShowScorePopup($"+{gained}");
            }
            audioMgr?.PlayDing();
            fxMgr?.PlayConfetti(tileManager ? tileManager.GetPosition(number) : Vector3.zero);
            fxMgr?.FlashGreen();

            // Condition de victoire (si le mode en définit une)
            if (modeCfg != null && modeCfg.victoryScore > 0 && score >= modeCfg.victoryScore)
            {
                yield return new WaitForSeconds(interRoundDelay);
                TriggerVictory();
                yield break;
            }

            yield return new WaitForSeconds(interRoundDelay);
            state = GameState.Playing;
            interRoundDelay = Mathf.Max(0.15f, 0.4f - correctCount * 0.01f);
            NewRound();
        }

        void TriggerVictory()
        {
            state = GameState.GameOver;
            lastScore = score;
            lastWasVictory = true;
            SaveManager.AddScore(score, lastMode.ToString());
            SaveManager.RecordRun(score, correctCount, answeredCount);

            if (hudGroup) { hudGroup.alpha = 0; hudGroup.blocksRaycasts = false; }
            if (player) { player.inputEnabled = false; player.UnlockCursor(); }
            audioMgr?.PlayStart(); // son joyeux
            audioMgr?.SetMusicTier(0);
            AccessibilityManager.Instance?.Announce("Victoire !");
            UIManager.Instance?.Show(ScreenId.GameOver, pushHistory: false);
        }

        IEnumerator HandleWrong(int number)
        {
            state = GameState.GameOver;
            combo = 0;

            if (modeCfg != null && modeCfg.oneLifeOnly)
            {
                lives = 0;
            }
            else if (modeCfg != null && !modeCfg.unlimitedLives)
            {
                lives--;
            }

            if (modeCfg != null && !modeCfg.noTimer)
                timeLeft = Mathf.Max(2f, timeLeft + modeCfg.timeOnWrong);

            if (hud)
            {
                hud.SetLives(lives, hud.maxHearts);
                hud.SetCombo(0, comboThreshold);
            }
            audioMgr?.PlayBuzz();
            fxMgr?.FlashRed();
            fxMgr?.ShakeCamera(0.35f, 0.4f);
            AccessibilityManager.Instance?.Announce("Erreur");

            yield return new WaitForSecondsRealtime(0.6f);
            if (lives <= 0) { TriggerGameOver(); yield break; }
            state = GameState.Playing;
            NewRound();
        }

        IEnumerator HandleTimeout()
        {
            state = GameState.GameOver;
            combo = 0;
            if (modeCfg != null && !modeCfg.unlimitedLives) lives--;
            timeLeft = modeCfg.timeoutResetTo * modeCfg.timeMultiplier;
            if (hud) { hud.SetLives(lives, hud.maxHearts); hud.SetCombo(0, comboThreshold); }
            audioMgr?.PlayBuzz();
            fxMgr?.FlashRed();
            fxMgr?.ShakeCamera(0.35f, 0.4f);
            AccessibilityManager.Instance?.Announce("Temps écoulé");
            yield return new WaitForSecondsRealtime(0.6f);
            if (lives <= 0) { TriggerGameOver(); yield break; }
            state = GameState.Playing;
            NewRound();
        }

        void TriggerGameOver()
        {
            state = GameState.GameOver;
            lastScore = score;
            lastWasVictory = false;
            SaveManager.AddScore(score, lastMode.ToString());
            SaveManager.RecordRun(score, correctCount, answeredCount);

            if (hudGroup) { hudGroup.alpha = 0; hudGroup.blocksRaycasts = false; }
            if (player) { player.inputEnabled = false; player.UnlockCursor(); }
            audioMgr?.PlayGameOver();
            audioMgr?.SetMusicTier(0);
            AccessibilityManager.Instance?.Announce($"Partie terminée. Score final : {score}");
            UIManager.Instance?.Show(ScreenId.GameOver, pushHistory: false);
        }
    }
}
