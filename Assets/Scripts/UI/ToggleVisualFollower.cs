using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Anime la pilule d'un Toggle : déplace le knob, change la couleur du fond,
    // met à jour le label OUI/NON.
    [RequireComponent(typeof(Toggle))]
    public class ToggleVisualFollower : MonoBehaviour
    {
        public Toggle toggle;
        public Image background;
        public RectTransform knob;
        public TMP_Text stateLabel;
        public Color colorOff;
        public Color colorOn;
        public float lerpSpeed = 14f;

        Vector2 knobOffPos;
        Vector2 knobOnPos;
        bool init;

        void OnEnable()
        {
            if (!toggle) toggle = GetComponent<Toggle>();
            if (toggle != null) toggle.onValueChanged.AddListener(OnChanged);
            // Force layout update before computing knob positions
            Canvas.ForceUpdateCanvases();
            ComputeKnobPositions();
            ApplyImmediate();
        }

        void OnDisable()
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnChanged);
        }

        void ComputeKnobPositions()
        {
            if (!knob) { init = true; return; }
            // En OFF, le knob est à gauche ; en ON, il est à droite.
            var parentRT = knob.parent as RectTransform;
            float parentW = parentRT ? parentRT.rect.width : 100f;
            float knobW = knob.rect.width;
            float pad = 6f;
            knobOffPos = new Vector2(pad, 0);
            knobOnPos  = new Vector2(parentW - knobW - pad, 0);
            init = true;
        }

        void OnChanged(bool _) { /* Update via Update() lerp */ }

        void Update()
        {
            if (!init) ComputeKnobPositions();
            if (!toggle) return;

            // Couleur fond
            if (background)
                background.color = Color.Lerp(background.color, toggle.isOn ? colorOn : colorOff, Time.unscaledDeltaTime * lerpSpeed);

            // Position knob
            if (knob)
            {
                Vector2 target = toggle.isOn ? knobOnPos : knobOffPos;
                knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, target, Time.unscaledDeltaTime * lerpSpeed);
            }

            // Texte OUI/NON
            if (stateLabel)
            {
                string desired = toggle.isOn ? "OUI" : "NON";
                if (stateLabel.text != desired) stateLabel.text = desired;
                stateLabel.color = toggle.isOn ? colorOn : new Color(0.5f, 0.5f, 0.55f);
            }
        }

        void ApplyImmediate()
        {
            if (background) background.color = toggle.isOn ? colorOn : colorOff;
            if (knob)        knob.anchoredPosition = toggle.isOn ? knobOnPos : knobOffPos;
            if (stateLabel)
            {
                stateLabel.text = toggle.isOn ? "OUI" : "NON";
                stateLabel.color = toggle.isOn ? colorOn : new Color(0.5f, 0.5f, 0.55f);
            }
        }
    }
}
