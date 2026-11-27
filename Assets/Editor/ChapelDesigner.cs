using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tools -> Apply Chapel Design: застосовує дизайн до відкритої сцени (лаба 1 або лаба 2) і зберігає функціонал.
public static class ChapelDesigner
{
    const string Dir = "Assets/Design/";
    const string MatDir = Dir + "Materials/";
    const string TexDir = Dir + "Textures/";

    static Material steel, slotGlow, threadM, ghostM, stone, woodWall, woodDark, plaster, cloth, hoodDark, bordeaux, brass, wax, flame, glass,
                    discDark, frameDark, woodBlack;

    static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

    // ---------- текстури ----------
    static float Per(float u, float v, int seed)
    {
        float a = 0f;
        for (int i = 1; i <= 4; i++)
        {
            a += Mathf.Sin((u * i * 3 + v * i * 2) * Mathf.PI * 2f + seed * i) * 0.5f / i;
            a += Mathf.Cos((v * i * 3 + u * i) * Mathf.PI * 2f + seed) * 0.5f / i;
        }
        return a * 0.5f + 0.5f;
    }

    static Texture2D MakeTex(string name, int size, System.Func<float, float, int, int, Color> f)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = f((float)x / size, (float)y / size, x, y);
        t.SetPixels(px); t.Apply();
        string path = TexDir + name + ".png";
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material M(string name, Color c, Texture2D tex = null, float tx = 1, float ty = 1,
                      float smooth = 0.15f, float metal = 0f, Color? emis = null)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = m == null;
        if (isNew) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", new Vector2(tx, ty)); }
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (!emis.HasValue) { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
        if (emis.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis.Value); }
        if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    static void MakeMaterials()
    {
        var stoneTex = MakeTex("StoneSlab", 512, (u, v, x, y) =>
        {
            float n = Per(u, v, 1) * 0.6f + Random.value * 0.12f;
            Color c = Color.Lerp(C("#27231f"), C("#4a443c"), n);
            int e = 5; int s = 512;
            if (x < e || y < e || x > s - e || y > s - e) c *= 0.3f;
            return c;
        });
        var woodTex = MakeTex("WoodPlanks", 512, (u, v, x, y) =>
        {
            int plank = Mathf.FloorToInt(u * 8f);
            float tone = Mathf.Abs(Mathf.Sin(plank * 12.9898f) * 43758.5453f) % 1f;
            float grain = Mathf.Sin(v * 60f + Per(u, v, plank + 3) * 9f + plank) * 0.5f + 0.5f;
            Color c = Color.Lerp(C("#1d1511"), C("#3d2e26"), tone * 0.6f + grain * 0.4f);
            float edge = Mathf.Abs(u * 8f - Mathf.Round(u * 8f));
            if (edge < 0.04f) c *= 0.35f;
            return c;
        });
        var plasterTex = MakeTex("Plaster", 256, (u, v, x, y) =>
            Color.Lerp(C("#2a2927"), C("#4b4946"), Per(u, v, 5) * 0.8f + Random.value * 0.1f));
        var clothTex = MakeTex("Cloth", 256, (u, v, x, y) =>
        {
            float fold = Mathf.Sin((u * 5f + Per(u, v, 2) * 0.8f) * Mathf.PI * 2f) * 0.5f + 0.5f;
            return Color.Lerp(C("#b8b0a2"), C("#e6dfd2"), fold * 0.7f + Per(u, v, 8) * 0.3f);
        });

        stone = M("Stone", Color.white, stoneTex, 5, 9, 0.35f);
        woodWall = M("WoodWall", Color.white, woodTex, 7, 1, 0.2f);
        woodDark = M("WoodDark", Color.white, woodTex, 2, 2, 0.2f);
        plaster = M("Plaster", Color.white, plasterTex, 6, 2, 0.05f);
        cloth = M("Cloth", Color.white, clothTex, 1, 1, 0.1f);
        steel = M("Steel", C("#e6eaee"), null, 1, 1, 0.8f, 0.5f, new Color(0.35f, 0.37f, 0.40f));
        slotGlow = M("SlotGlow", C("#050404"), null, 1, 1, 0.0f);
        threadM = M("ThreadSeam", C("#692B34"), null, 1, 1, 0.1f, 0f, new Color(0.45f, 0.08f, 0.12f));
        ghostM = M("GhostCloth", C("#5c5852"), clothTex, 1, 1, 0.05f);
        hoodDark = M("HoodVoid", C("#050404"), null, 1, 1, 0.0f);
        bordeaux = M("Bordeaux", C("#34131a"), null, 1, 1, 0.1f);
        brass = M("Brass", C("#A17C4D"), null, 1, 1, 0.55f, 0.9f);
        wax = M("Wax", C("#D9D2C5"), null, 1, 1, 0.3f);
        flame = M("Flame", C("#ffb050"), null, 1, 1, 0.0f, 0f, new Color(2.0f, 1.05f, 0.32f));
        glass = M("WindowGlass", C("#2a3340"), null, 1, 1, 0.0f, 0f, new Color(0.30f, 0.42f, 0.60f) * 1.6f);
        discDark = M("DistortionDisc", C("#0b0708"), null, 1, 1, 0.85f);
        frameDark = M("FrameDark", C("#0d0b0a"), null, 1, 1, 0.2f);
        woodBlack = M("WoodBlack", C("#151110"), null, 1, 1, 0.35f);
    }

    // ---------- допоміжне ----------
    static GameObject P(PrimitiveType t, string n, Vector3 pos, Vector3 sc, Material m, Transform par,
                        bool col = true, Vector3? rot = null)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = n;
        g.transform.SetParent(par, false);
        g.transform.localPosition = pos;
        g.transform.localScale = sc;
        if (rot.HasValue) g.transform.localEulerAngles = rot.Value;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static Light AddLight(Transform par, string n, Vector3 pos, LightType type, Color c, float intensity, float range, bool flicker)
    {
        var go = new GameObject(n);
        go.transform.SetParent(par, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = type; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        if (flicker) go.AddComponent<CandleFlicker>();
        return l;
    }

    static void Candle(Transform par, Vector3 pos, float h, bool light, float lightIntensity = 4f)
    {
        var root = new GameObject("Candle"); root.transform.SetParent(par, false); root.transform.localPosition = pos;
        P(PrimitiveType.Cylinder, "Holder", new Vector3(0, 0.015f, 0), new Vector3(0.11f, 0.015f, 0.11f), brass, root.transform, false);
        P(PrimitiveType.Cylinder, "Wax", new Vector3(0, 0.03f + h / 2f, 0), new Vector3(0.055f, h / 2f, 0.055f), wax, root.transform, false);
        P(PrimitiveType.Sphere, "Flame", new Vector3(0, 0.03f + h + 0.035f, 0), new Vector3(0.03f, 0.055f, 0.03f), flame, root.transform, false);
        if (light)
            AddLight(root.transform, "Light", new Vector3(0, 0.03f + h + 0.08f, 0), LightType.Point,
                     C("#ff9a4d"), lightIntensity, 5.5f, true);
    }

    static void Robe(Transform par, Vector3 pos, float h, int variant)
    {
        var r = new GameObject("Figure" + variant); r.transform.SetParent(par, false); r.transform.localPosition = pos;
        float s = 1f;
        P(PrimitiveType.Cylinder, "RobeLow", new Vector3(0, h * 0.28f, 0), new Vector3(0.66f * s, h * 0.28f, 0.66f * s), cloth, r.transform);
        P(PrimitiveType.Cylinder, "RobeUp", new Vector3(0, h * 0.62f, 0), new Vector3(0.46f * s, h * 0.12f, 0.40f * s), cloth, r.transform, false);
        P(PrimitiveType.Sphere, "Shoulders", new Vector3(0, h * 0.78f, 0), new Vector3(0.64f * s, 0.30f, 0.42f * s), cloth, r.transform, false);
        P(PrimitiveType.Sphere, "Hood", new Vector3(0, h * 0.90f, 0.0f), new Vector3(0.34f, 0.44f, 0.38f), cloth, r.transform, false);
        P(PrimitiveType.Sphere, "HoodVoid", new Vector3(0, h * 0.89f, 0.15f), new Vector3(0.20f, 0.26f, 0.12f), hoodDark, r.transform, false);
        if (variant % 3 == 0)
        {
            P(PrimitiveType.Capsule, "SleeveL", new Vector3(-0.34f, h * 0.60f, 0.02f), new Vector3(0.13f, 0.42f, 0.13f), cloth, r.transform, false);
            P(PrimitiveType.Capsule, "SleeveR", new Vector3(0.34f, h * 0.60f, 0.02f), new Vector3(0.13f, 0.42f, 0.13f), cloth, r.transform, false);
        }
        if (variant % 3 == 1)
            P(PrimitiveType.Cube, "BackSeam", new Vector3(0, h * 0.50f, -0.30f), new Vector3(0.03f, h * 0.55f, 0.01f), hoodDark, r.transform, false);
        r.transform.localEulerAngles = new Vector3(0, (variant % 2 == 0 ? 4f : -5f), (variant % 3 - 1) * 1.2f);
    }

    static void SeatedGhost(Transform par, Vector3 pos, float k)
    {
        var r = new GameObject("PewGhost"); r.transform.SetParent(par, false); r.transform.localPosition = pos;
        P(PrimitiveType.Capsule, "Body", new Vector3(0, 0.88f, 0), new Vector3(0.52f * k, 0.44f * k, 0.44f * k), ghostM, r.transform, false);
        P(PrimitiveType.Sphere, "Head", new Vector3(0, 1.36f * k, 0), new Vector3(0.34f * k, 0.40f * k, 0.34f * k), ghostM, r.transform, false);
    }

    // ---------- Volume-профілі ----------
    static VolumeProfile MakeProfile(string name, System.Action<VolumeProfile> fill)
    {
        string path = Dir + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        fill(p);
        AssetDatabase.CreateAsset(p, path);
        foreach (var comp in p.components) AssetDatabase.AddObjectToAsset(comp, p);
        AssetDatabase.SaveAssets();
        return p;
    }

    static void Ensure(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static GameObject FindRoot(Scene sc, string n)
    {
        foreach (var g in sc.GetRootGameObjects()) if (g.name == n) return g;
        return null;
    }

    static void RestylePrefab()
    {
        string path = "Assets/Prefabs/Candle.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        var mr = root.GetComponent<Renderer>();
        if (mr != null) mr.sharedMaterial = wax;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (r.gameObject != root) r.sharedMaterial = flame;
        foreach (var l in root.GetComponentsInChildren<Light>(true))
        {
            l.color = C("#ff9a4d"); l.intensity = 3f; l.range = 5f; l.shadows = LightShadows.None;
            if (l.GetComponent<CandleFlicker>() == null) l.gameObject.AddComponent<CandleFlicker>();
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }


    static void AddVisual(GameObject item, string kind)
    {
        var old = item.transform.Find("Visual"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var mr = item.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
        var v = new GameObject("Visual").transform; v.SetParent(item.transform, false);
        v.localPosition = new Vector3(0, -0.34f, 0);
        if (kind == "Spool")
        {
            v.localPosition = Vector3.zero;
            P(PrimitiveType.Cylinder, "Body", Vector3.zero, new Vector3(0.9f, 0.4f, 0.9f), woodDark, v, false);
            P(PrimitiveType.Cylinder, "Thread", Vector3.zero, new Vector3(0.95f, 0.26f, 0.95f), threadM, v, false);
        }
        else if (kind == "Needle")
        {
            P(PrimitiveType.Cylinder, "Shaft", Vector3.zero, new Vector3(0.14f, 0.9f, 0.14f), steel, v, false, new Vector3(0, 0, 90));
            P(PrimitiveType.Sphere, "Eye", new Vector3(0.88f, 0, 0), new Vector3(0.22f, 0.12f, 0.22f), steel, v, false);
        }
        else
        {
            P(PrimitiveType.Cube, "Cloth", Vector3.zero, new Vector3(1.6f, 0.12f, 1.2f), cloth, v, false, new Vector3(0, 12, 0));
            P(PrimitiveType.Cube, "Cloth2", new Vector3(0.08f, 0.1f, 0.02f), new Vector3(1.2f, 0.1f, 0.9f), cloth, v, false, new Vector3(0, -8, 0));
        }
    }

    static void BuildGame(Transform root, GameObject fabric, GameObject snap, Light altarSpot, Transform figs, GameObject padSrc)
    {
        var game = new GameObject("RitualGame").transform; game.SetParent(root, false);

        // старі копії (якщо дизайн застосовують повторно)
        var oldVis = fabric.transform.Find("Visual"); if (oldVis != null) Object.DestroyImmediate(oldVis.gameObject);
        fabric.name = "Fabric";

        var spool = Object.Instantiate(fabric); spool.name = "Spool";
        var needle = Object.Instantiate(fabric); needle.name = "Needle";
        spool.transform.SetParent(game, true); needle.transform.SetParent(game, true);

        // тумби
        var pos = new Dictionary<string, Vector3>
        {
            { "Spool", new Vector3(-1.45f, 0f, 3.4f) },
            { "Needle", new Vector3(1.45f, 0f, 4.0f) },
            { "Fabric", new Vector3(-1.45f, 0f, 4.6f) },
        };
        foreach (var kv in pos)
        {
            var pl = new GameObject("Plinth_" + kv.Key).transform; pl.SetParent(game, false); pl.position = kv.Value;
            P(PrimitiveType.Cube, "Column", new Vector3(0, 0.5f, 0), new Vector3(0.4f, 1.0f, 0.4f), woodBlack, pl);
            P(PrimitiveType.Cube, "Top", new Vector3(0, 1.015f, 0), new Vector3(0.5f, 0.03f, 0.5f), brass, pl);
            AddLight(game, "ItemGlow_" + kv.Key, kv.Value + new Vector3(0, 1.7f, 0), LightType.Point, C("#ffd9a0"), 3.5f, 2.6f, false);
        }
        var items = new[] { spool, needle, fabric };
        foreach (var it in items)
        {
            AddVisual(it, it.name);
            it.transform.position = pos[it.name] + new Vector3(0, 1.075f, 0);
            it.transform.rotation = Quaternion.identity;
            if (it.GetComponent<RitualItem>() == null) it.AddComponent<RitualItem>();
        }

        // слоти на вівтарі (Snap Zone)
        var slotPos = new Dictionary<string, Vector3>
        {
            { "Spool", new Vector3(0.55f, 1.30f, 7.0f) },
            { "Needle", new Vector3(-0.55f, 1.30f, 7.0f) },
            { "Fabric", new Vector3(0.0f, 1.30f, 7.0f) },
        };
        var slots = new List<SnapZone>();
        foreach (var kv in slotPos)
        {
            GameObject z;
            if (kv.Key == "Fabric") z = snap;
            else { z = Object.Instantiate(snap); z.name = kv.Key + "Slot"; z.transform.SetParent(game, true); }
            z.transform.position = kv.Value;
            z.transform.rotation = Quaternion.Euler(0, kv.Key == "Needle" ? 20f : 0f, 0);
            var bc = z.GetComponent<BoxCollider>(); if (bc != null) bc.size = new Vector3(0.4f, 0.4f, 0.4f);
            var sn = z.GetComponent<SnapZone>(); sn.requiredName = kv.Key;
            slots.Add(sn);
            P(PrimitiveType.Cylinder, "SlotMarker_" + kv.Key, new Vector3(kv.Value.x, 1.285f, kv.Value.z), new Vector3(0.24f, 0.003f, 0.24f), slotGlow, game, false);
        }

        // додаткові точки телепортації: до котушки й на платформу вівтаря
        if (padSrc != null)
        {
            var padSpool = Object.Instantiate(padSrc); padSpool.name = "TeleportPadSpool";
            padSpool.transform.SetParent(game, true); padSpool.transform.position = new Vector3(-0.9f, 0.02f, 3.5f);
            var padAltar = Object.Instantiate(padSrc); padAltar.name = "TeleportPadAltar";
            padAltar.transform.SetParent(game, true); padAltar.transform.position = new Vector3(0f, 0.42f, 6.2f);
        }

        // нитка, яка з'являється, коли сорочку зшито
        var seam = new GameObject("ThreadSeam").transform; seam.SetParent(game, false);
        P(PrimitiveType.Cube, "SeamSleeve", new Vector3(-0.58f, 1.285f, 7.30f), new Vector3(0.78f, 0.006f, 0.014f), threadM, seam, false, new Vector3(0, 14, 0));
        P(PrimitiveType.Cube, "SeamPlacket", new Vector3(0.0f, 1.292f, 7.28f), new Vector3(0.012f, 0.006f, 0.74f), threadM, seam, false);
        P(PrimitiveType.Cube, "SeamHem", new Vector3(0.0f, 1.285f, 7.68f), new Vector3(0.56f, 0.006f, 0.012f), threadM, seam, false);

        // напис над вівтарем
        var lab = new GameObject("RitualLabel"); lab.transform.SetParent(game, false);
        lab.transform.position = new Vector3(0, 3.0f, 8.55f);
        var tm = lab.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lab.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        tm.fontSize = 80; tm.characterSize = 0.05f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.color = C("#d9d2c5"); tm.text = "COLLECT  SPOOL  -  NEEDLE  -  FABRIC\n0 / 3";

        var rg = game.gameObject.AddComponent<RitualGame>();
        rg.slots = slots.ToArray(); rg.label = tm; rg.seam = seam.gameObject; rg.altarLight = altarSpot;
        var ft = new List<FigureTurn>();
        foreach (Transform f in figs) { var t = f.gameObject.GetComponent<FigureTurn>(); if (t == null) t = f.gameObject.AddComponent<FigureTurn>(); ft.Add(t); }
        rg.figures = ft.ToArray();
    }

    [MenuItem("Tools/Apply Chapel Design")]
    public static void Build()
    {
        Ensure("Assets", "Design"); Ensure("Assets/Design", "Materials"); Ensure("Assets/Design", "Textures");
        Random.InitState(7);
        MakeMaterials();
        RestylePrefab();

        var scene = EditorSceneManager.GetActiveScene();
        bool vr = GameObject.Find("[BuildingBlock] Camera Rig") != null;

        // ----- прибираємо стару декорацію -----
        foreach (var n in new[] { "ChapelDesign", "Room", "Altar", "Ceiling", "BenchLeft", "BenchRight", "Figure1", "Figure2", "Figure3" })
        {
            GameObject g;
            while ((g = FindRoot(scene, n)) != null) Object.DestroyImmediate(g);
        }

        var root = new GameObject("ChapelDesign").transform;

        // ----- атмосфера -----
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.115f, 0.135f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.045f, 0.040f, 0.045f);
        RenderSettings.fogDensity = 0.015f;
        var dl = FindRoot(scene, "Directional Light");
        if (dl != null)
        {
            var l = dl.GetComponent<Light>();
            l.intensity = 0.10f; l.color = C("#9fb0d0"); l.shadows = LightShadows.None;
            dl.transform.rotation = Quaternion.Euler(55, -30, 0);
        }

        var arch = new GameObject("Architecture").transform; arch.SetParent(root, false);


        // ----- підлога -----
        P(PrimitiveType.Cube, "Floor", new Vector3(0, -0.1f, 0), new Vector3(10, 0.2f, 18), stone, arch);
        P(PrimitiveType.Cube, "Runner", new Vector3(0, 0.011f, -1.3f), new Vector3(2.2f, 0.02f, 14.3f), bordeaux, arch, false);

        // ----- стіни -----
        foreach (int sx in new[] { -1, 1 })
        {
            P(PrimitiveType.Cube, "WoodPanel", new Vector3(sx * 5f, 1.1f, 0), new Vector3(0.4f, 2.2f, 18), woodWall, arch);
            P(PrimitiveType.Cube, "WallUpper", new Vector3(sx * 5f, 4.6f, 0), new Vector3(0.4f, 4.8f, 18), plaster, arch);
        }
        foreach (int sz in new[] { -1, 1 })
        {
            P(PrimitiveType.Cube, "EndWood", new Vector3(0, 1.1f, sz * 9.2f), new Vector3(10.4f, 2.2f, 0.4f), woodWall, arch);
            P(PrimitiveType.Cube, "EndUpper", new Vector3(0, 5.85f, sz * 9.2f), new Vector3(10.4f, 7.3f, 0.4f), plaster, arch);
        }

        // ----- дах, ферми, колони -----
        foreach (int sx in new[] { -1, 1 })
            P(PrimitiveType.Cube, "Roof", new Vector3(sx * 2.5f, 8.3f, 0), new Vector3(5.8f, 0.25f, 18.4f), woodDark, arch, false, new Vector3(0, 0, -sx * 26.57f));
        P(PrimitiveType.Cube, "RidgeBeam", new Vector3(0, 9.2f, 0), new Vector3(0.3f, 0.3f, 18.2f), frameDark, arch, false);
        float[] trusses = { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
        foreach (float z in trusses)
        {
            foreach (int sx in new[] { -1, 1 })
            {
                P(PrimitiveType.Cube, "TrussBeam", new Vector3(sx * 2.45f, 7.95f, z), new Vector3(5.9f, 0.32f, 0.3f), woodBlack, arch, false, new Vector3(0, 0, -sx * 26.57f));
                P(PrimitiveType.Cube, "Column", new Vector3(sx * 4.3f, 3.5f, z), new Vector3(0.7f, 7f, 0.7f), woodBlack, arch);
                P(PrimitiveType.Cube, "ColumnBase", new Vector3(sx * 4.3f, 0.25f, z), new Vector3(0.95f, 0.5f, 0.95f), stone, arch, false);
                P(PrimitiveType.Cube, "ColumnCap", new Vector3(sx * 4.3f, 6.85f, z), new Vector3(0.95f, 0.3f, 0.95f), woodBlack, arch, false);
            }
        }

        // ----- вікна -----
        float[] wz = { -6f, -3f, 0f, 3f, 6f };
        for (int i = 0; i < wz.Length; i++)
            foreach (int sx in new[] { -1, 1 })
            {
                P(PrimitiveType.Cube, "WindowPane", new Vector3(sx * 4.76f, 4.4f, wz[i]), new Vector3(0.05f, 3.4f, 0.85f), glass, arch, false);
                P(PrimitiveType.Cube, "WinBarV", new Vector3(sx * 4.74f, 4.4f, wz[i]), new Vector3(0.07f, 3.5f, 0.06f), frameDark, arch, false);
                P(PrimitiveType.Cube, "WinBarH", new Vector3(sx * 4.74f, 4.9f, wz[i]), new Vector3(0.07f, 0.06f, 0.9f), frameDark, arch, false);
                P(PrimitiveType.Cube, "WinBarH2", new Vector3(sx * 4.74f, 3.7f, wz[i]), new Vector3(0.07f, 0.06f, 0.9f), frameDark, arch, false);
                if (i % 2 == 0)
                {
                    var sl = AddLight(root, "WindowBeam", new Vector3(sx * 4.2f, 6.2f, wz[i]), LightType.Spot, C("#8fa6cc"), 110f, 15f, false);
                    sl.spotAngle = 42f;
                    sl.transform.LookAt(new Vector3(-sx * 1.2f, 0f, wz[i]));
                }
            }
        // велике вікно за вівтарем
        P(PrimitiveType.Cube, "BigWindow", new Vector3(0, 5.2f, 8.96f), new Vector3(1.5f, 4.4f, 0.05f), glass, arch, false);
        P(PrimitiveType.Cube, "BigWindowTop", new Vector3(0, 7.4f, 8.96f), new Vector3(1.05f, 1.05f, 0.05f), glass, arch, false, new Vector3(0, 0, 45));
        P(PrimitiveType.Cube, "BigBarV", new Vector3(0, 5.6f, 8.93f), new Vector3(0.07f, 5.4f, 0.06f), frameDark, arch, false);
        P(PrimitiveType.Cube, "BigBarH", new Vector3(0, 5.2f, 8.93f), new Vector3(1.6f, 0.07f, 0.06f), frameDark, arch, false);
        P(PrimitiveType.Cube, "BigBarH2", new Vector3(0, 6.4f, 8.93f), new Vector3(1.6f, 0.07f, 0.06f), frameDark, arch, false);
        AddLight(root, "BigWindowGlow", new Vector3(0, 5.2f, 7.8f), LightType.Point, C("#7f98c4"), 28f, 12f, false);

        // ----- вівтар -----
        var altar = new GameObject("Altar").transform; altar.SetParent(root, false);
        P(PrimitiveType.Cube, "Platform", new Vector3(0, 0.2f, 7.2f), new Vector3(4.6f, 0.4f, 2.7f), stone, altar);
        P(PrimitiveType.Cube, "Table", new Vector3(0, 0.825f, 7.35f), new Vector3(2.4f, 0.85f, 0.9f), woodBlack, altar);
        float ty = 1.26f;
        P(PrimitiveType.Cube, "ShirtBody", new Vector3(0, ty, 7.30f), new Vector3(0.58f, 0.03f, 0.78f), cloth, altar, false);
        P(PrimitiveType.Cube, "SleeveL", new Vector3(-0.58f, ty + 0.005f, 7.40f), new Vector3(0.78f, 0.03f, 0.20f), cloth, altar, false, new Vector3(0, 14, 0));
        P(PrimitiveType.Cube, "SleeveR", new Vector3(0.58f, ty + 0.005f, 7.42f), new Vector3(0.78f, 0.03f, 0.20f), cloth, altar, false, new Vector3(0, -22, 0));
        P(PrimitiveType.Cube, "Collar", new Vector3(0, ty + 0.012f, 6.94f), new Vector3(0.28f, 0.035f, 0.10f), cloth, altar, false);
        P(PrimitiveType.Cube, "Placket", new Vector3(0, ty + 0.018f, 7.28f), new Vector3(0.03f, 0.036f, 0.70f), plaster, altar, false);
        for (int i = 0; i < 4; i++)
            P(PrimitiveType.Sphere, "Button", new Vector3(0, ty + 0.035f, 7.05f + i * 0.17f), new Vector3(0.028f, 0.012f, 0.028f), brass, altar, false);
        if (!vr) P(PrimitiveType.Cylinder, "Spool", new Vector3(0.95f, ty + 0.05f, 7.25f), new Vector3(0.09f, 0.05f, 0.09f), woodDark, altar, false);
        if (!vr) P(PrimitiveType.Cylinder, "Thread", new Vector3(0.95f, ty + 0.05f, 7.25f), new Vector3(0.095f, 0.035f, 0.095f), hoodDark, altar, false);
        P(PrimitiveType.Cube, "ScissorA", new Vector3(-0.95f, ty + 0.01f, 7.2f), new Vector3(0.025f, 0.01f, 0.24f), brass, altar, false, new Vector3(0, 12, 0));
        P(PrimitiveType.Cube, "ScissorB", new Vector3(-0.95f, ty + 0.012f, 7.2f), new Vector3(0.025f, 0.01f, 0.24f), brass, altar, false, new Vector3(0, -12, 0));
        P(PrimitiveType.Cube, "Sketch", new Vector3(0.72f, ty - 0.005f, 7.58f), new Vector3(0.32f, 0.006f, 0.22f), wax, altar, false, new Vector3(0, 18, 0));
        Candle(altar, new Vector3(-1.05f, 1.25f, 7.10f), 0.30f, true, 5f);
        Candle(altar, new Vector3(-0.88f, 1.25f, 7.48f), 0.18f, false);
        Candle(altar, new Vector3(-1.12f, 1.25f, 7.55f), 0.24f, false);
        Candle(altar, new Vector3(1.10f, 1.25f, 7.05f), 0.26f, true, 5f);
        Candle(altar, new Vector3(0.90f, 1.25f, 7.55f), 0.34f, false);
        var spot = AddLight(root, "AltarSpot", new Vector3(0, 6.0f, 5.9f), LightType.Spot, C("#ffe6c4"), 120f, 12f, false);
        spot.spotAngle = 34f;
        spot.transform.LookAt(new Vector3(0, 1.25f, 7.3f));

        // ----- лави -----
        var pews = new GameObject("Pews").transform; pews.SetParent(root, false);
        float[] rows = { -6f, -4.8f, -3.6f, -2.4f, -1.2f, 0f };
        foreach (int sx in new[] { -1, 1 })
            foreach (float z in rows)
            {
                float cx = sx * 2.55f;
                P(PrimitiveType.Cube, "Seat", new Vector3(cx, 0.46f, z), new Vector3(2.9f, 0.06f, 0.45f), woodDark, pews);
                P(PrimitiveType.Cube, "Back", new Vector3(cx, 0.9f, z - 0.22f), new Vector3(2.9f, 0.8f, 0.06f), woodDark, pews);
                P(PrimitiveType.Cube, "EndAisle", new Vector3(sx * 1.1f, 0.5f, z - 0.05f), new Vector3(0.07f, 1.0f, 0.6f), woodBlack, pews);
                P(PrimitiveType.Cube, "EndWall", new Vector3(sx * 4.0f, 0.5f, z - 0.05f), new Vector3(0.07f, 1.0f, 0.6f), woodBlack, pews);
                P(PrimitiveType.Cube, "CrossV", new Vector3(sx * 1.06f, 0.62f, z - 0.05f), new Vector3(0.02f, 0.34f, 0.045f), brass, pews, false);
                P(PrimitiveType.Cube, "CrossH", new Vector3(sx * 1.06f, 0.68f, z - 0.05f), new Vector3(0.02f, 0.045f, 0.2f), brass, pews, false);
            }
        var ghosts = new GameObject("PewGhosts").transform; ghosts.SetParent(root, false);
        SeatedGhost(ghosts, new Vector3(-2.2f, 0f, -4.8f), 1.0f);
        SeatedGhost(ghosts, new Vector3(-3.1f, 0f, -2.4f), 0.95f);
        SeatedGhost(ghosts, new Vector3(-1.9f, 0f, -0.0f), 1.05f);
        SeatedGhost(ghosts, new Vector3(2.6f, 0f, -6.0f), 1.0f);
        SeatedGhost(ghosts, new Vector3(1.9f, 0f, -3.6f), 1.05f);
        SeatedGhost(ghosts, new Vector3(3.2f, 0f, -1.2f), 0.97f);

        // ----- постаті в балахонах -----
        var figs = new GameObject("Figures").transform; figs.SetParent(root, false);
        Robe(figs, new Vector3(-2.5f, 0f, 3.8f), 1.82f, 0);
        Robe(figs, new Vector3(-2.7f, 0f, 5.2f), 1.74f, 1);
        Robe(figs, new Vector3(2.5f, 0f, 3.8f), 1.86f, 2);
        Robe(figs, new Vector3(2.7f, 0f, 5.2f), 1.78f, 3);
        Robe(figs, new Vector3(-1.5f, 0.4f, 6.6f), 1.80f, 4);
        Robe(figs, new Vector3(1.6f, 0.4f, 6.7f), 1.88f, 5);
        Robe(figs, new Vector3(-3.3f, 0f, 7.4f), 1.76f, 6);

        // ----- люстри та бра -----
        var lights = new GameObject("Candlelight").transform; lights.SetParent(root, false);
        foreach (float z in new[] { -4.5f, 0.5f, 4.5f })
        {
            var ch = new GameObject("Chandelier").transform; ch.SetParent(lights, false); ch.localPosition = new Vector3(0, 5.0f, z);
            P(PrimitiveType.Cylinder, "Chain", new Vector3(0, 2.15f, 0), new Vector3(0.025f, 2.15f, 0.025f), frameDark, ch, false);
            P(PrimitiveType.Cylinder, "Ring", Vector3.zero, new Vector3(1.3f, 0.02f, 1.3f), frameDark, ch, false);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Candle(ch, new Vector3(Mathf.Cos(a) * 0.62f, 0.02f, Mathf.Sin(a) * 0.62f), 0.12f, false);
            }
            AddLight(ch, "Light", new Vector3(0, 0.25f, 0), LightType.Point, C("#ff9c52"), 45f, 12f, true);
        }
        float[] sc = { -4.5f, 1.5f, 7.5f };
        foreach (int sx in new[] { -1, 1 })
            foreach (float z in trusses)
            {
                P(PrimitiveType.Cube, "SconceBracket", new Vector3(sx * 3.9f, 2.3f, z), new Vector3(0.12f, 0.05f, 0.2f), brass, lights, false);
                Candle(lights, new Vector3(sx * 3.9f, 2.32f, z), 0.2f, Mathf.Abs(z) < 6f && z != -1.5f, 3.5f);
            }
        // свічки на підлозі біля фігур
        Candle(lights, new Vector3(-2.0f, 0f, 3.5f), 0.25f, false);
        Candle(lights, new Vector3(-3.0f, 0f, 4.2f), 0.18f, false);
        Candle(lights, new Vector3(2.1f, 0f, 3.4f), 0.22f, true, 3f);
        Candle(lights, new Vector3(3.1f, 0f, 5.6f), 0.3f, false);
        Candle(lights, new Vector3(-0.6f, 0f, 5.4f), 0.2f, true, 3f);
        Candle(lights, new Vector3(0.7f, 0f, 5.5f), 0.26f, false);

        // ----- зона спотворення (існуючий об'єкт з лаби 1) -----
        var dz = FindRoot(scene, "DistortionZone");
        if (dz != null)
        {
            dz.transform.position = new Vector3(0, 1.25f, 2.0f);
            dz.transform.localScale = new Vector3(3.4f, 2.5f, 3.4f);
        }
        P(PrimitiveType.Cylinder, "DistortionDisc", new Vector3(0, 0.014f, 2.0f), new Vector3(3.4f, 0.004f, 3.4f), discDark, root, false);

        // ----- фізичні свічки (Prefab) -----
        float[] candleX = { -0.75f, -0.25f, 0.25f, 0.75f };
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        var candlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Candle.prefab");
        var oldCandles = new System.Collections.Generic.List<GameObject>();
        foreach (var g in scene.GetRootGameObjects())
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(g)) continue;
            var src2 = PrefabUtility.GetCorrespondingObjectFromSource(g);
            if (src2 != null && src2.name == "Candle") oldCandles.Add(g);
        }
        int cnt = Mathf.Max(oldCandles.Count, 4);
        foreach (var g in oldCandles) Object.DestroyImmediate(g);
        for (int ci = 0; ci < cnt; ci++)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(candlePrefab, scene);
            inst.name = ci == 0 ? "Candle" : "Candle (" + ci + ")";
            inst.transform.position = new Vector3(candleX[ci % 4], 0.6f + 0.1f * (ci / 4), -5.0f - 0.2f * (ci % 2));
            inst.transform.rotation = Quaternion.identity;
            var waxM = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "Wax.mat");
            var flameM = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "Flame.mat");
            var rr = inst.GetComponent<Renderer>(); if (rr != null) rr.sharedMaterial = waxM;
            foreach (var r2 in inst.GetComponentsInChildren<Renderer>(true)) if (r2.gameObject != inst) r2.sharedMaterial = flameM;
        }

        if (vr)
        {
            var rig = GameObject.Find("[BuildingBlock] Camera Rig");
            rig.transform.position = new Vector3(0, 0, 2.8f);
            rig.transform.rotation = Quaternion.identity;
            var p1 = FindRoot(scene, "TeleportPad1"); if (p1 != null) p1.transform.position = new Vector3(1.0f, 0.02f, 4.8f);
            var p2 = FindRoot(scene, "TeleportPad2"); if (p2 != null) p2.transform.position = new Vector3(-1.0f, 0.02f, 4.8f);
            var cube = FindRoot(scene, "Fabric");
            if (cube == null) cube = FindRoot(scene, "[BuildingBlock] Cube");
            var sz = FindRoot(scene, "SnapZone");
            if (cube != null && sz != null) BuildGame(root, cube, sz, spot, figs, FindRoot(scene, "TeleportPad1"));
            var hot = FindRoot(scene, "Teleport Hotspot");
            if (hot != null) hot.transform.position = new Vector3(0, 0, 0.8f);
        }
        else
        {
            var pl = FindRoot(scene, "Player");
            if (pl != null) { pl.transform.position = new Vector3(0, 1.05f, -7.6f); pl.transform.rotation = Quaternion.identity; }
        }

        // ----- камери: увімкнути пост-обробку -----
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var ud = cam.GetUniversalAdditionalCameraData();
            ud.renderPostProcessing = true;
            cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.05f);
        }

        // ----- постобробка (окремий Volume) -----
        var main = MakeProfile("PostFX", p =>
        {
            var bloom = p.Add<Bloom>(true); bloom.threshold.Override(1.0f); bloom.intensity.Override(0.55f); bloom.scatter.Override(0.72f);
            var vig = p.Add<Vignette>(true); vig.intensity.Override(0.34f); vig.smoothness.Override(0.55f);
            var ca = p.Add<ColorAdjustments>(true); ca.contrast.Override(20f); ca.saturation.Override(-20f); ca.postExposure.Override(0.5f);
            var gr = p.Add<FilmGrain>(true); gr.type.Override(FilmGrainLookup.Thin1); gr.intensity.Override(vr ? 0.15f : 0.4f);
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        });
        var gv = new GameObject("PostFX Volume"); gv.transform.SetParent(root, false);
        var vol = gv.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = main; vol.weight = 1f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Дизайн каплиці застосовано до сцени " + scene.name + (vr ? " (VR)" : ""));
    }
}
