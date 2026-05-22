using System.Collections;
using TMPro;
using UnityEngine;

namespace MathsClass
{
    // Une dalle numérotée. Le joueur marche dessus → déclenche OnTileStepped sur GameManager.
    // Anim de sink + texte visible + overlay forme pour mode daltonien.
    public class Tile : MonoBehaviour
    {
        public int number;
        public Transform visual;
        public ParticleSystem stepParticles;

        [Header("Affichage")]
        public TMP_Text numberLabel;        // affiche le chiffre sur la dalle
        public Renderer visualRenderer;     // pour glow/couleur
        public GameObject colorblindShape;  // forme (étoile/triangle…) visible si daltonien actif

        bool fired;
        Vector3 visualBasePos;
        Vector3 visualBaseScale;

        void Awake()
        {
            if (visual)
            {
                visualBasePos = visual.localPosition;
                visualBaseScale = visual.localScale;
            }
            if (numberLabel) numberLabel.text = number.ToString();
            if (colorblindShape) colorblindShape.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (!other.CompareTag("Player")) return;
            fired = true;
            if (stepParticles) stepParticles.Play();
            StartCoroutine(SinkAnim());
            if (GameManager.Instance != null) GameManager.Instance.OnTileStepped(number);
        }

        public void ResetTrigger()
        {
            fired = false;
            if (visual)
            {
                visual.localPosition = visualBasePos;
                visual.localScale = visualBaseScale;
            }
        }

        public void SetColorblind(bool active)
        {
            if (colorblindShape) colorblindShape.SetActive(active);
        }

        IEnumerator SinkAnim()
        {
            if (!visual) yield break;
            float t = 0f;
            const float sinkTime = 0.12f;
            const float riseTime = 0.18f;
            Vector3 sunkPos = visualBasePos + Vector3.down * 0.06f;
            Vector3 sunkScale = new Vector3(visualBaseScale.x, visualBaseScale.y * 0.5f, visualBaseScale.z);
            while (t < sinkTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / sinkTime);
                visual.localPosition = Vector3.Lerp(visualBasePos, sunkPos, k);
                visual.localScale = Vector3.Lerp(visualBaseScale, sunkScale, k);
                yield return null;
            }
            t = 0f;
            while (t < riseTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / riseTime);
                visual.localPosition = Vector3.Lerp(sunkPos, visualBasePos, k);
                visual.localScale = Vector3.Lerp(sunkScale, visualBaseScale, k);
                yield return null;
            }
            visual.localPosition = visualBasePos;
            visual.localScale = visualBaseScale;
        }
    }
}
