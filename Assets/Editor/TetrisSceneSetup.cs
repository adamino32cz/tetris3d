#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Z příkazové řádky (-executeMethod TetrisBatch.BuildScene) nebo z menu
// postaví scénu Kostka: světlo, kamera, volume, písma, manažer hry.
public static class TetrisSceneSetup
{
    const string ScenePath = "Assets/Scenes/Kostka.unity";

    [MenuItem("Tools/Kostka/Vytvořit scénu")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        PlayerSettings.companyName = "adamino32cz";
        PlayerSettings.productName = "Kostka";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;

        // Teplé, měkké prostředí (žádný neon).
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(1.00f, 0.95f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(1.00f, 0.86f, 0.76f);
        RenderSettings.ambientGroundColor = new Color(0.82f, 0.72f, 0.64f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.96f, 0.88f, 0.80f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.reflectionIntensity = 0.25f;

        var pivotGo = new GameObject("Pivot");
        pivotGo.transform.position = new Vector3(0f, 5.4f, 0f);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.91f, 0.84f);
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 120f;
        cam.allowMSAA = true;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var orbit = camGo.AddComponent<CameraOrbit>();
        orbit.pivot = pivotGo.transform;
        orbit.yaw = 38f;
        orbit.targetYaw = 38f;
        orbit.pitch = 42f;
        orbit.distance = 17.5f;
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.renderShadows = true;
        }

        // Slunce
        var sunGo = new GameObject("Slunce");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.84f);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.38f;
        sun.shadowBias = 0.05f;
        sun.shadowNormalBias = 0.4f;
        sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.sun = sun;

        // Výplňové světlo
        var fillGo = new GameObject("Vypln");
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(1f, 0.82f, 0.86f);
        fill.intensity = 0.32f;
        fill.shadows = LightShadows.None;
        fillGo.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

        // Jemný volume: Neutral tone, slabá viněta, žádný bloom.
        var volGo = new GameObject("Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "KostkaVolume";
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);
        var vig = profile.Add<Vignette>(true);
        vig.color.Override(new Color(0.55f, 0.35f, 0.25f));
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.45f);
        vig.rounded.Override(false);
        var adj = profile.Add<ColorAdjustments>(true);
        adj.contrast.Override(-6f);
        adj.saturation.Override(10f);
        adj.postExposure.Override(0.12f);
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(12f);
        wb.tint.Override(4f);
        vol.sharedProfile = profile;
        Directory.CreateDirectory("Assets/Settings");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/KostkaVolume.asset");

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Materiály uložené v projektu, ať se URP shadery nedostanou ze buildu pryč.
        Directory.CreateDirectory("Assets/Resources");
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (litShader == null) Debug.LogError("[Kostka] Shader URP Lit se nenašel.");
        if (unlitShader == null) Debug.LogError("[Kostka] Shader URP Unlit se nenašel.");
        var litMat = new Material(litShader);
        litMat.name = "KostkaLit";
        var unlitMat = new Material(unlitShader);
        unlitMat.name = "KostkaUnlit";
        AssetDatabase.CreateAsset(litMat, "Assets/Resources/KostkaLit.mat");
        AssetDatabase.CreateAsset(unlitMat, "Assets/Resources/KostkaUnlit.mat");
        AssetDatabase.SaveAssets();

        var hra = new GameObject("Hra");
        var gm = hra.AddComponent<GameManager>();
        hra.AddComponent<SoundFX>();
        hra.AddComponent<AutoDemo>();
        gm.fontTitleSrc = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Baloo2-Bold.ttf");
        gm.fontUiSrc = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Nunito-Bold.ttf");
        gm.litTemplate = litMat;
        gm.unlitTemplate = unlitMat;
        if (gm.fontTitleSrc == null || gm.fontUiSrc == null)
            Debug.LogWarning("[Kostka] Písmo se nenačetlo – zkontroluj Assets/Fonts.");

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Kostka] Scéna uložena: " + ScenePath);
        _ = SceneManager.GetActiveScene();
    }
}
#endif
