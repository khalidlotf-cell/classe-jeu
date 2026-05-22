using UnityEngine;

namespace MathsClass
{
    // Fait flotter le chiffre d'une dalle au-dessus du sol :
    //  - oscillation verticale douce (haut / bas)
    //  - billboard sur l'axe Y : le chiffre reste DEBOUT et tourné vers le joueur,
    //    donc lisible de loin et sous n'importe quel angle.
    public class FloatingNumber : MonoBehaviour
    {
        [Header("Oscillation")]
        public float bobAmplitude = 0.2f;   // amplitude haut/bas (mètres)
        public float bobSpeed = 1.7f;       // vitesse de l'oscillation

        [Header("Orientation")]
        public bool faceCamera = true;      // tourne le chiffre vers le joueur

        Vector3 basePos;
        float phase;
        Transform cam;

        void OnEnable()
        {
            basePos = transform.localPosition;
            // déphasage aléatoire : les 10 dalles n'oscillent pas en même temps
            phase = Random.value * Mathf.PI * 2f;
        }

        void LateUpdate()
        {
            // --- oscillation verticale ---
            float y = basePos.y + Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude;
            transform.localPosition = new Vector3(basePos.x, y, basePos.z);

            // --- billboard vertical : face au joueur, mais toujours « debout » ---
            if (!faceCamera) return;
            if (cam == null)
            {
                var c = Camera.main;
                if (c) cam = c.transform;
                if (cam == null) return;
            }
            Vector3 dir = transform.position - cam.position;
            dir.y = 0f; // on annule l'inclinaison verticale -> le chiffre reste droit
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
