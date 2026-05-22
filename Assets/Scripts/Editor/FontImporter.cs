using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathsClassEditor
{
    public static class FontImporter
    {
        // Importe Assets/Fonts/Fredoka.ttf, génère un TMP Font Asset SDF, et l'applique
        // à tous les TMP_Text de la scène + à AccessibilityManager.defaultFont.
        public static void Execute()
        {
            // On utilise Bowlby One SC : bubble cartoon très proche du logo MathsClass.
            string ttfPath = "Assets/Fonts/Bowlby.ttf";
            if (!File.Exists(ttfPath))
            {
                Debug.LogError($"TTF introuvable : {ttfPath}");
                return;
            }
            AssetDatabase.ImportAsset(ttfPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (!font) { Debug.LogError("Font import a échoué."); return; }

            string assetPath = "Assets/Fonts/Bowlby SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            TMP_FontAsset fontAsset;
            if (existing) { fontAsset = existing; }
            else
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(font);
                fontAsset.name = "Bowlby SDF";
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"TMP Font Asset créé : {assetPath}");
            }

            // Font fallback (Fredoka) pour caractères que Bowlby SC ne couvre pas
            string fallbackTtf = "Assets/Fonts/Fredoka.ttf";
            if (File.Exists(fallbackTtf))
            {
                AssetDatabase.ImportAsset(fallbackTtf);
                var fbFont = AssetDatabase.LoadAssetAtPath<Font>(fallbackTtf);
                string fbPath = "Assets/Fonts/Fredoka SDF.asset";
                var fbExisting = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fbPath);
                TMP_FontAsset fbAsset = fbExisting;
                if (!fbAsset && fbFont)
                {
                    fbAsset = TMP_FontAsset.CreateFontAsset(fbFont);
                    fbAsset.name = "Fredoka SDF";
                    AssetDatabase.CreateAsset(fbAsset, fbPath);
                    AssetDatabase.SaveAssets();
                }
                if (fbAsset)
                {
                    if (fontAsset.fallbackFontAssetTable == null)
                        fontAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    if (!fontAsset.fallbackFontAssetTable.Contains(fbAsset))
                        fontAsset.fallbackFontAssetTable.Add(fbAsset);
                    EditorUtility.SetDirty(fontAsset);
                }
            }

            // Charger la scène si nécessaire
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // Appliquer à tous les TMP_Text et TextMeshPro du scène
            int count = 0;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                t.font = fontAsset;
                EditorUtility.SetDirty(t);
                count++;
            }
            // Aussi assigner sur AccessibilityManager.defaultFont (si présent)
            var access = Object.FindObjectsByType<MathsClass.AccessibilityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var a in access)
            {
                a.defaultFont = fontAsset;
                EditorUtility.SetDirty(a);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Police Fredoka appliquée à {count} TMP_Text. Scène sauvegardée.");
        }
    }
}
