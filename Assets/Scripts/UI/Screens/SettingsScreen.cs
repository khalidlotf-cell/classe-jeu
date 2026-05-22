using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran paramètres (M_3_parametres.jpg).
    // Toggles : TTS, sous-titres, daltonien, OpenDyslexic, mode détente.
    // Sliders : volume musique, volume effets, sensibilité caméra.
    // Persistance via SaveManager.
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Toggles")]
        public Toggle ttsToggle;
        public Toggle subtitlesToggle;
        public Toggle colorblindToggle;
        public Toggle dyslexicToggle;
        public Toggle relaxedToggle;

        [Header("Sliders")]
        public Slider musicSlider;
        public Slider sfxSlider;
        public Slider sensitivitySlider;

        [Header("Navigation")]
        public Button backButton;

        Settings cache;
        bool initializing;

        void OnEnable()
        {
            cache = SaveManager.LoadSettings();
            initializing = true;
            if (ttsToggle)         ttsToggle.isOn         = cache.tts;
            if (subtitlesToggle)   subtitlesToggle.isOn   = cache.subtitles;
            if (colorblindToggle)  colorblindToggle.isOn  = cache.colorblind;
            if (dyslexicToggle)    dyslexicToggle.isOn    = cache.dyslexicFont;
            if (relaxedToggle)     relaxedToggle.isOn     = cache.relaxedMode;
            if (musicSlider)       musicSlider.value       = cache.musicVolume;
            if (sfxSlider)         sfxSlider.value         = cache.sfxVolume;
            if (sensitivitySlider) sensitivitySlider.value = cache.cameraSensitivity;
            initializing = false;

            if (ttsToggle)         ttsToggle.onValueChanged.AddListener(_ => Apply());
            if (subtitlesToggle)   subtitlesToggle.onValueChanged.AddListener(_ => Apply());
            if (colorblindToggle)  colorblindToggle.onValueChanged.AddListener(_ => Apply());
            if (dyslexicToggle)    dyslexicToggle.onValueChanged.AddListener(_ => Apply());
            if (relaxedToggle)     relaxedToggle.onValueChanged.AddListener(_ => Apply());
            if (musicSlider)       musicSlider.onValueChanged.AddListener(_ => Apply());
            if (sfxSlider)         sfxSlider.onValueChanged.AddListener(_ => Apply());
            if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(_ => Apply());
            if (backButton)        backButton.onClick.AddListener(() => UIManager.Instance.Back());
        }

        void OnDisable()
        {
            // Au cas où le screen est désactivé sans GC.
            if (ttsToggle)         ttsToggle.onValueChanged.RemoveAllListeners();
            if (subtitlesToggle)   subtitlesToggle.onValueChanged.RemoveAllListeners();
            if (colorblindToggle)  colorblindToggle.onValueChanged.RemoveAllListeners();
            if (dyslexicToggle)    dyslexicToggle.onValueChanged.RemoveAllListeners();
            if (relaxedToggle)     relaxedToggle.onValueChanged.RemoveAllListeners();
            if (musicSlider)       musicSlider.onValueChanged.RemoveAllListeners();
            if (sfxSlider)         sfxSlider.onValueChanged.RemoveAllListeners();
            if (sensitivitySlider) sensitivitySlider.onValueChanged.RemoveAllListeners();
            if (backButton)        backButton.onClick.RemoveAllListeners();
        }

        void Apply()
        {
            if (initializing) return;
            cache.tts          = ttsToggle && ttsToggle.isOn;
            cache.subtitles    = subtitlesToggle && subtitlesToggle.isOn;
            cache.colorblind   = colorblindToggle && colorblindToggle.isOn;
            cache.dyslexicFont = dyslexicToggle && dyslexicToggle.isOn;
            cache.relaxedMode  = relaxedToggle && relaxedToggle.isOn;
            if (musicSlider)       cache.musicVolume       = musicSlider.value;
            if (sfxSlider)         cache.sfxVolume         = sfxSlider.value;
            if (sensitivitySlider) cache.cameraSensitivity = sensitivitySlider.value;
            SaveManager.SaveSettings(cache);
        }
    }
}
