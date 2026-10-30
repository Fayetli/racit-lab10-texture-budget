using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Builds the Lab 10 scene from the ready-made textures in Assets/Textures.
// Menu: Lab → Rebuild Texture Budget Scene (for the instructor; students do not need it).
public static class LabSceneBuilder
{
    const string ScenePath = "Assets/Scenes/TextureBudgetLab.unity";

    static Texture2D T(string n) { return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + n + ".png"); }

    static Material MakeLit(string name, string albedo, string normal, Vector2 tiling, Vector2 offset, bool transparent, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetTexture("_BaseMap", T(albedo)); m.SetTextureScale("_BaseMap", tiling); m.SetTextureOffset("_BaseMap", offset);
        if (normal != null)
        {
            m.SetTexture("_BumpMap", T(normal)); m.SetTextureScale("_BumpMap", tiling); m.SetTextureOffset("_BumpMap", offset);
            m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
        }
        m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
        if (transparent)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_SrcBlend", 5); m.SetFloat("_DstBlend", 10); m.SetFloat("_ZWrite", 0);
            m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000; m.SetShaderPassEnabled("ShadowCaster", false);
            // no specular highlights: with alpha blending they would show through on fully transparent pixels
            m.SetFloat("_SpecularHighlights", 0); m.SetFloat("_EnvironmentReflections", 0);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        }
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m, Transform parent)
    {
        var g = GameObject.CreatePrimitive(t); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = pos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Group(string n, Transform p = null)
    {
        var g = new GameObject(n); if (p != null) g.transform.SetParent(p, false); return g;
    }

    static RectTransform UIRegion(Transform parent, Texture atlas, string name, float x, float y, float w, float h, Vector2 anchor, Vector2 pos, float scale)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = new Vector2(w * scale, h * scale);
        rt.anchoredPosition = pos;
        var ri = go.GetComponent<RawImage>();
        ri.texture = atlas;
        ri.uvRect = new Rect(x / 2048f, 1f - (y + h) / 2048f, w / 2048f, h / 2048f);
        ri.raycastTarget = false;
        return rt;
    }

    static RectTransform UIText(Transform parent, string name, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text; t.fontSize = size; t.alignment = align; t.fontStyle = FontStyle.Bold;
        t.color = Color.white; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        return rt;
    }

    static Material MakeColor(string name, Color c, float metallic, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    static readonly string[] TextureNames = { "Ground_Albedo", "Ground_Normal", "Building_Albedo", "Building_Normal", "Props_Albedo", "Props_Normal",
                                              "Skybox", "Character_Albedo", "Character_Normal", "UI_Atlas", "Decals", "Noise" };

    // Initial import state: uncompressed RGBA32, mipmaps, Max Size 2048, no platform overrides (156 MB in total).
    [MenuItem("Lab/Reset Texture Import Settings (156 MB)")]
    public static void ResetTextures()
    {
        foreach (var n in TextureNames)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath("Assets/Textures/" + n + ".png");
            if (imp == null) { Debug.LogError("Немає текстури " + n); continue; }
            bool normal = n.EndsWith("_Normal"), alpha = n == "Decals" || n == "UI_Atlas";
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (!normal) imp.sRGBTexture = n != "Noise";   // noise is utility data, not colour
            imp.mipmapEnabled = true; imp.streamingMipmaps = false; imp.isReadable = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = 2048; imp.anisoLevel = 4; imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.alphaIsTransparency = alpha;
            imp.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            var ps = imp.GetDefaultPlatformTextureSettings();
            ps.format = TextureImporterFormat.RGBA32; ps.maxTextureSize = 2048; ps.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SetPlatformTextureSettings(ps);
            foreach (var plat in new[] { "Android", "iPhone", "Standalone", "WebGL" }) imp.ClearPlatformTextureSettings(plat);
            imp.SaveAndReimport();
        }
        Debug.Log("Імпорт текстур повернено до початкового стану.");
    }

    [MenuItem("Lab/Rebuild Texture Budget Scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var mGround = MakeLit("M_Ground", "Ground_Albedo", "Ground_Normal", new Vector2(10, 10), Vector2.zero, false, 0f);
        mGround.SetTexture("_DetailAlbedoMap", T("Noise")); mGround.SetTextureScale("_DetailAlbedoMap", new Vector2(40, 40));
        mGround.SetFloat("_DetailAlbedoMapScale", 1f); mGround.EnableKeyword("_DETAIL_MULX2");
        var mBld = MakeLit("M_Building", "Building_Albedo", "Building_Normal", new Vector2(2, 2), Vector2.zero, false, 0.1f);
        var mBldTall = MakeLit("M_Building_Tall", "Building_Albedo", "Building_Normal", new Vector2(2, 3), Vector2.zero, false, 0.1f);
        var mProps = MakeLit("M_Props", "Props_Albedo", "Props_Normal", Vector2.one, Vector2.zero, false, 0.15f);
        var mChar = MakeLit("M_Character", "Character_Albedo", "Character_Normal", new Vector2(2, 2), Vector2.zero, false, 0.25f);
        string[] dn = { "Oil", "Cracks", "Arrow", "Splat" };
        Vector2[] offs = { new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(0.5f, 0) };
        var dec = new Material[4];
        for (int i = 0; i < 4; i++) dec[i] = MakeLit("M_Decal_" + dn[i], "Decals", null, new Vector2(0.5f, 0.5f), offs[i], true, 0f);

        var sky = new Material(Shader.Find("Skybox/Panoramic")) { name = "M_Sky" };
        sky.SetTexture("_MainTex", T("Skybox")); sky.SetFloat("_Mapping", 1); sky.SetFloat("_ImageType", 0);
        AssetDatabase.CreateAsset(sky, "Assets/Materials/M_Sky.mat");
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;

        var env = Group("Environment");
        Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(8, 1, 8), mGround, env.transform);
        var blds = Group("Buildings", env.transform);
        Vector3[] bp = { new Vector3(-14, 4, 16), new Vector3(0, 4, 20), new Vector3(14, 4, 16), new Vector3(-26, 4, 6), new Vector3(26, 4, 6) };
        for (int i = 0; i < bp.Length; i++) Prim(PrimitiveType.Cube, "Building_" + (i + 1), bp[i], new Vector3(8, 8, 8), mBld, blds.transform);
        Prim(PrimitiveType.Cube, "Building_Tall", new Vector3(-26, 6, 20), new Vector3(8, 12, 8), mBldTall, blds.transform);

        var props = Group("Props");
        var crates = Group("Crates", props.transform);
        Vector3[] cp = { new Vector3(5, 0.5f, 4), new Vector3(6.1f, 0.5f, 4.3f), new Vector3(5.5f, 1.5f, 4.1f), new Vector3(-6, 0.5f, 5), new Vector3(-6.2f, 1.5f, 5.1f), new Vector3(8, 0.5f, 8) };
        for (int i = 0; i < cp.Length; i++)
        {
            var crate = Prim(PrimitiveType.Cube, "Crate_" + (i + 1), cp[i], Vector3.one, mProps, crates.transform);
            crate.transform.rotation = Quaternion.Euler(0, i * 17f, 0);
            var rb = crate.AddComponent<Rigidbody>(); rb.mass = 6f; rb.linearDamping = 1.5f; rb.angularDamping = 2f;
        }
        var barrels = Group("Barrels", props.transform);
        Vector3[] brp = { new Vector3(-4, 0.6f, 3), new Vector3(-3, 0.6f, 4.2f), new Vector3(3.5f, 0.6f, 7) };
        for (int i = 0; i < brp.Length; i++) Prim(PrimitiveType.Cylinder, "Barrel_" + (i + 1), brp[i], new Vector3(0.8f, 0.6f, 0.8f), mProps, barrels.transform);

        var ch = Group("Character"); ch.transform.position = new Vector3(0, 0, 2);
        Prim(PrimitiveType.Capsule, "Body", new Vector3(0, 1.1f, 0), new Vector3(0.7f, 0.6f, 0.5f), mChar, ch.transform);
        Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 1.95f, 0), Vector3.one * 0.42f, mChar, ch.transform);
        Prim(PrimitiveType.Capsule, "Arm_L", new Vector3(-0.5f, 1.15f, 0), new Vector3(0.2f, 0.5f, 0.2f), mChar, ch.transform);
        Prim(PrimitiveType.Capsule, "Arm_R", new Vector3(0.5f, 1.15f, 0), new Vector3(0.2f, 0.5f, 0.2f), mChar, ch.transform);
        Prim(PrimitiveType.Capsule, "Leg_L", new Vector3(-0.17f, 0.4f, 0), new Vector3(0.24f, 0.4f, 0.24f), mChar, ch.transform);
        Prim(PrimitiveType.Capsule, "Leg_R", new Vector3(0.17f, 0.4f, 0), new Vector3(0.24f, 0.4f, 0.24f), mChar, ch.transform);

        foreach (var col in ch.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        var cc = ch.AddComponent<CharacterController>(); cc.center = new Vector3(0, 1, 0); cc.height = 2f; cc.radius = 0.35f;
        var player = ch.AddComponent<PlayerController>();

        var decals = Group("Decals");
        Vector3[] dp = { new Vector3(-2.5f, 0.02f, 6), new Vector3(2.5f, 0.03f, 5), new Vector3(0, 0.04f, 9), new Vector3(-1f, 0.05f, -1f), new Vector3(-4f, 0.05f, 11f) };
        float[] dsz = { 3, 3, 3, 2, 2 };
        int[] dmat = { 0, 1, 2, 3, 3 };
        for (int i = 0; i < dp.Length; i++)
        {
            var q = Prim(PrimitiveType.Quad, "Decal_" + dn[dmat[i]] + (i == 4 ? "_2" : ""), dp[i], new Vector3(dsz[i], dsz[i], 1), dec[dmat[i]], decals.transform);
            q.transform.rotation = Quaternion.Euler(90, 0, 0);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            if (dmat[i] == 3) { var h = q.AddComponent<Hazard>(); h.kind = Hazard.Kind.Damage; h.radius = 0.9f; }
            if (dmat[i] == 0) { var h = q.AddComponent<Hazard>(); h.kind = Hazard.Kind.Slow; h.radius = 1.0f; }
        }

        // Coins and key use a plain colour material, no textures (so they do not add to the texture budget).
        var mGold = MakeColor("M_Gold", new Color(1f, 0.78f, 0.15f), 0.8f, 0.6f);
        var pk = Group("Pickups");
        Vector3[] coinPos = { new Vector3(-9, 0.8f, 1), new Vector3(9, 0.8f, 0), new Vector3(-8, 0.8f, 10), new Vector3(8, 0.8f, 12),
                              new Vector3(-3, 0.8f, 13), new Vector3(4, 0.8f, 13), new Vector3(-6.2f, 2.5f, 5.1f), new Vector3(12, 0.8f, -3) };
        for (int i = 0; i < coinPos.Length; i++)
        {
            var c = Prim(PrimitiveType.Cylinder, "Coin_" + (i + 1), coinPos[i], new Vector3(0.5f, 0.04f, 0.5f), mGold, pk.transform);
            c.transform.rotation = Quaternion.Euler(90, 0, 0);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.AddComponent<Pickup>().kind = Pickup.Kind.Coin;
        }
        var key = Prim(PrimitiveType.Cube, "Key", new Vector3(0, 0.9f, 13.5f), new Vector3(0.18f, 0.5f, 0.1f), mGold, pk.transform);
        Object.DestroyImmediate(key.GetComponent<Collider>());
        key.AddComponent<Pickup>().kind = Pickup.Kind.Key;
        var keyHead = Prim(PrimitiveType.Sphere, "KeyHead", new Vector3(0, 0.3f, 0), new Vector3(2.2f, 0.9f, 1.5f), mGold, key.transform);
        Object.DestroyImmediate(keyHead.GetComponent<Collider>());

        var light = GameObject.Find("Directional Light");
        light.transform.rotation = Quaternion.Euler(48, -32, 0);
        var l = light.GetComponent<Light>(); l.intensity = 1.3f; l.shadows = LightShadows.Soft; l.color = new Color(1f, 0.96f, 0.88f);
        var cam = Camera.main;
        cam.transform.position = new Vector3(0, 3.2f, -8); cam.transform.rotation = Quaternion.Euler(10, 0, 0);
        cam.fieldOfView = 55; cam.clearFlags = CameraClearFlags.Skybox; cam.farClipPlane = 200;

        // UI: regions of the atlas are shown through RawImage + uvRect.
        var cvGo = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cvGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
        var cs = cvGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        var atlas = T("UI_Atlas"); var tr = cvGo.transform;
        var TL = new Vector2(0, 1); var TR = new Vector2(1, 1); var BC = new Vector2(0.5f, 0); var ML = new Vector2(0, 0.5f);
        var hpBar = UIRegion(tr, atlas, "HP_Bar", 0, 0, 512, 64, TL, new Vector2(30, -30), 1f);
        var hpFill = UIRegion(hpBar, atlas, "HP_Fill", 4, 68, 8, 8, new Vector2(0, 0.5f), new Vector2(8, 0), 1f);
        hpFill.sizeDelta = new Vector2(496, 48);
        var hpText = UIText(hpBar, "HP_Text", "HP 100", 32, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 60));
        UIRegion(tr, atlas, "Btn_Play", 0, 80, 512, 128, ML, new Vector2(30, 90), 0.6f);
        UIRegion(tr, atlas, "Btn_Options", 0, 220, 512, 128, ML, new Vector2(30, 0), 0.6f);
        UIRegion(tr, atlas, "Btn_Quit", 0, 360, 512, 128, ML, new Vector2(30, -90), 0.6f);
        UIRegion(tr, atlas, "Panel_Info", 540, 0, 760, 520, TR, new Vector2(-30, -30), 1f);
        RawImage keyIcon = null;
        for (int i = 0; i < 8; i++)
        {
            var slot = UIRegion(tr, atlas, i == 5 ? "Icon_Key" : "Icon_" + (i + 1), i * 128, 560, 128, 128, BC, new Vector2((i - 3.5f) * 128, 30), 1f);
            if (i == 5) keyIcon = slot.GetComponent<RawImage>();
        }
        var score = UIText(tr, "Score_Text", "", 36, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(30, -120), new Vector2(700, 60));
        var message = UIText(tr, "Message_Text", "", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1400, 300));
        var hint = UIText(tr, "Hint_Text", "WASD — рух · Shift — біг · Пробіл — стрибок · Tab — камера · R — заново", 24, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(30, 20), new Vector2(1000, 40));
        hint.GetComponent<Text>().fontStyle = FontStyle.Normal;

        // Gameplay
        cam.gameObject.AddComponent<FollowCamera>().target = ch.transform;
        player.cameraTransform = cam.transform;
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.player = player; gm.cameraRig = cam.GetComponent<FollowCamera>();
        gm.hpFill = hpFill; gm.hpText = hpText.GetComponent<Text>(); gm.scoreText = score.GetComponent<Text>();
        gm.messageText = message.GetComponent<Text>(); gm.keyIcon = keyIcon;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Сцену збережено: " + ScenePath);
    }
}
