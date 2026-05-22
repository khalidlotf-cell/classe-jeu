using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Effets : flash rouge sur erreur, screen shake, particules confetti.
    public class FXManager : MonoBehaviour
    {
        public Camera mainCam;
        public Image flashImage;
        public ParticleSystem confettiPrefab;

        Vector3 camBaseLocalPos;
        Coroutine shakeCo;
        Coroutine flashCo;

        void Awake()
        {
            if (mainCam) camBaseLocalPos = mainCam.transform.localPosition;
        }

        public void FlashRed()
        {
            if (!flashImage) return;
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            flashImage.color = new Color(1, 0.1f, 0.1f, 0.55f);
            float t = 0;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.55f, 0f, t / 0.35f);
                flashImage.color = new Color(1, 0.1f, 0.1f, a);
                yield return null;
            }
            flashImage.color = new Color(1, 0.1f, 0.1f, 0f);
        }

        public void FlashGreen()
        {
            if (!flashImage) return;
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashGreenRoutine());
        }

        IEnumerator FlashGreenRoutine()
        {
            flashImage.color = new Color(0.3f, 1f, 0.4f, 0.35f);
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.35f, 0f, t / 0.3f);
                flashImage.color = new Color(0.3f, 1f, 0.4f, a);
                yield return null;
            }
            flashImage.color = new Color(0.3f, 1f, 0.4f, 0f);
        }

        public void ShakeCamera(float magnitude, float duration)
        {
            if (!mainCam) return;
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(ShakeRoutine(magnitude, duration));
        }

        IEnumerator ShakeRoutine(float magnitude, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - (t / duration);
                Vector3 r = Random.insideUnitSphere * magnitude * k;
                r.z = 0;
                mainCam.transform.localPosition = camBaseLocalPos + r;
                yield return null;
            }
            mainCam.transform.localPosition = camBaseLocalPos;
        }

        public void PlayConfetti(Vector3 worldPos)
        {
            if (!confettiPrefab) return;
            var inst = Instantiate(confettiPrefab, worldPos + Vector3.up * 0.3f, Quaternion.identity);
            inst.Play();
            Destroy(inst.gameObject, 4f);
        }
    }
}
