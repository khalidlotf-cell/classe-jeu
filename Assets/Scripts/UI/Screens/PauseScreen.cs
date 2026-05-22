using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran pause (M_5_pause.jpg) : Reprendre / Paramètres / Menu / Quitter.
    public class PauseScreen : MonoBehaviour
    {
        public Button resumeButton;
        public Button settingsButton;
        public Button mainMenuButton;
        public Button quitButton;

        void Awake()
        {
            if (resumeButton)   resumeButton.onClick.AddListener(() => GameManager.Instance?.Resume());
            if (settingsButton) settingsButton.onClick.AddListener(() => UIManager.Instance.Show(ScreenId.Settings));
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.QuitToMenu());
            if (quitButton)     quitButton.onClick.AddListener(() => GameManager.Instance?.QuitGame());
        }
    }
}
