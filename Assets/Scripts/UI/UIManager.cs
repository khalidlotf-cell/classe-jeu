using System.Collections.Generic;
using UnityEngine;

namespace MathsClass
{
    public enum ScreenId
    {
        MainMenu,
        ModeSelect,
        Settings,
        Scores,
        Pause,
        GameOver,
        Credits
    }

    // Switcher d'écrans simple + pile pour le bouton "Retour".
    // Chaque écran est un GameObject racine avec un script qui implémente IScreen.
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [System.Serializable]
        public class ScreenBinding
        {
            public ScreenId id;
            public GameObject root;
        }

        public List<ScreenBinding> screens = new List<ScreenBinding>();

        readonly Dictionary<ScreenId, GameObject> map = new Dictionary<ScreenId, GameObject>();
        readonly Stack<ScreenId> history = new Stack<ScreenId>();

        public ScreenId currentScreen { get; private set; } = ScreenId.MainMenu;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            foreach (var s in screens) if (s.root) map[s.id] = s.root;
            HideAll();
        }

        void Start()
        {
            // Affiche par défaut le menu principal s'il existe.
            if (map.ContainsKey(ScreenId.MainMenu)) Show(ScreenId.MainMenu, pushHistory: false);
        }

        public void Show(ScreenId id, bool pushHistory = true)
        {
            if (!map.TryGetValue(id, out var go) || !go) return;
            if (pushHistory && currentScreen != id) history.Push(currentScreen);
            HideAllInternal();
            go.SetActive(true);
            currentScreen = id;
        }

        public void Hide(ScreenId id)
        {
            if (map.TryGetValue(id, out var go) && go) go.SetActive(false);
        }

        public void Back()
        {
            if (history.Count == 0) { Show(ScreenId.MainMenu, false); return; }
            var prev = history.Pop();
            HideAllInternal();
            if (map.TryGetValue(prev, out var go) && go) go.SetActive(true);
            currentScreen = prev;
        }

        public void HideAll()
        {
            HideAllInternal();
            history.Clear();
        }

        void HideAllInternal()
        {
            foreach (var kv in map) if (kv.Value) kv.Value.SetActive(false);
        }
    }
}
