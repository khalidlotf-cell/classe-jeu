using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran de crédits — exigé par la partie "Droit du jeu" du cahier des charges :
    // équipe + rôles, outils d'IA utilisés, ressources externes et leurs licences.
    public class CreditsScreen : MonoBehaviour
    {
        public Button backButton;

        void OnEnable()
        {
            if (backButton) backButton.onClick.AddListener(OnBack);
        }

        void OnDisable()
        {
            if (backButton) backButton.onClick.RemoveListener(OnBack);
        }

        void OnBack()
        {
            if (UIManager.Instance) UIManager.Instance.Back();
        }
    }
}
