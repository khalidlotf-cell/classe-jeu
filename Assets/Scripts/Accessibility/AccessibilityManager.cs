using System;
using TMPro;
using UnityEngine;

namespace MathsClass
{
    // Centralise l'accessibilité : TTS, sous-titres, OpenDyslexic, daltonien, mode détente.
    // Appliqué au démarrage et à chaque changement de Settings.
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [Header("Polices (assignées dans l'inspecteur)")]
        public TMP_FontAsset defaultFont;
        public TMP_FontAsset dyslexicFont;

        [Header("Daltonien")]
        public bool isColorblind { get; private set; }
        public bool isRelaxed { get; private set; }
        public bool subtitlesEnabled { get; private set; }
        public bool ttsEnabled { get; private set; }

        // Évènements écoutés par la HUD/sous-titres/dalles
        public static event Action<string> OnSpeak;       // texte à lire (TTS + sous-titres)
        public static event Action<bool>   OnFontChanged; // true = dyslexic
        public static event Action<bool>   OnColorblindChanged;

        Settings settings;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            settings = SaveManager.LoadSettings();
            Apply(settings);
            SaveManager.OnSettingsChanged += Apply;
        }

        void OnDestroy()
        {
            SaveManager.OnSettingsChanged -= Apply;
        }

        public void Apply(Settings s)
        {
            settings = s;
            isColorblind = s.colorblind;
            isRelaxed = s.relaxedMode;
            subtitlesEnabled = s.subtitles;
            ttsEnabled = s.tts;

            // Police globale TMP
            var target = (s.dyslexicFont && dyslexicFont) ? dyslexicFont : defaultFont;
            if (target)
            {
                foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    t.font = target;
                }
            }

            OnFontChanged?.Invoke(s.dyslexicFont);
            OnColorblindChanged?.Invoke(s.colorblind);
        }

        // Annonce un texte. Si TTS activé → essaie de lire.
        // Si sous-titres activés → SubtitleDisplay l'affiche.
        public void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            OnSpeak?.Invoke(text);
            if (ttsEnabled) TryNativeTTS(text);
        }

        // Stub TTS multi-plateforme.
        // - Éditeur macOS : utilise `say` via System.Diagnostics
        // - Sinon : silencieux (les sous-titres prennent le relais)
        // Convertit les symboles mathématiques en mots pour une lecture correcte.
        // Les moteurs TTS ne prononcent pas « − », « × », « ÷ » -> on les remplace.
        static string ToSpeakable(string text)
        {
            return text
                .Replace("+", " plus ")
                .Replace("−", " moins ")        // U+2212, le signe moins du jeu
                .Replace("-", " moins ")        // trait d'union ordinaire
                .Replace("×", " fois ")
                .Replace("*", " fois ")
                .Replace("÷", " divisé par ")
                .Replace("/", " divisé par ")
                .Replace("=", " égale ")
                .Replace("(", " parenthèse ")
                .Replace(")", " fermer parenthèse ");
        }

        void TryNativeTTS(string text)
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            try
            {
                string spoken = ToSpeakable(text);
                var p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "/usr/bin/say";
                p.StartInfo.Arguments = "-v Thomas \"" + spoken.Replace("\"", "") + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
            catch { /* fallback silencieux */ }
#endif
        }
    }
}
