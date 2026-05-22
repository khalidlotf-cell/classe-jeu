using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran fin de partie (M_6_game_over.jpg) : trophée + score + record + Rejouer / Menu.
    // Affiche "VICTOIRE !" si le joueur a gagné, sinon "PARTIE TERMINÉE".
    public class GameOverScreen : MonoBehaviour
    {
        public TMP_Text titleText;
        public TMP_Text scoreText;
        public TMP_Text recordText;
        public Button replayButton;
        public Button mainMenuButton;
        public ParticleSystem confetti;

        void Awake()
        {
            if (replayButton)   replayButton.onClick.AddListener(() => GameManager.Instance?.RestartCurrentMode());
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.QuitToMenu());
        }

        void OnEnable()
        {
            if (GameManager.Instance == null) return;
            int score = GameManager.Instance.lastScore;
            string mode = GameManager.Instance.lastMode.ToString();
            bool victory = GameManager.Instance.lastWasVictory;
            if (titleText) titleText.text = victory ? "VICTOIRE !" : "PARTIE TERMINÉE";
            if (scoreText)  scoreText.text = score.ToString();
            int best = SaveManager.BestScore(mode);
            if (recordText) recordText.text = $"RECORD PERSONNEL : {best}";
            if (confetti) confetti.Play();
        }
    }
}
