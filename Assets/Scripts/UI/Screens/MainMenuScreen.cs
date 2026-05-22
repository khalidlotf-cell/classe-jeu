using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran d'accueil (M_1_accueil.png) : logo + Jouer / Scores / Paramètres / Crédits.
    public class MainMenuScreen : MonoBehaviour
    {
        public Button playButton;
        public Button scoresButton;
        public Button settingsButton;
        public Button creditsButton;

        void Awake()
        {
            if (playButton)     playButton.onClick.AddListener(() => UIManager.Instance.Show(ScreenId.ModeSelect));
            if (scoresButton)   scoresButton.onClick.AddListener(() => UIManager.Instance.Show(ScreenId.Scores));
            if (settingsButton) settingsButton.onClick.AddListener(() => UIManager.Instance.Show(ScreenId.Settings));
            if (creditsButton)  creditsButton.onClick.AddListener(() => UIManager.Instance.Show(ScreenId.Credits));
        }
    }
}
