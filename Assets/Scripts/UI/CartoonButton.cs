using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathsClass
{
    // Animation cartoon (gonflement hover, presse au clic) pour les boutons UI.
    // À ajouter sur tous les boutons fidèles aux maquettes.
    [RequireComponent(typeof(RectTransform))]
    public class CartoonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.05f;
        public float pressScale = 0.96f;
        public float lerpSpeed = 18f;

        [Header("SFX (optionnel)")]
        public AudioClip hoverClip;
        public AudioClip clickClip;

        Vector3 baseScale;
        Vector3 targetScale;
        Button btn;

        void Awake()
        {
            baseScale = transform.localScale;
            targetScale = baseScale;
            btn = GetComponent<Button>();
            if (btn) btn.onClick.AddListener(PlayClickSfx);
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);
        }

        public void OnPointerEnter(PointerEventData _) { targetScale = baseScale * hoverScale; PlayHoverSfx(); }
        public void OnPointerExit(PointerEventData _)  { targetScale = baseScale; }
        public void OnPointerDown(PointerEventData _)  { targetScale = baseScale * pressScale; }
        public void OnPointerUp(PointerEventData _)    { targetScale = baseScale * hoverScale; }

        void PlayHoverSfx() { if (hoverClip && Camera.main) AudioSource.PlayClipAtPoint(hoverClip, Camera.main.transform.position, 0.3f); }
        void PlayClickSfx() { if (clickClip && Camera.main) AudioSource.PlayClipAtPoint(clickClip, Camera.main.transform.position, 0.5f); }
    }
}
