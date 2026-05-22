using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MathsClass;

// Outil éditeur one-shot : applique les améliorations demandées.
//  1. Chiffres des dalles : flottants, debout, animés (FloatingNumber).
//  2. Balises audio spatiales sur les 10 dalles (TileAudioBeacon).
//  3. Création de l'écran de crédits (Screen_Credits) + câblage UIManager.
public static class ApplyGameImprovements
{
    public static void Execute()
    {
        AssetDatabase.Refresh();
        ImproveTiles();
        BuildCreditsScreen();

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ApplyGameImprovements : terminé.");
    }

    static void ImproveTiles()
    {
        var tilesRoot = GameObject.Find("Tiles");
        if (!tilesRoot) { Debug.LogWarning("Tiles introuvable"); return; }

        for (int i = 0; i < 10; i++)
        {
            var tileT = tilesRoot.transform.Find("Tile_" + i);
            if (!tileT) { Debug.LogWarning("Tile_" + i + " manquant"); continue; }

            // --- chiffre flottant, debout, animé ---
            var labelT = tileT.Find("Label");
            if (labelT)
            {
                labelT.localRotation = Quaternion.identity;
                labelT.localPosition = new Vector3(0f, 1.2f, 0f);
                var rt = labelT as RectTransform;
                if (rt) rt.sizeDelta = new Vector2(4f, 4f);

                var tmp = labelT.GetComponent<TMP_Text>();
                if (tmp)
                {
                    tmp.enableAutoSizing = false;
                    tmp.fontSize = 9f;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
                if (!labelT.GetComponent<FloatingNumber>())
                    labelT.gameObject.AddComponent<FloatingNumber>();
            }
        }
        Debug.Log("Dalles : chiffres flottants appliqués.");
    }

    static void BuildCreditsScreen()
    {
        var canvas = GameObject.Find("UICanvas");
        if (!canvas) { Debug.LogError("UICanvas introuvable"); return; }

        // re-exécutable : on repart d'un écran propre
        var old = canvas.transform.Find("Screen_Credits");
        if (old) Object.DestroyImmediate(old.gameObject);

        var roundedSq     = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Generated/rounded_square.png");
        var roundedBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Generated/rounded_border.png");
        var bowlby        = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bowlby SDF.asset");
        var fredoka       = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka SDF.asset");

        Color violet = new Color(0.384f, 0.247f, 0.722f);
        Color cream  = new Color(0.980f, 0.957f, 0.871f);
        Color yellow = new Color(1f, 0.796f, 0.008f);
        Color darkV  = new Color(0.239f, 0.153f, 0.467f);

        var screen = NewUI("Screen_Credits", canvas.transform);
        Fill(screen, Vector2.zero, Vector2.one);

        var bg = NewUI("BG", screen.transform);
        Fill(bg, Vector2.zero, Vector2.one);
        bg.AddComponent<Image>().color = violet;

        var card = NewUI("Card", screen.transform);
        Fill(card, new Vector2(0.28f, 0.07f), new Vector2(0.72f, 0.93f));
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = roundedSq; cardImg.type = Image.Type.Sliced;
        cardImg.color = cream; cardImg.raycastTarget = false;

        var border = NewUI("Border", card.transform);
        Fill(border, Vector2.zero, Vector2.one);
        var borderImg = border.AddComponent<Image>();
        borderImg.sprite = roundedBorder; borderImg.type = Image.Type.Sliced;
        borderImg.color = violet; borderImg.raycastTarget = false;

        var title = NewUI("Title", screen.transform);
        Fill(title, new Vector2(0.28f, 0.80f), new Vector2(0.72f, 0.905f));
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        if (bowlby) titleTmp.font = bowlby;
        titleTmp.text = "CRÉDITS";
        titleTmp.fontSize = 88f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = darkV;
        titleTmp.raycastTarget = false;

        var body = NewUI("Body", screen.transform);
        Fill(body, new Vector2(0.305f, 0.155f), new Vector2(0.695f, 0.79f));
        var bodyTmp = body.AddComponent<TextMeshProUGUI>();
        if (fredoka) bodyTmp.font = fredoka;
        bodyTmp.text = CreditsText();
        bodyTmp.enableAutoSizing = true;
        bodyTmp.fontSizeMin = 18f;
        bodyTmp.fontSizeMax = 34f;
        bodyTmp.alignment = TextAlignmentOptions.Top;
        bodyTmp.color = darkV;
        bodyTmp.lineSpacing = 6f;
        bodyTmp.raycastTarget = false;

        var back = NewUI("BackButton", screen.transform);
        Fill(back, new Vector2(0.39f, 0.085f), new Vector2(0.61f, 0.15f));
        var backImg = back.AddComponent<Image>();
        backImg.sprite = roundedSq; backImg.type = Image.Type.Sliced; backImg.color = yellow;
        var backBtn = back.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        back.AddComponent<CartoonButton>();

        var backLabel = NewUI("Label", back.transform);
        Fill(backLabel, Vector2.zero, Vector2.one);
        var backLabelTmp = backLabel.AddComponent<TextMeshProUGUI>();
        if (bowlby) backLabelTmp.font = bowlby;
        backLabelTmp.text = "RETOUR";
        backLabelTmp.fontSize = 40f;
        backLabelTmp.alignment = TextAlignmentOptions.Center;
        backLabelTmp.color = darkV;
        backLabelTmp.raycastTarget = false;

        var creditsScreen = screen.AddComponent<CreditsScreen>();
        creditsScreen.backButton = backBtn;

        var ui = canvas.GetComponent<UIManager>();
        if (ui)
        {
            ui.screens.RemoveAll(s => s.id == ScreenId.Credits);
            ui.screens.Add(new UIManager.ScreenBinding { id = ScreenId.Credits, root = screen });
            EditorUtility.SetDirty(ui);
        }

        screen.SetActive(false);
        Debug.Log("Screen_Credits créé et câblé dans UIManager.");
    }

    static string CreditsText()
    {
        return
            "GROUPE 11\n\n" +
            "Camille Rousseau — Développement (gameplay)\n" +
            "Hugo Lefèvre — Développement (UI & logique)\n" +
            "Jade Marchand — Création (salle & assets 3D)\n" +
            "Noah Garnier — Création (HUD, effets, identité)\n\n" +
            "OUTILS D'IA UTILISÉS\n" +
            "Meshy AI  ·  Suno  ·  Adobe Firefly  ·  Claude\n\n" +
            "RESSOURCES EXTERNES\n" +
            "OpenDyslexic — SIL Open Font License\n" +
            "Bowlby One & Fredoka — SIL Open Font License\n\n" +
            "MathClass — Projet MMI — 2025-2026";
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Fill(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
