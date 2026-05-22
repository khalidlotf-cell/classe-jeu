using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MathsClass;

public static class WireUIScreens
{
    public static void Execute()
    {
        var canvas = GameObject.Find("UICanvas");
        if (!canvas) { Debug.LogError("UICanvas introuvable"); return; }
        var ui = canvas.GetComponent<UIManager>();
        if (!ui) { Debug.LogError("UIManager introuvable"); return; }

        ui.screens.Clear();
        var pairs = new (ScreenId id, string name)[]
        {
            (ScreenId.MainMenu,   "Screen_MainMenu"),
            (ScreenId.ModeSelect, "Screen_ModeSelect"),
            (ScreenId.Settings,   "Screen_Settings"),
            (ScreenId.Scores,     "Screen_Scores"),
            (ScreenId.Pause,      "Screen_Pause"),
            (ScreenId.GameOver,   "Screen_GameOver"),
            (ScreenId.Credits,    "Screen_Credits"),
        };

        foreach (var (id, name) in pairs)
        {
            var t = canvas.transform.Find(name);
            if (!t) { Debug.LogWarning($"{name} manquant"); continue; }
            ui.screens.Add(new UIManager.ScreenBinding { id = id, root = t.gameObject });
            t.gameObject.SetActive(id == ScreenId.MainMenu);
        }

        EditorUtility.SetDirty(ui);
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"UIManager câblé : {ui.screens.Count} écrans");
    }
}
