using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MathsClass;

public static class WireUIButtons
{
    public static void Execute()
    {
        var canvas = GameObject.Find("UICanvas");
        if (!canvas) { Debug.LogError("UICanvas introuvable"); return; }

        WireModes(canvas.transform.Find("Screen_ModeSelect"));
        WireScores(canvas.transform.Find("Screen_Scores"));

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("WireUIButtons terminé");
    }

    static void WireModes(Transform root)
    {
        if (!root) { Debug.LogWarning("Screen_ModeSelect introuvable"); return; }
        var screen = root.GetComponent<ModeSelectScreen>();
        if (!screen) { Debug.LogWarning("ModeSelectScreen introuvable"); return; }

        var pairs = new (GameMode mode, string name)[]
        {
            (GameMode.Classique, "ClassiqueBtn"),
            (GameMode.Detente,   "DetenteBtn"),
            (GameMode.Speedrun,  "SpeedrunBtn"),
            (GameMode.Infini,    "InfiniBtn"),
            (GameMode.Survie,    "SurvieBtn"),
        };

        var list = new System.Collections.Generic.List<ModeSelectScreen.ModeButton>();
        foreach (var (mode, name) in pairs)
        {
            var t = root.Find(name);
            if (!t) { Debug.LogWarning($"Bouton {name} introuvable"); continue; }
            var btn = t.GetComponent<Button>();
            if (!btn) { Debug.LogWarning($"{name} sans Button"); continue; }
            list.Add(new ModeSelectScreen.ModeButton { mode = mode, button = btn });
        }
        screen.modeButtons = list.ToArray();
        EditorUtility.SetDirty(screen);
        Debug.Log($"ModeSelect câblé : {list.Count} boutons");
    }

    static void WireScores(Transform root)
    {
        if (!root) { Debug.LogWarning("Screen_Scores introuvable"); return; }
        var screen = root.GetComponent<ScoresScreen>();
        if (!screen) { Debug.LogWarning("ScoresScreen introuvable"); return; }

        var rows = new ScoresScreen.Row[5];
        for (int i = 0; i < 5; i++)
        {
            rows[i] = new ScoresScreen.Row
            {
                rank   = root.Find($"R{i}_rank")?.GetComponent<TMP_Text>(),
                player = root.Find($"R{i}_player")?.GetComponent<TMP_Text>(),
                score  = root.Find($"R{i}_score")?.GetComponent<TMP_Text>(),
                mode   = root.Find($"R{i}_mode")?.GetComponent<TMP_Text>(),
            };
        }
        screen.rows = rows;
        EditorUtility.SetDirty(screen);
        Debug.Log("Scores rows câblées");
    }
}
