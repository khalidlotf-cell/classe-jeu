using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class RegenFonts
{
    public static void Execute()
    {
        Regen("Assets/Fonts/Bowlby SDF.asset",   "Assets/Fonts/Bowlby.ttf");
        Regen("Assets/Fonts/Fredoka SDF.asset",  "Assets/Fonts/Fredoka.ttf");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Regen(string assetPath, string ttfPath)
    {
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (!ttf) { Debug.LogError($"TTF introuvable: {ttfPath}"); return; }

        var fresh = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (!fresh) { Debug.LogError($"Échec génération pour {ttfPath}"); return; }
        fresh.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing)
        {
            EditorUtility.CopySerialized(fresh, existing);
            // sub-assets : remplace l'atlas-texture sub-asset
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (sub is Texture2D || sub is Material)
                    if (sub != existing) Object.DestroyImmediate(sub, true);

            if (fresh.atlasTextures != null)
                foreach (var tx in fresh.atlasTextures)
                    if (tx) AssetDatabase.AddObjectToAsset(tx, existing);

            if (fresh.material) AssetDatabase.AddObjectToAsset(fresh.material, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Régénéré (in-place): {assetPath}");
        }
        else
        {
            AssetDatabase.CreateAsset(fresh, assetPath);
            if (fresh.atlasTextures != null)
                foreach (var tx in fresh.atlasTextures)
                    if (tx) AssetDatabase.AddObjectToAsset(tx, fresh);
            if (fresh.material) AssetDatabase.AddObjectToAsset(fresh.material, fresh);
            Debug.Log($"Créé: {assetPath}");
        }
    }
}
