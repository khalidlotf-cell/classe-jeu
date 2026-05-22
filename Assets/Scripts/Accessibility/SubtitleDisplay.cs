using System.Collections;
using TMPro;
using UnityEngine;

namespace MathsClass
{
    // Affiche un sous-titre temporaire en bas de l'écran lorsque
    // AccessibilityManager.Announce() est appelé et que le réglage est actif.
    public class SubtitleDisplay : MonoBehaviour
    {
        public TMP_Text label;
        public CanvasGroup group;
        public float displayDuration = 2.5f;

        Coroutine fadeCo;

        void OnEnable() { AccessibilityManager.OnSpeak += Show; HideImmediate(); }
        void OnDisable() { AccessibilityManager.OnSpeak -= Show; }

        void Show(string text)
        {
            if (!AccessibilityManager.Instance || !AccessibilityManager.Instance.subtitlesEnabled) return;
            if (label) label.text = text;
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeRoutine());
        }

        IEnumerator FadeRoutine()
        {
            if (!group) yield break;
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(displayDuration);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                yield return null;
            }
            group.alpha = 0f;
        }

        void HideImmediate()
        {
            if (group) group.alpha = 0f;
            if (label) label.text = "";
        }
    }
}
