using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran sélection de mode (M_2_menu.png) : Classique / Détente / Speedrun / Infini / Survie.
    public class ModeSelectScreen : MonoBehaviour
    {
        [System.Serializable]
        public class ModeButton
        {
            public GameMode mode;
            public Button button;
        }

        public ModeButton[] modeButtons;
        public Button backButton;

        void Awake()
        {
            foreach (var mb in modeButtons)
            {
                if (!mb.button) continue;
                var captured = mb.mode;
                mb.button.onClick.AddListener(() => StartMode(captured));
            }
            if (backButton) backButton.onClick.AddListener(() => UIManager.Instance.Back());
        }

        void StartMode(GameMode mode)
        {
            // GameManager s'occupe de charger la scène / activer le HUD.
            if (GameManager.Instance) GameManager.Instance.StartGameWithMode(mode);
        }
    }
}
