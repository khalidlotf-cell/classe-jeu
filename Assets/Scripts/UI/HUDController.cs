using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // HUD in-game : score, chrono, vies (cœurs cartoon), combo, palier.
    // Le GameManager pousse les valeurs ici plutôt que d'avoir tout le mapping en interne.
    public class HUDController : MonoBehaviour
    {
        [Header("Texts")]
        public TMP_Text scoreText;
        public TMP_Text timerText;
        public TMP_Text comboText;
        public TMP_Text palierText;

        [Header("Hearts")]
        public Image[] hearts;          // ordre : 0 = leftmost
        public Sprite heartFull;
        public Sprite heartEmpty;
        public int maxHearts = 3;

        [Header("Couleurs chrono")]
        public Color timerCalm   = new Color(0.38f, 0.25f, 0.72f); // violet (calme)
        public Color timerWarn   = new Color(1f, 0.62f, 0.1f);     // orange (attention)
        public Color timerAlarm  = new Color(0.95f, 0.25f, 0.25f); // rouge (urgence)

        [Header("Score popup")]
        public TMP_Text scorePopup;
        public CanvasGroup scorePopupGroup;

        Coroutine popupCo;

        public void SetScore(int score)
        {
            if (scoreText) scoreText.text = score.ToString("0");
        }

        public void SetTimer(float seconds)
        {
            if (!timerText) return;
            int sec = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timerText.text = sec.ToString();
            // 3 paliers de couleur = repère visuel redondant (accessibilité)
            timerText.color = sec <= 3 ? timerAlarm : (sec <= 6 ? timerWarn : timerCalm);
        }

        public void SetLives(int current, int max)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (!hearts[i]) continue;
                bool show = i < max;
                hearts[i].enabled = show;
                if (!show) continue;
                bool full = i < current;
                if (full && heartFull) hearts[i].sprite = heartFull;
                else if (!full && heartEmpty) hearts[i].sprite = heartEmpty;
                hearts[i].color = full ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        public void SetCombo(int combo, int threshold)
        {
            if (!comboText) return;
            if (combo >= threshold)
            {
                comboText.text = $"COMBO ×2  ({combo})";
                comboText.color = new Color(1f, 0.6f, 0.2f);
            }
            else if (combo > 0)
            {
                comboText.text = $"streak {combo}";
                comboText.color = new Color(1f, 1f, 1f, 0.7f);
            }
            else comboText.text = "";
        }

        public void SetPalier(int tier)
        {
            if (!palierText) return;
            string[] names = { "", "Échauffement", "Soustraction", "Multiplication", "Priorité", "Division", "Chaos", "Enfer" };
            int t = Mathf.Clamp(tier, 1, 7);
            palierText.text = $"Palier {t} — {names[t]}";
        }

        public void ShowScorePopup(string text)
        {
            if (!scorePopup) return;
            scorePopup.text = text;
            if (popupCo != null) StopCoroutine(popupCo);
            popupCo = StartCoroutine(PopupRoutine());
        }

        IEnumerator PopupRoutine()
        {
            if (!scorePopupGroup) yield break;
            scorePopupGroup.alpha = 1f;
            scorePopup.transform.localScale = Vector3.one * 0.6f;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                scorePopup.transform.localScale = Vector3.Lerp(Vector3.one * 0.6f, Vector3.one * 1.15f, t / 0.18f);
                yield return null;
            }
            t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                scorePopupGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.7f);
                yield return null;
            }
            scorePopupGroup.alpha = 0f;
        }
    }
}
