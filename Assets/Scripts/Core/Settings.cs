using System;

namespace MathsClass
{
    // Données utilisateur persistées (volumes, accessibilité, sensibilité).
    // Chargées/sauvées par SaveManager via PlayerPrefs.
    [Serializable]
    public class Settings
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float cameraSensitivity = 0.5f;

        public bool tts = false;
        public bool subtitles = false;
        public bool colorblind = false;
        public bool dyslexicFont = false;
        public bool relaxedMode = false;

        public static Settings Default => new Settings();
    }
}
