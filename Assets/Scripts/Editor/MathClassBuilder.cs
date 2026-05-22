using System.Collections.Generic;
using System.IO;
using MathsClass;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MathsClassEditor
{
    // Builder unique : MathClass > Build Game (V3) reconstruit la scène complète :
    // - 3D classroom (sol, murs, bureaux, tableau, posters, plantes)
    // - 10 dalles 0-9 + Player FPS
    // - Managers (GameManager, AudioManager, FXManager, AccessibilityManager, UIManager)
    // - Canvas avec 6 écrans (MainMenu, ModeSelect, Settings, Pause, GameOver, Scores)
    //   utilisant les maquettes comme fond + boutons interactifs superposés
    // - HUD (score, chrono, cœurs, combo, palier) + flash overlay + sous-titres
    public static class MathClassBuilder
    {
        // ---------- Palette officielle ----------
        static readonly Color VioletPrimary  = new Color32(0x62, 0x3F, 0xB8, 0xFF); // #623FB8
        static readonly Color VioletDark     = new Color32(0x3D, 0x27, 0x77, 0xFF);
        static readonly Color YellowAccent   = new Color32(0xFF, 0xCB, 0x02, 0xFF); // #FFCB02
        static readonly Color WhiteUI        = Color.white;
        static readonly Color WallCream      = new Color32(0xFF, 0xE8, 0xC2, 0xFF);
        static readonly Color WallPeach      = new Color32(0xFF, 0xD8, 0xB0, 0xFF);
        static readonly Color FloorTile      = new Color32(0xF6, 0xD9, 0xA8, 0xFF);
        static readonly Color CeilingCream   = new Color32(0xFF, 0xF6, 0xE6, 0xFF);
        static readonly Color BlackboardGreen = new Color32(0x3E, 0x82, 0x4E, 0xFF);
        static readonly Color BlackboardFrame = new Color32(0x97, 0x60, 0x36, 0xFF);
        static readonly Color DeskWood        = new Color32(0xD9, 0x9C, 0x60, 0xFF);
        static readonly Color DeskTop         = new Color32(0xF2, 0xC8, 0x8E, 0xFF);

        // Couleurs des dalles 0-9 (cartoon, contrastées, pour daltoniens utilisez ApplyColorblind)
        static readonly Color[] TileColors =
        {
            new Color32(0xFF, 0x6B, 0x6B, 0xFF), // 0 rouge
            new Color32(0xFF, 0xA5, 0x4D, 0xFF), // 1 orange
            new Color32(0xFF, 0xE6, 0x4D, 0xFF), // 2 jaune
            new Color32(0x9B, 0xE0, 0x6B, 0xFF), // 3 vert clair
            new Color32(0x4D, 0xC1, 0x7C, 0xFF), // 4 vert foncé
            new Color32(0x4D, 0xC9, 0xE0, 0xFF), // 5 cyan
            new Color32(0x4D, 0x8A, 0xE0, 0xFF), // 6 bleu
            new Color32(0xA0, 0x6B, 0xE0, 0xFF), // 7 violet
            new Color32(0xE0, 0x6B, 0xC4, 0xFF), // 8 rose
            new Color32(0xC4, 0xC4, 0xC4, 0xFF), // 9 gris
        };

        // ---------- Dossiers ----------
        const string GeneratedDir = "Assets/UI/Generated";

        [MenuItem("MathClass/Build Game (V3)")]
        public static void BuildAll()
        {
            EnsureTMPEssentials();
            EnsurePlayerTag();
            EnsureFolders();

            // Charger sprites maquettes (importés dans Assets/Maquettes/)
            var maquettes = LoadMaquettes();

            // Générer/charger sprites cartoon (rounded rect, etc.)
            var sprites = GenerateUISprites();

            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            BuildLighting();
            BuildRoom();
            var blackboardText = BuildBlackboard();
            var tiles = BuildTiles();
            var (playerGO, playerCtrl, spawnT, playerCam) = BuildPlayer();

            var managersGO = new GameObject("Managers");
            // Audio
            var audioMgr = managersGO.AddComponent<AudioManager>();
            var sfxSrc = managersGO.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false; sfxSrc.spatialBlend = 0f;
            var musicSrc = managersGO.AddComponent<AudioSource>();
            musicSrc.playOnAwake = false; musicSrc.spatialBlend = 0f; musicSrc.volume = 0.35f;
            audioMgr.sfx = sfxSrc; audioMgr.music = musicSrc;
            // Accessibilité
            var access = managersGO.AddComponent<AccessibilityManager>();

            // FX
            var fx = managersGO.AddComponent<FXManager>();
            fx.mainCam = playerCam;
            fx.confettiPrefab = MakeConfettiTemplate();

            // TileManager
            var tileMgrGO = new GameObject("TileManager");
            var tileMgr = tileMgrGO.AddComponent<TileManager>();
            tileMgr.tiles = tiles;

            // GameManager
            var gm = managersGO.AddComponent<GameManager>();
            gm.blackboardText = blackboardText;
            gm.player = playerCtrl;
            gm.playerSpawn = spawnT;
            gm.tileManager = tileMgr;
            gm.audioMgr = audioMgr;
            gm.fxMgr = fx;

            // ---------- UI ----------
            var ui = BuildUI(sprites, maquettes, fx, playerCam);
            gm.hud = ui.hud;
            gm.hudGroup = ui.hudGroup;
            fx.flashImage = ui.flashImage;

            // ---------- Police cartoon (Bowlby) appliquée à tous les TMP_Text ----------
            ApplyCartoonFont(access);

            // ---------- Final ----------
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MathClass V3 scene built. Press Play.");
        }

        // ====================================================================
        // 3D ENVIRONMENT
        // ====================================================================

        static void BuildLighting()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.05f;
            l.color = new Color(1f, 0.97f, 0.88f);
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, 30, 0);

            var fill = new GameObject("Fill");
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.intensity = 0.35f;
            fl.color = new Color(0.85f, 0.92f, 1f);
            fill.transform.rotation = Quaternion.Euler(40, 210, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.88f, 0.99f);
            RenderSettings.ambientEquatorColor = new Color(0.96f, 0.92f, 0.85f);
            RenderSettings.ambientGroundColor = new Color(0.65f, 0.55f, 0.45f);
        }

        static void BuildRoom()
        {
            // Sol
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2.6f, 1, 2.6f);
            Paint(floor, FloorTile);

            // Murs
            Wall("Wall_North", new Vector3(0, 2.5f, 13f), new Vector3(26, 5, 0.4f), WallCream);
            Wall("Wall_South", new Vector3(0, 2.5f, -13f), new Vector3(26, 5, 0.4f), WallCream);
            Wall("Wall_East",  new Vector3(13f, 2.5f, 0), new Vector3(0.4f, 5, 26), WallPeach);
            Wall("Wall_West",  new Vector3(-13f, 2.5f, 0), new Vector3(0.4f, 5, 26), WallPeach);

            // Plafond
            var ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceil.name = "Ceiling";
            ceil.transform.position = new Vector3(0, 5.05f, 0);
            ceil.transform.localScale = new Vector3(26, 0.3f, 26);
            Paint(ceil, CeilingCream);

            // Bureaux à l'arrière (décoratifs)
            var desks = new GameObject("Desks");
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4; i++)
                    BuildDesk(desks.transform, new Vector3(-7.5f + i * 5f, 0, -10f + row * 2.5f));

            // Posters violet/jaune sur les murs (aspect maquette)
            Poster("Poster_1", new Vector3(-9, 3.5f, 12.78f), Quaternion.identity, YellowAccent);
            Poster("Poster_2", new Vector3(9, 3.5f, 12.78f), Quaternion.identity, VioletPrimary);
            Poster("Poster_3", new Vector3(-12.78f, 3.5f, 0), Quaternion.Euler(0, 90, 0), new Color(0.99f, 0.92f, 0.69f));
            Poster("Poster_4", new Vector3(12.78f, 3.5f, 5), Quaternion.Euler(0, -90, 0), new Color(0.79f, 0.89f, 0.99f));

            // Plantes en coin
            BuildPlant(new Vector3(-12, 0, -12));
            BuildPlant(new Vector3(12, 0, -12));
            BuildPlant(new Vector3(-12, 0, 11.5f));
            BuildPlant(new Vector3(12, 0, 11.5f));
        }

        static void Wall(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.position = pos;
            w.transform.localScale = scale;
            Paint(w, color);
        }

        static void BuildDesk(Transform parent, Vector3 root)
        {
            var holder = new GameObject("Desk");
            holder.transform.SetParent(parent);
            holder.transform.position = root;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Top";
            top.transform.SetParent(holder.transform, false);
            top.transform.localPosition = new Vector3(0, 0.85f, 0);
            top.transform.localScale = new Vector3(2.4f, 0.12f, 1.2f);
            Paint(top, DeskTop);

            for (int i = 0; i < 4; i++)
            {
                float lx = (i % 2 == 0 ? -1 : 1) * 1.05f;
                float lz = (i / 2 == 0 ? -1 : 1) * 0.5f;
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Leg";
                leg.transform.SetParent(holder.transform, false);
                leg.transform.localPosition = new Vector3(lx, 0.4f, lz);
                leg.transform.localScale = new Vector3(0.12f, 0.85f, 0.12f);
                Paint(leg, DeskWood);
            }

            var book = GameObject.CreatePrimitive(PrimitiveType.Cube);
            book.name = "Book";
            book.transform.SetParent(holder.transform, false);
            book.transform.localPosition = new Vector3(0.5f, 0.95f, 0);
            book.transform.localScale = new Vector3(0.5f, 0.08f, 0.4f);
            book.transform.localRotation = Quaternion.Euler(0, Random.Range(-15f, 15f), 0);
            Paint(book, VioletPrimary);
        }

        static void Poster(string name, Vector3 pos, Quaternion rot, Color color)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = name;
            p.transform.position = pos;
            p.transform.rotation = rot;
            p.transform.localScale = new Vector3(2.5f, 1.5f, 0.05f);
            Paint(p, color);
        }

        static void BuildPlant(Vector3 root)
        {
            var holder = new GameObject("Plant");
            holder.transform.position = root;

            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "Pot";
            pot.transform.SetParent(holder.transform, false);
            pot.transform.localPosition = new Vector3(0, 0.3f, 0);
            pot.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            Paint(pot, VioletPrimary);

            for (int i = 0; i < 3; i++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "Foliage_" + i;
                leaf.transform.SetParent(holder.transform, false);
                leaf.transform.localPosition = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    0.85f + i * 0.18f,
                    Random.Range(-0.25f, 0.25f));
                float s = Random.Range(0.55f, 0.75f);
                leaf.transform.localScale = new Vector3(s, s, s);
                Paint(leaf, new Color(0.45f + Random.Range(-0.1f, 0.1f), 0.78f, 0.55f));
            }
        }

        static TMP_Text BuildBlackboard()
        {
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BlackboardFrame";
            frame.transform.position = new Vector3(0, 3f, 12.78f);
            frame.transform.localScale = new Vector3(11.5f, 4.4f, 0.25f);
            Paint(frame, BlackboardFrame);

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Blackboard";
            board.transform.position = new Vector3(0, 3f, 12.62f);
            board.transform.localScale = new Vector3(10.8f, 3.7f, 0.05f);
            Paint(board, BlackboardGreen);

            var eq = new GameObject("EquationText");
            eq.transform.position = new Vector3(0, 3f, 12.55f);
            eq.transform.rotation = Quaternion.identity;
            var tmp = eq.AddComponent<TextMeshPro>();
            tmp.text = "1 + 1 = ?";
            tmp.fontSize = 8;
            tmp.color = new Color(1f, 1f, 0.95f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            var rt = eq.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10, 3);
            return tmp;
        }

        static Tile[] BuildTiles()
        {
            var parent = new GameObject("Tiles");
            var arr = new Tile[10];
            for (int i = 0; i < 10; i++)
            {
                float x = -9f + i * 2f;
                float z = 5f;

                var root = new GameObject("Tile_" + i);
                root.transform.SetParent(parent.transform);
                root.transform.position = new Vector3(x, 0.1f, z);

                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = new Vector3(1.8f, 0.2f, 1.8f);
                Paint(visual, TileColors[i]);

                var trig = new GameObject("Trigger");
                trig.transform.SetParent(root.transform, false);
                trig.transform.localPosition = new Vector3(0, 1.5f, 0);
                var bc = trig.AddComponent<BoxCollider>();
                bc.size = new Vector3(1.8f, 3f, 1.8f);
                bc.isTrigger = true;
                var tcomp = trig.AddComponent<Tile>();
                tcomp.number = i;
                tcomp.visual = visual.transform;
                tcomp.visualRenderer = visual.GetComponent<Renderer>();

                // Particules à l'impact
                var partGO = new GameObject("StepParticles");
                partGO.transform.SetParent(root.transform, false);
                partGO.transform.localPosition = new Vector3(0, 0.15f, 0);
                var ps = partGO.AddComponent<ParticleSystem>();
                ConfigureStepParticles(ps, TileColors[i]);
                tcomp.stepParticles = ps;

                // Texte du chiffre sur la dalle (visible d'en haut)
                var label = new GameObject("Label");
                label.transform.SetParent(root.transform, false);
                label.transform.localPosition = new Vector3(0, 0.16f, 0);
                label.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var ltmp = label.AddComponent<TextMeshPro>();
                ltmp.text = i.ToString();
                ltmp.fontSize = 4;
                ltmp.color = Color.white;
                ltmp.alignment = TextAlignmentOptions.Center;
                ltmp.fontStyle = FontStyles.Bold;
                ltmp.enableWordWrapping = false;
                var lrt = label.GetComponent<RectTransform>();
                lrt.sizeDelta = new Vector2(1.5f, 1.5f);
                ltmp.outlineColor = new Color32(20, 20, 20, 255);
                ltmp.outlineWidth = 0.2f;
                tcomp.numberLabel = ltmp;

                // Forme daltonien (10 formes différentes via primitives)
                var shape = MakeColorblindShape(i);
                shape.name = "ColorblindShape";
                shape.transform.SetParent(root.transform, false);
                shape.transform.localPosition = new Vector3(0, 0.3f, 0);
                shape.transform.localScale = Vector3.one * 0.5f;
                Paint(shape, Color.white);
                shape.SetActive(false);
                tcomp.colorblindShape = shape;

                arr[i] = tcomp;
            }
            return arr;
        }

        static GameObject MakeColorblindShape(int n)
        {
            switch (n % 5)
            {
                case 0: return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                case 1: return GameObject.CreatePrimitive(PrimitiveType.Cube);
                case 2: return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                case 3: return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                default:
                    var go = new GameObject("Tetra");
                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    mf.sharedMesh = MakeTetra();
                    mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    return go;
            }
        }

        static Mesh MakeTetra()
        {
            Vector3[] v = {
                new Vector3(0, 1, 0), new Vector3(-0.866f, -0.5f, -0.5f),
                new Vector3(0.866f, -0.5f, -0.5f), new Vector3(0, -0.5f, 0.866f)
            };
            int[] t = { 0,2,1, 0,3,2, 0,1,3, 1,2,3 };
            var m = new Mesh { vertices = v, triangles = t };
            m.RecalculateNormals();
            return m;
        }

        static void ConfigureStepParticles(ParticleSystem ps, Color c)
        {
            var main = ps.main;
            main.duration = 0.3f; main.loop = false;
            main.startLifetime = 0.6f; main.startSpeed = 2.2f;
            main.startSize = 0.18f; main.startColor = c;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.4f; main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.5f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.2f));
            sz.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        static ParticleSystem MakeConfettiTemplate()
        {
            var go = new GameObject("ConfettiTemplate");
            go.SetActive(false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.2f; main.loop = false;
            main.startLifetime = 1.6f; main.startSpeed = 7f;
            main.startSize = 0.22f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f; shape.radius = 0.1f;

            main.startColor = new ParticleSystem.MinMaxGradient(BuildPartyGradient());

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            return ps;
        }

        static Gradient BuildPartyGradient()
        {
            var g = new Gradient();
            Color[] colors = {
                new Color(0.99f, 0.45f, 0.55f), new Color(0.99f, 0.78f, 0.42f),
                new Color(0.99f, 0.92f, 0.45f), new Color(0.55f, 0.93f, 0.60f),
                new Color(0.55f, 0.85f, 0.97f), new Color(0.78f, 0.65f, 0.97f),
            };
            var ck = new GradientColorKey[colors.Length];
            var ak = new GradientAlphaKey[2];
            for (int i = 0; i < colors.Length; i++)
                ck[i] = new GradientColorKey(colors[i], i / (float)(colors.Length - 1));
            ak[0] = new GradientAlphaKey(1, 0);
            ak[1] = new GradientAlphaKey(1, 1);
            g.SetKeys(ck, ak);
            return g;
        }

        static (GameObject root, PlayerController ctrl, Transform spawn, Camera cam) BuildPlayer()
        {
            var p = new GameObject("Player");
            p.tag = "Player";
            p.transform.position = new Vector3(0, 1f, -8f);
            var cc = p.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.4f;
            var pc = p.AddComponent<PlayerController>();

            var camGO = new GameObject("PlayerCamera");
            camGO.transform.SetParent(p.transform, false);
            camGO.transform.localPosition = new Vector3(0, 1.65f, 0);
            var camComp = camGO.AddComponent<Camera>();
            camComp.fieldOfView = 70;
            camComp.clearFlags = CameraClearFlags.Skybox;
            camComp.backgroundColor = new Color(0.78f, 0.88f, 0.99f);
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
            pc.cam = camGO.transform;

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0, 1f, -8f);

            return (p, pc, spawn.transform, camComp);
        }

        // ====================================================================
        // UI : 6 écrans + HUD utilisant les maquettes comme fond
        // ====================================================================

        public class UIBuilt
        {
            public CanvasGroup hudGroup;
            public HUDController hud;
            public Image flashImage;
        }

        class Maquettes
        {
            public Sprite accueil, modes, parametres, pause, gameOver, scores;
        }

        class GeneratedSprites
        {
            public Sprite roundedSquare;       // pour overlays / boutons invisibles
            public Sprite roundedSquareBorder; // contour seul (hover)
            public Sprite circle;
            public Sprite heart;
        }

        static Maquettes LoadMaquettes()
        {
            var m = new Maquettes();
            // NOTE : noms de fichiers inversés (vérifié visuellement)
            // M_1_accueil contient l'écran CHOISIR UN MODE
            // M_2_menu contient l'écran d'accueil MathsClass / JOUER
            m.accueil    = LoadSprite("Assets/Maquettes/M_2_menu.png.jpg");
            m.modes      = LoadSprite("Assets/Maquettes/M_1_accueil.png");
            m.parametres = LoadSprite("Assets/Maquettes/M_3_parametres.jpg");
            m.pause      = LoadSprite("Assets/Maquettes/M_5_pause.jpg");
            m.gameOver   = LoadSprite("Assets/Maquettes/M_6_game_over.jpg");
            m.scores     = LoadSprite("Assets/Maquettes/M_7_scores.jpg");
            return m;
        }

        static Sprite LoadSprite(string path)
        {
            // Force re-import as Sprite type
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static GeneratedSprites GenerateUISprites()
        {
            var g = new GeneratedSprites();
            g.roundedSquare       = MakeOrLoadRoundedRect("rounded_square.png", 256, 64, Color.white, default, 0);
            g.roundedSquareBorder = MakeOrLoadRoundedRect("rounded_border.png", 256, 64, new Color(1,1,1,0), VioletPrimary, 8);
            g.circle = MakeOrLoadCircle("circle.png", 128, Color.white);
            g.heart  = MakeOrLoadHeart("heart.png", 128, new Color32(0xFF, 0x55, 0x77, 0xFF));
            return g;
        }

        static Sprite MakeOrLoadRoundedRect(string filename, int size, int corner, Color fill, Color border, int borderWidth)
        {
            string path = $"{GeneratedDir}/{filename}";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color clear = new Color(0, 0, 0, 0);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dxL = x; int dxR = size - 1 - x;
                    int dyB = y; int dyT = size - 1 - y;
                    int dx = Mathf.Min(dxL, dxR);
                    int dy = Mathf.Min(dyB, dyT);
                    Color c = fill;
                    if (dx < corner && dy < corner)
                    {
                        float rd = Mathf.Sqrt((corner - dx) * (corner - dx) + (corner - dy) * (corner - dy));
                        if (rd > corner) c = clear;
                        else if (borderWidth > 0 && rd > corner - borderWidth) c = border;
                        else c = fill;
                    }
                    else if (borderWidth > 0)
                    {
                        int margin = Mathf.Min(dx, dy);
                        if (margin < borderWidth) c = border;
                    }
                    tex.SetPixel(x, y, c);
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            ConfigureSpriteImporter(path, corner);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite MakeOrLoadCircle(string filename, int size, Color fill)
        {
            string path = $"{GeneratedDir}/{filename}";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                int r = size / 2;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                    tex.SetPixel(x, y, d < r ? fill : new Color(0, 0, 0, 0));
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            ConfigureSpriteImporter(path, 0);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite MakeOrLoadHeart(string filename, int size, Color fill)
        {
            string path = $"{GeneratedDir}/{filename}";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color clear = new Color(0, 0, 0, 0);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Heart equation: ((x^2+y^2-1)^3 - x^2 y^3) ≤ 0 (centered, normalized)
                    float nx = (x - size * 0.5f) / (size * 0.42f);
                    float ny = (size * 0.55f - y) / (size * 0.42f);
                    float lhs = (nx * nx + ny * ny - 1f);
                    lhs = lhs * lhs * lhs - nx * nx * ny * ny * ny;
                    tex.SetPixel(x, y, lhs <= 0f ? fill : clear);
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            ConfigureSpriteImporter(path, 0);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ConfigureSpriteImporter(string path, int border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            if (border > 0)
            {
                importer.spriteBorder = new Vector4(border, border, border, border);
            }
            importer.SaveAndReimport();
        }

        static UIBuilt BuildUI(GeneratedSprites sprites, Maquettes maquettes, FXManager fx, Camera cam)
        {
            // EventSystem (new Input System)
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var uiModule = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null) uiModule.actionsAsset = actionsAsset;
            else uiModule.AssignDefaultActions();

            // Canvas
            var canvasGO = new GameObject("UICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ===== HUD =====
            var hudGO = new GameObject("HUD", typeof(RectTransform), typeof(CanvasGroup));
            hudGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(hudGO);
            var hudGroup = hudGO.GetComponent<CanvasGroup>();
            var hud = hudGO.AddComponent<HUDController>();
            BuildHUD(hudGO.transform, hud, sprites);

            // ===== Flash overlay =====
            var flashGO = new GameObject("FlashOverlay", typeof(RectTransform));
            flashGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(flashGO);
            var flashImg = flashGO.AddComponent<Image>();
            flashImg.color = new Color(1, 0.1f, 0.1f, 0f);
            flashImg.raycastTarget = false;

            // ===== Subtitles =====
            BuildSubtitles(canvasGO.transform, sprites);

            // ===== Screens =====
            var mainMenu     = BuildMainMenu(canvasGO.transform, maquettes.accueil, sprites);
            var modeSelect   = BuildModeSelect(canvasGO.transform, maquettes.modes, sprites);
            var settings     = BuildSettings(canvasGO.transform, maquettes.parametres, sprites);
            var scoresScreen = BuildScores(canvasGO.transform, maquettes.scores, sprites);
            var pauseScreen  = BuildPause(canvasGO.transform, maquettes.pause, sprites);
            var gameOver     = BuildGameOver(canvasGO.transform, maquettes.gameOver, sprites);

            // ===== UIManager =====
            var uiMgr = canvasGO.AddComponent<UIManager>();
            uiMgr.screens = new List<UIManager.ScreenBinding>
            {
                new UIManager.ScreenBinding { id = ScreenId.MainMenu,   root = mainMenu },
                new UIManager.ScreenBinding { id = ScreenId.ModeSelect, root = modeSelect },
                new UIManager.ScreenBinding { id = ScreenId.Settings,   root = settings },
                new UIManager.ScreenBinding { id = ScreenId.Scores,     root = scoresScreen },
                new UIManager.ScreenBinding { id = ScreenId.Pause,      root = pauseScreen },
                new UIManager.ScreenBinding { id = ScreenId.GameOver,   root = gameOver }
            };

            return new UIBuilt { hud = hud, hudGroup = hudGroup, flashImage = flashImg };
        }

        // ---------- Helpers UI ----------
        static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static GameObject MakeBackgroundFromMaquette(Transform parent, Sprite sprite, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = false; // stretch to fill
            img.raycastTarget = false;
            return go;
        }

        // Bouton transparent superposé à un élément graphique de la maquette.
        // Coordonnées en RATIO (0-1) du canvas 1920x1080.
        static Button MakeOverlayButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite hoverSprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.001f); // quasi-invisible mais raycast OK
            img.sprite = hoverSprite;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // teinte sur hover
            var cb = btn.colors;
            cb.normalColor      = new Color(1, 1, 1, 0.001f);
            cb.highlightedColor = new Color(1, 1, 1, 0.18f);
            cb.pressedColor     = new Color(1, 1, 1, 0.30f);
            cb.selectedColor    = new Color(1, 1, 1, 0.05f);
            btn.colors = cb;
            go.AddComponent<CartoonButton>();
            return btn;
        }

        // ---------- HUD ----------
        static void BuildHUD(Transform parent, HUDController hud, GeneratedSprites sprites)
        {
            // Chrono central en haut (énorme)
            var timer = MakeText(parent, "TimerText", "12",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120),
                new Vector2(360, 200), 150, TextAlignmentOptions.Center, VioletDark);
            timer.fontStyle = FontStyles.Bold;

            // Score haut gauche (gros, jaune avec contour foncé pour rester lisible sur tous fonds)
            var score = MakeText(parent, "ScoreText", "0",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230, -110),
                new Vector2(360, 120), 90, TextAlignmentOptions.Left, YellowAccent);
            score.fontStyle = FontStyles.Bold;
            score.outlineColor = VioletDark;
            score.outlineWidth = 0.15f;

            // Cœurs haut droite (3 par défaut)
            var heartsRoot = new GameObject("Hearts", typeof(RectTransform));
            heartsRoot.transform.SetParent(parent, false);
            var hRT = heartsRoot.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(1f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
            hRT.pivot = new Vector2(1f, 1f);
            hRT.anchoredPosition = new Vector2(-60, -60);
            hRT.sizeDelta = new Vector2(360, 100);
            var layout = heartsRoot.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 12; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            var hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var hgo = new GameObject($"Heart_{i}", typeof(RectTransform));
                hgo.transform.SetParent(heartsRoot.transform, false);
                var hRt = hgo.GetComponent<RectTransform>();
                hRt.sizeDelta = new Vector2(80, 80);
                var img = hgo.AddComponent<Image>();
                img.sprite = sprites.heart;
                img.color = WhiteUI;
                hearts[i] = img;
            }

            // Combo center-low
            var combo = MakeText(parent, "ComboText", "",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 220),
                new Vector2(700, 70), 44, TextAlignmentOptions.Center, YellowAccent);
            combo.fontStyle = FontStyles.Bold;
            combo.outlineColor = VioletDark; combo.outlineWidth = 0.18f;

            // Palier bas gauche (anchor bottom-left + pivot top-left adjusted via anchoredPosition)
            var palier = MakeText(parent, "PalierText", "Palier 1 — Échauffement",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(420, 90),
                new Vector2(700, 50), 28, TextAlignmentOptions.Left, WhiteUI);
            palier.outlineColor = VioletDark; palier.outlineWidth = 0.15f;

            // Score popup centre haut (apparait sur bonne réponse)
            var popupGO = new GameObject("ScorePopup", typeof(RectTransform), typeof(CanvasGroup));
            popupGO.transform.SetParent(parent, false);
            var pRT = popupGO.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(0, 80);
            pRT.sizeDelta = new Vector2(400, 200);
            var pGroup = popupGO.GetComponent<CanvasGroup>();
            pGroup.alpha = 0;
            var popupTxt = popupGO.AddComponent<TextMeshProUGUI>();
            popupTxt.text = "+1";
            popupTxt.fontSize = 110;
            popupTxt.fontStyle = FontStyles.Bold;
            popupTxt.alignment = TextAlignmentOptions.Center;
            popupTxt.color = YellowAccent;
            popupTxt.outlineColor = VioletDark;
            popupTxt.outlineWidth = 0.2f;

            // Crosshair simple
            var crossGO = new GameObject("Crosshair", typeof(RectTransform));
            crossGO.transform.SetParent(parent, false);
            var crt = crossGO.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(10, 10);
            var crossImg = crossGO.AddComponent<Image>();
            crossImg.sprite = sprites.circle;
            crossImg.color = new Color(1, 1, 1, 0.7f);
            crossImg.raycastTarget = false;

            hud.scoreText = score;
            hud.timerText = timer;
            hud.comboText = combo;
            hud.palierText = palier;
            hud.hearts = hearts;
            hud.heartFull = sprites.heart;
            hud.heartEmpty = sprites.heart; // mêmê sprite, alpha géré par script
            hud.scorePopup = popupTxt;
            hud.scorePopupGroup = pGroup;
        }

        static void BuildSubtitles(Transform parent, GeneratedSprites sprites)
        {
            var go = new GameObject("Subtitles", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(1200, 100);

            var bg = go.AddComponent<Image>();
            bg.sprite = sprites.roundedSquare;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.65f);
            bg.raycastTarget = false;

            var txt = MakeText(go.transform, "Label", "",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
                Vector2.zero, 36, TextAlignmentOptions.Center, Color.white);
            var rt2 = txt.GetComponent<RectTransform>();
            rt2.anchorMin = Vector2.zero; rt2.anchorMax = Vector2.one;
            rt2.offsetMin = new Vector2(40, 10); rt2.offsetMax = new Vector2(-40, -10);

            var sd = go.AddComponent<SubtitleDisplay>();
            sd.label = txt;
            sd.group = go.GetComponent<CanvasGroup>();
            sd.group.alpha = 0;
        }

        // ---------- Screen builders ----------
        // Coordonnées : ratios 0..1 sur le canvas 1920x1080.
        // Estimées à l'œil depuis les maquettes — ajustables ensuite dans l'inspecteur.

        static GameObject BuildMainMenu(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_MainMenu", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);
            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            // JOUER (gros bouton violet, centre)
            var play = MakeOverlayButton(screen.transform, "PlayButton",
                new Vector2(0.27f, 0.30f), new Vector2(0.73f, 0.53f));
            // SCORES (bouton blanc, gauche)
            var scores = MakeOverlayButton(screen.transform, "ScoresButton",
                new Vector2(0.33f, 0.16f), new Vector2(0.53f, 0.27f));
            // PARAMÈTRES (bouton blanc, droite)
            var settings = MakeOverlayButton(screen.transform, "SettingsButton",
                new Vector2(0.54f, 0.16f), new Vector2(0.74f, 0.27f));
            // CRÉDITS (bas centre)
            var credits = MakeOverlayButton(screen.transform, "CreditsButton",
                new Vector2(0.43f, 0.02f), new Vector2(0.57f, 0.07f));

            var s = screen.AddComponent<MainMenuScreen>();
            s.playButton = play; s.scoresButton = scores;
            s.settingsButton = settings; s.creditsButton = credits;

            return screen;
        }

        static GameObject BuildModeSelect(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_ModeSelect", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);
            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            // 5 boutons modes (verticalement empilés sur la maquette)
            var classique = MakeOverlayButton(screen.transform, "ClassiqueBtn",
                new Vector2(0.32f, 0.62f), new Vector2(0.68f, 0.71f));
            var detente = MakeOverlayButton(screen.transform, "DetenteBtn",
                new Vector2(0.32f, 0.51f), new Vector2(0.68f, 0.60f));
            var speedrun = MakeOverlayButton(screen.transform, "SpeedrunBtn",
                new Vector2(0.32f, 0.40f), new Vector2(0.68f, 0.49f));
            var infini = MakeOverlayButton(screen.transform, "InfiniBtn",
                new Vector2(0.32f, 0.29f), new Vector2(0.68f, 0.38f));
            var survie = MakeOverlayButton(screen.transform, "SurvieBtn",
                new Vector2(0.32f, 0.18f), new Vector2(0.68f, 0.27f));

            // Bouton retour invisible (haut gauche par défaut, ESC fonctionnera aussi)
            var back = MakeOverlayButton(screen.transform, "BackButton",
                new Vector2(0.02f, 0.92f), new Vector2(0.10f, 0.98f));

            var s = screen.AddComponent<ModeSelectScreen>();
            s.modeButtons = new[]
            {
                new ModeSelectScreen.ModeButton { mode = GameMode.Classique, button = classique },
                new ModeSelectScreen.ModeButton { mode = GameMode.Detente,   button = detente },
                new ModeSelectScreen.ModeButton { mode = GameMode.Speedrun,  button = speedrun },
                new ModeSelectScreen.ModeButton { mode = GameMode.Infini,    button = infini },
                new ModeSelectScreen.ModeButton { mode = GameMode.Survie,    button = survie },
            };
            s.backButton = back;

            return screen;
        }

        static GameObject BuildSettings(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_Settings", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);
            // Fond classroom (maquette M_3) — sert juste de décor
            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            // Carte procédurale qui remplace entièrement la card statique de la maquette
            MakeCard(screen.transform, "Card",
                new Vector2(0.180f, 0.030f), new Vector2(0.820f, 0.965f), sprites);

            // Titre PARAMÈTRES
            MakeText(screen.transform, "Title", "PARAMÈTRES",
                new Vector2(0.205f, 0.860f), new Vector2(0.795f, 0.935f), Vector2.zero, Vector2.zero,
                56, TextAlignmentOptions.Center, VioletDark).fontStyle = FontStyles.Bold;

            // 8 lignes de réglages, échelonnées de y=0.78 (haut) à y=0.18 (bas)
            // Chaque ligne fait 0.075 de haut, espace de 0.005
            // Indices : 0=TTS, 1=SOUS-TITRES, 2=DALTONIEN, 3=OPENDYSLEXIC, 4=MUSIQUE, 5=EFFETS, 6=SENS, 7=DETENTE
            float[] rowCenters = { 0.770f, 0.690f, 0.610f, 0.530f, 0.450f, 0.370f, 0.290f, 0.210f };
            float rowH = 0.030f;

            // Helpers pour positionner labels et contrôles
            float labelXMin = 0.235f, labelXMax = 0.560f;
            float ctrlXMin  = 0.580f, ctrlXMax  = 0.770f;

            // ----- Toggles -----
            var ttsToggle        = AddSettingToggleRow(screen.transform, "TTS",        "SYNTHÈSE VOCALE (TTS)",       "Lit les calculs et événements à voix haute",        rowCenters[0], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);
            var subtitlesToggle  = AddSettingToggleRow(screen.transform, "Subtitles",  "SOUS-TITRES",                 "Affiche un texte pour chaque événement sonore",     rowCenters[1], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);
            var colorblindToggle = AddSettingToggleRow(screen.transform, "Colorblind", "MODE DALTONIEN",              "Ajoute des formes distinctes sur les dalles",       rowCenters[2], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);
            var dyslexicToggle   = AddSettingToggleRow(screen.transform, "Dyslexic",   "POLICE OPENDYSLEXIC",         "Remplace la police dans tout le jeu",               rowCenters[3], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);

            // ----- Sliders -----
            var musicSlider = AddSettingSliderRow(screen.transform, "Music", "VOLUME MUSIQUE", null,                              rowCenters[4], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);
            var sfxSlider   = AddSettingSliderRow(screen.transform, "Sfx",   "VOLUME EFFETS", null,                               rowCenters[5], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);
            var sensSlider  = AddSettingSliderRow(screen.transform, "Sens",  "SENSIBILITÉ CAMÉRA", null,                          rowCenters[6], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);

            // ----- Mode détente (toggle final) -----
            var relaxedToggle = AddSettingToggleRow(screen.transform, "Relaxed", "MODE DÉTENTE",                "Chrono ×1.5 — score enregistré séparément",         rowCenters[7], rowH, labelXMin, labelXMax, ctrlXMin, ctrlXMax, sprites);

            // RETOUR (bouton jaune cartoon)
            var back = MakeYellowButton(screen.transform, "BackButton", "← RETOUR",
                new Vector2(0.42f, 0.105f), new Vector2(0.58f, 0.165f), sprites);

            var s = screen.AddComponent<SettingsScreen>();
            s.ttsToggle = ttsToggle; s.subtitlesToggle = subtitlesToggle;
            s.colorblindToggle = colorblindToggle; s.dyslexicToggle = dyslexicToggle;
            s.relaxedToggle = relaxedToggle;
            s.musicSlider = musicSlider; s.sfxSlider = sfxSlider; s.sensitivitySlider = sensSlider;
            s.backButton = back;

            return screen;
        }

        // Construit une ligne complète : titre + description + toggle pilule à droite.
        static Toggle AddSettingToggleRow(Transform parent, string id, string title, string desc,
            float yCenter, float rowH, float labelXMin, float labelXMax, float ctrlXMin, float ctrlXMax,
            GeneratedSprites sprites)
        {
            // Titre
            var t = MakeText(parent, $"{id}_Title", title,
                new Vector2(labelXMin, yCenter + rowH * 0.1f), new Vector2(labelXMax, yCenter + rowH * 1.0f),
                Vector2.zero, Vector2.zero, 24, TextAlignmentOptions.Left, VioletDark);
            t.fontStyle = FontStyles.Bold;
            // Description
            if (!string.IsNullOrEmpty(desc))
            {
                var d = MakeText(parent, $"{id}_Desc", desc,
                    new Vector2(labelXMin, yCenter - rowH * 0.9f), new Vector2(labelXMax, yCenter + rowH * 0.0f),
                    Vector2.zero, Vector2.zero, 16, TextAlignmentOptions.Left, new Color(0.45f, 0.40f, 0.55f));
            }
            // Toggle (pilule)
            var toggle = MakeVisibleToggle(parent, $"{id}_Toggle",
                new Vector2(ctrlXMin, yCenter - rowH * 0.45f), new Vector2(ctrlXMin + 0.06f, yCenter + rowH * 0.45f),
                sprites);
            return toggle;
        }

        static Slider AddSettingSliderRow(Transform parent, string id, string title, string desc,
            float yCenter, float rowH, float labelXMin, float labelXMax, float ctrlXMin, float ctrlXMax,
            GeneratedSprites sprites)
        {
            var t = MakeText(parent, $"{id}_Title", title,
                new Vector2(labelXMin, yCenter - rowH * 0.5f), new Vector2(labelXMax, yCenter + rowH * 0.5f),
                Vector2.zero, Vector2.zero, 24, TextAlignmentOptions.Left, VioletDark);
            t.fontStyle = FontStyles.Bold;
            var slider = MakeVisibleSlider(parent, $"{id}_Slider",
                new Vector2(ctrlXMin, yCenter - rowH * 0.45f), new Vector2(ctrlXMax, yCenter + rowH * 0.45f),
                sprites);
            return slider;
        }

        // Bouton jaune cartoon (style RETOUR de la maquette).
        static Button MakeYellowButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, GeneratedSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = sprites.roundedSquare;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = YellowAccent;

            var lbl = MakeText(go.transform, "Label", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                32, TextAlignmentOptions.Center, VioletDark);
            lbl.fontStyle = FontStyles.Bold;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            var cb = btn.colors;
            cb.normalColor = YellowAccent;
            cb.highlightedColor = Color.Lerp(YellowAccent, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(YellowAccent, Color.black, 0.2f);
            cb.selectedColor = YellowAccent;
            btn.colors = cb;
            go.AddComponent<CartoonButton>();
            return btn;
        }

        // Pilule visible (style maquette) qui montre le vrai état OUI/NON.
        // Elle masque la pilule statique de la maquette sous-jacente.
        static Toggle MakeVisibleToggle(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, GeneratedSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Background pilule (gris clair quand OFF, violet quand ON)
            var bg = go.AddComponent<Image>();
            bg.sprite = sprites.roundedSquare;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0xE0, 0xE0, 0xE5, 0xFF);

            // Knob blanc (rond, plus petit que la pilule)
            var knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(go.transform, false);
            var kRT = knob.GetComponent<RectTransform>();
            kRT.anchorMin = new Vector2(0, 0); kRT.anchorMax = new Vector2(0, 1);
            kRT.pivot = new Vector2(0, 0.5f);
            kRT.anchoredPosition = new Vector2(4, 0);
            kRT.sizeDelta = new Vector2(28, -8);
            var kImg = knob.AddComponent<Image>();
            kImg.sprite = sprites.circle;
            kImg.color = Color.white;

            // Label OUI/NON petit, à droite (debord léger sur fond cream du masque)
            var label = new GameObject("StateLabel", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            var lRT = label.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(1f, 0f); lRT.anchorMax = new Vector2(1f, 1f);
            lRT.pivot = new Vector2(0f, 0.5f);
            lRT.anchoredPosition = new Vector2(8, 0);
            lRT.sizeDelta = new Vector2(70, 0);
            var ltxt = label.AddComponent<TextMeshProUGUI>();
            ltxt.text = "NON"; ltxt.fontSize = 22;
            ltxt.color = new Color(0.5f, 0.5f, 0.55f); ltxt.fontStyle = FontStyles.Bold;
            ltxt.alignment = TextAlignmentOptions.MidlineLeft;
            ltxt.raycastTarget = false;

            var t = go.AddComponent<Toggle>();
            t.targetGraphic = bg;
            t.graphic = kImg;
            t.transition = Selectable.Transition.None;
            var follower = go.AddComponent<ToggleVisualFollower>();
            follower.toggle = t;
            follower.background = bg;
            follower.knob = kRT;
            follower.stateLabel = ltxt;
            follower.colorOff = new Color32(0xE0, 0xE0, 0xE5, 0xFF);
            follower.colorOn = VioletPrimary;

            return t;
        }

        // Slider visible : rail clair + remplissage violet + handle blanc.
        // Couvre le rail de la maquette.
        static Slider MakeVisibleSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, GeneratedSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Rail (background)
            var bg = go.AddComponent<Image>();
            bg.sprite = sprites.roundedSquare;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0xE0, 0xE0, 0xE5, 0xFF);

            // Fill area + Fill (rail interne, sans padding pour rester fin)
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = new Vector2(4, 4); faRT.offsetMax = new Vector2(-4, -4);
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0.5f, 1f);
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.sprite = sprites.roundedSquare;
            fImg.type = Image.Type.Sliced;
            fImg.color = VioletPrimary;

            // Handle area + handle (cercle blanc plus petit)
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(12, 0); haRT.offsetMax = new Vector2(-12, 0);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var hRT = handle.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(24, 24);
            var hImg = handle.AddComponent<Image>();
            hImg.sprite = sprites.circle;
            hImg.color = Color.white;

            var s = go.AddComponent<Slider>();
            s.targetGraphic = hImg;
            s.fillRect = fRT;
            s.handleRect = hRT;
            s.minValue = 0; s.maxValue = 1; s.value = 0.5f;
            s.transition = Selectable.Transition.None;
            return s;
        }

        static GameObject BuildPause(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_Pause", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);

            // Vignette sombre derrière
            var vignette = new GameObject("Vignette", typeof(RectTransform));
            vignette.transform.SetParent(screen.transform, false);
            StretchFull(vignette);
            var vImg = vignette.AddComponent<Image>();
            vImg.color = new Color(0, 0, 0, 0.45f);

            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            var resume   = MakeOverlayButton(screen.transform, "ResumeButton",   new Vector2(0.36f, 0.55f), new Vector2(0.64f, 0.65f));
            var settings = MakeOverlayButton(screen.transform, "SettingsButton", new Vector2(0.36f, 0.45f), new Vector2(0.64f, 0.54f));
            var menu     = MakeOverlayButton(screen.transform, "MainMenuButton", new Vector2(0.36f, 0.34f), new Vector2(0.64f, 0.43f));
            var quit     = MakeOverlayButton(screen.transform, "QuitButton",     new Vector2(0.36f, 0.23f), new Vector2(0.64f, 0.32f));

            var s = screen.AddComponent<PauseScreen>();
            s.resumeButton = resume; s.settingsButton = settings;
            s.mainMenuButton = menu; s.quitButton = quit;

            return screen;
        }

        static GameObject BuildGameOver(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_GameOver", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);
            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            // Carte procédurale (blanc + contour violet) qui remplace la card statique de la maquette
            MakeCard(screen.transform, "Card",
                new Vector2(0.318f, 0.080f), new Vector2(0.682f, 0.92f), sprites);

            // Titre dynamique : "PARTIE TERMINÉE" ou "VICTOIRE !" selon GameManager.lastWasVictory
            var title = MakeText(screen.transform, "Title", "PARTIE TERMINÉE",
                new Vector2(0.318f, 0.825f), new Vector2(0.682f, 0.890f), Vector2.zero, Vector2.zero,
                52, TextAlignmentOptions.Center, VioletDark);
            title.fontStyle = FontStyles.Bold;

            // Trophée (utilise la portion image trophée de la maquette via crop placement)
            MakeTrophyArt(screen.transform, sprites,
                new Vector2(0.345f, 0.560f), new Vector2(0.655f, 0.800f));

            // Label "SCORE" (au-dessus du nombre, bien séparé)
            var scoreLabel = MakeText(screen.transform, "ScoreLabel", "SCORE",
                new Vector2(0.318f, 0.520f), new Vector2(0.682f, 0.560f), Vector2.zero,
                Vector2.zero, 28, TextAlignmentOptions.Center, new Color(0.45f, 0.40f, 0.55f));
            scoreLabel.fontStyle = FontStyles.Bold;

            // Texte score (gros nombre sous le label, bien espacé)
            var score = MakeText(screen.transform, "ScoreText", "0",
                new Vector2(0.318f, 0.395f), new Vector2(0.682f, 0.515f), Vector2.zero,
                Vector2.zero, 90, TextAlignmentOptions.Center, VioletDark);
            score.fontStyle = FontStyles.Bold;

            // Texte record
            var record = MakeText(screen.transform, "RecordText", "RECORD PERSONNEL : 0",
                new Vector2(0.318f, 0.330f), new Vector2(0.682f, 0.380f), Vector2.zero,
                Vector2.zero, 26, TextAlignmentOptions.Center, new Color(0.45f, 0.40f, 0.55f));
            record.fontStyle = FontStyles.Bold;

            // Séparateur
            MakeFlatMaskColor(screen.transform, "Separator",
                new Vector2(0.345f, 0.315f), new Vector2(0.655f, 0.319f),
                new Color32(0xE5, 0xE0, 0xD5, 0xFF));

            // Boutons cartoon procéduraux (violet primaire + secondaire blanc)
            var replay = MakeCartoonButton(screen.transform, "ReplayButton", "REJOUER",
                new Vector2(0.345f, 0.215f), new Vector2(0.655f, 0.305f), true, sprites);
            var menu   = MakeCartoonButton(screen.transform, "MainMenuButton", "MENU PRINCIPAL",
                new Vector2(0.345f, 0.115f), new Vector2(0.655f, 0.205f), false, sprites);

            // Confettis légers
            var confettiGO = new GameObject("Confetti");
            confettiGO.transform.SetParent(screen.transform, false);
            var ps = confettiGO.AddComponent<ParticleSystem>();
            ConfigureConfettiOverlay(ps);

            var s = screen.AddComponent<GameOverScreen>();
            s.titleText = title;
            s.scoreText = score; s.recordText = record;
            s.replayButton = replay; s.mainMenuButton = menu;
            s.confetti = ps;

            return screen;
        }

        static void ConfigureConfettiOverlay(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 1.5f; main.loop = false;
            main.startLifetime = 2.5f; main.startSpeed = 6f;
            main.startSize = 0.2f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.startColor = new ParticleSystem.MinMaxGradient(BuildPartyGradient());

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 100) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f; shape.radius = 0.2f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        static GameObject BuildScores(Transform parent, Sprite bgSprite, GeneratedSprites sprites)
        {
            var screen = new GameObject("Screen_Scores", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            StretchFull(screen);
            MakeBackgroundFromMaquette(screen.transform, bgSprite, "BG");

            // Carte procédurale qui remplace la card statique
            MakeCard(screen.transform, "Card",
                new Vector2(0.205f, 0.090f), new Vector2(0.795f, 0.92f), sprites);

            // Titre "SCORES"
            MakeText(screen.transform, "Title", "SCORES",
                new Vector2(0.205f, 0.825f), new Vector2(0.795f, 0.890f), Vector2.zero, Vector2.zero,
                64, TextAlignmentOptions.Center, VioletDark).fontStyle = FontStyles.Bold;

            // Bandeau header violet (rang/joueur/score/mode)
            var headerBar = MakeFlatMaskColor(screen.transform, "HeaderBar",
                new Vector2(0.225f, 0.700f), new Vector2(0.775f, 0.760f), VioletPrimary);
            MakeText(headerBar.transform, "HdrRang", "RANG",
                new Vector2(0.05f, 0f), new Vector2(0.20f, 1f), Vector2.zero, Vector2.zero,
                26, TextAlignmentOptions.Center, Color.white).fontStyle = FontStyles.Bold;
            MakeText(headerBar.transform, "HdrJoueur", "JOUEUR",
                new Vector2(0.22f, 0f), new Vector2(0.50f, 1f), Vector2.zero, Vector2.zero,
                26, TextAlignmentOptions.Left, Color.white).fontStyle = FontStyles.Bold;
            MakeText(headerBar.transform, "HdrScore", "SCORE",
                new Vector2(0.50f, 0f), new Vector2(0.75f, 1f), Vector2.zero, Vector2.zero,
                26, TextAlignmentOptions.Right, Color.white).fontStyle = FontStyles.Bold;
            MakeText(headerBar.transform, "HdrMode", "MODE",
                new Vector2(0.78f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero,
                26, TextAlignmentOptions.Left, Color.white).fontStyle = FontStyles.Bold;

            // Bandeau des stats en bas (pastille violet clair)
            MakeFlatMaskColor(screen.transform, "StatsBg",
                new Vector2(0.225f, 0.140f), new Vector2(0.775f, 0.260f),
                new Color(VioletPrimary.r, VioletPrimary.g, VioletPrimary.b, 0.10f));

            // Étiquettes des stats
            MakeText(screen.transform, "BestLbl", "TON MEILLEUR",
                new Vector2(0.225f, 0.215f), new Vector2(0.42f, 0.255f), Vector2.zero, Vector2.zero,
                22, TextAlignmentOptions.Center, VioletDark).fontStyle = FontStyles.Bold;
            MakeText(screen.transform, "GamesLbl", "PARTIES JOUÉES",
                new Vector2(0.42f, 0.215f), new Vector2(0.58f, 0.255f), Vector2.zero, Vector2.zero,
                22, TextAlignmentOptions.Center, VioletDark).fontStyle = FontStyles.Bold;
            MakeText(screen.transform, "RateLbl", "TAUX DE RÉUSSITE",
                new Vector2(0.58f, 0.215f), new Vector2(0.775f, 0.255f), Vector2.zero, Vector2.zero,
                22, TextAlignmentOptions.Center, VioletDark).fontStyle = FontStyles.Bold;

            var rows = new ScoresScreen.Row[5];
            float[] yTops = { 0.690f, 0.620f, 0.550f, 0.480f, 0.410f };
            float rowH = 0.060f;
            for (int i = 0; i < 5; i++)
            {
                float yTop = yTops[i];
                rows[i] = new ScoresScreen.Row
                {
                    rank   = MakeRowText(screen.transform, $"R{i}_rank",   yTop - rowH, yTop, 0.252f, 0.335f, TextAlignmentOptions.Center, 30),
                    player = MakeRowText(screen.transform, $"R{i}_player", yTop - rowH, yTop, 0.346f, 0.500f, TextAlignmentOptions.Left,   30),
                    score  = MakeRowText(screen.transform, $"R{i}_score",  yTop - rowH, yTop, 0.500f, 0.638f, TextAlignmentOptions.Right,  30),
                    mode   = MakeRowText(screen.transform, $"R{i}_mode",   yTop - rowH, yTop, 0.654f, 0.748f, TextAlignmentOptions.Left,   26),
                };
            }

            var best   = MakeRowText(screen.transform, "BestText",         0.155f, 0.215f, 0.225f, 0.42f, TextAlignmentOptions.Center, 50);
            var games  = MakeRowText(screen.transform, "GamesPlayedText",  0.155f, 0.215f, 0.42f, 0.58f,  TextAlignmentOptions.Center, 50);
            var rate   = MakeRowText(screen.transform, "SuccessRateText",  0.155f, 0.215f, 0.58f, 0.775f, TextAlignmentOptions.Center, 50);

            var back = MakeCartoonButton(screen.transform, "BackButton", "← RETOUR",
                new Vector2(0.42f, 0.040f), new Vector2(0.58f, 0.090f), false, sprites);

            var s = screen.AddComponent<ScoresScreen>();
            s.rows = rows;
            s.bestText = best; s.gamesPlayedText = games; s.successRateText = rate;
            s.backButton = back;

            return screen;
        }

        static TMP_Text MakeRowText(Transform parent, string name, float yMin, float yMax, float xMin, float xMax,
                                     TextAlignmentOptions align, int size = 28)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = "";
            t.fontSize = size;
            t.color = VioletDark;
            t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            return t;
        }

        // Panneau crème arrondi pour masquer une zone (rounded sprite).
        static GameObject MakeCreamMask(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, GeneratedSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = sprites.roundedSquare;
            img.type = Image.Type.Sliced;
            img.color = new Color32(0xFA, 0xF4, 0xDE, 0xFF);
            img.raycastTarget = false;
            return go;
        }

        // Rectangle plat pour masquer (utile à l'intérieur d'un panel rounded).
        static GameObject MakeFlatMask(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            return MakeFlatMaskColor(parent, name, anchorMin, anchorMax, new Color32(0xFA, 0xF4, 0xDE, 0xFF));
        }

        // Rectangle plat de couleur arbitraire.
        static GameObject MakeFlatMaskColor(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        // Carte cartoon : fond blanc rounded + contour violet, style maquette.
        // Renvoie le GameObject du contenu interne (où placer les éléments enfants).
        static GameObject MakeCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, GeneratedSprites sprites)
        {
            // Fond blanc rounded
            var bg = new GameObject(name, typeof(RectTransform));
            bg.transform.SetParent(parent, false);
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = sprites.roundedSquare;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color32(0xFA, 0xF4, 0xDE, 0xFF);
            bgImg.raycastTarget = false;

            // Contour violet par-dessus
            var border = new GameObject("Border", typeof(RectTransform));
            border.transform.SetParent(bg.transform, false);
            var brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var borderImg = border.AddComponent<Image>();
            borderImg.sprite = sprites.roundedSquareBorder;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = VioletPrimary;
            borderImg.raycastTarget = false;

            return bg;
        }

        // Bouton cartoon procédural fidèle aux maquettes.
        // primary = true → fond violet, texte blanc
        // primary = false → fond blanc, contour violet, texte violet foncé
        static Button MakeCartoonButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, bool primary, GeneratedSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Background
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = sprites.roundedSquare;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = primary ? VioletPrimary : Color.white;

            // Border (uniquement pour les boutons secondaires — le primaire utilise sa couleur pleine)
            if (!primary)
            {
                var border = new GameObject("Border", typeof(RectTransform));
                border.transform.SetParent(go.transform, false);
                var brt = border.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                var borderImg = border.AddComponent<Image>();
                borderImg.sprite = sprites.roundedSquareBorder;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = VioletPrimary;
                borderImg.raycastTarget = false;
            }

            // Label
            var lbl = MakeText(go.transform, "Label", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                40, TextAlignmentOptions.Center, primary ? Color.white : VioletDark);
            lbl.fontStyle = FontStyles.Bold;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            // Button + animation cartoon
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            var cb = btn.colors;
            var hover = primary ? Color.Lerp(VioletPrimary, Color.white, 0.15f) : new Color(0.95f, 0.95f, 0.97f);
            var press = primary ? Color.Lerp(VioletPrimary, Color.black, 0.20f) : new Color(0.85f, 0.85f, 0.88f);
            cb.normalColor = bgImg.color;
            cb.highlightedColor = hover;
            cb.pressedColor = press;
            cb.selectedColor = bgImg.color;
            btn.colors = cb;
            go.AddComponent<CartoonButton>();
            return btn;
        }

        // Image décorative trophée — utilise le sprite du M_6 cropped via UV (impossible facilement),
        // donc on crée un placeholder coloré avec un emoji-trophée. Pour vraie image, drag M_6 cropped.
        static GameObject MakeTrophyArt(Transform parent, GeneratedSprites sprites, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Trophy", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            // Fond ciel arrondi
            var bg = go.AddComponent<Image>();
            bg.sprite = sprites.roundedSquare;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x8E, 0xD2, 0xFF, 0xFF);
            bg.raycastTarget = false;
            // Cercle jaune trophée (placeholder)
            var cup = new GameObject("Cup", typeof(RectTransform));
            cup.transform.SetParent(go.transform, false);
            var cRT = cup.GetComponent<RectTransform>();
            cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.sizeDelta = new Vector2(140, 140);
            var cImg = cup.AddComponent<Image>();
            cImg.sprite = sprites.circle;
            cImg.color = YellowAccent;
            cImg.raycastTarget = false;
            // Texte "1" au centre (placeholder, première place)
            var t = MakeText(go.transform, "Number", "1",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                90, TextAlignmentOptions.Center, VioletDark);
            t.fontStyle = FontStyles.Bold;
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            return go;
        }

        // ---------- Generic helpers ----------
        static TMP_Text MakeText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, int fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fontSize; t.color = color;
            t.alignment = align; t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        // ====================================================================
        // Outils éditeur
        // ====================================================================

        static Material MakeMat(Color c, string name)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            return m;
        }

        static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r) r.sharedMaterial = MakeMat(c, go.name + "_Mat");
        }

        static void EnsureTMPEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            string[] candidates =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
            };
            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    AssetDatabase.ImportPackage(p, false);
                    AssetDatabase.Refresh();
                    return;
                }
            }
            Debug.LogWarning("TMP Essentials not found. Text may render poorly.");
        }

        static void EnsurePlayerTag()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == "Player") return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Player";
            so.ApplyModifiedProperties();
        }

        // Importe Bowlby (TTF) et l'applique comme police globale via TMP_FontAsset SDF.
        // Si déjà présent, le réutilise.
        static void ApplyCartoonFont(MathsClass.AccessibilityManager access)
        {
            string ttfPath = "Assets/Fonts/Bowlby.ttf";
            string assetPath = "Assets/Fonts/Bowlby SDF.asset";
            if (!File.Exists(ttfPath))
            {
                Debug.LogWarning("Bowlby.ttf manquant — police cartoon non appliquée.");
                return;
            }
            AssetDatabase.ImportAsset(ttfPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (!font) return;
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (!fontAsset)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(font);
                fontAsset.name = "Bowlby SDF";
                AssetDatabase.CreateAsset(fontAsset, assetPath);
            }
            // Fallback Fredoka si présent
            string fbTtf = "Assets/Fonts/Fredoka.ttf";
            if (File.Exists(fbTtf))
            {
                AssetDatabase.ImportAsset(fbTtf);
                var fbFont = AssetDatabase.LoadAssetAtPath<Font>(fbTtf);
                string fbPath = "Assets/Fonts/Fredoka SDF.asset";
                var fbAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fbPath);
                if (!fbAsset && fbFont)
                {
                    fbAsset = TMP_FontAsset.CreateFontAsset(fbFont);
                    fbAsset.name = "Fredoka SDF";
                    AssetDatabase.CreateAsset(fbAsset, fbPath);
                }
                if (fbAsset)
                {
                    if (fontAsset.fallbackFontAssetTable == null)
                        fontAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    if (!fontAsset.fallbackFontAssetTable.Contains(fbAsset))
                        fontAsset.fallbackFontAssetTable.Add(fbAsset);
                }
            }
            // Applique à tous les TMP_Text de la scène
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                t.font = fontAsset;
            // Et expose comme defaultFont sur l'AccessibilityManager
            if (access != null) access.defaultFont = fontAsset;
            AssetDatabase.SaveAssets();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))           AssetDatabase.CreateFolder("Assets", "UI");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Generated")) AssetDatabase.CreateFolder("Assets/UI", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Maquettes"))    AssetDatabase.CreateFolder("Assets", "Maquettes");
        }
    }
}
