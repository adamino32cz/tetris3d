#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Pomocné příkazy pro spouštění z příkazové řádky (-batchmode -executeMethod ...).
public static class TetrisBatch
{
    const string Scene = "Assets/Scenes/Kostka.unity";

    public static void BuildScene() => TetrisSceneSetup.BuildScene();

    public static void BuildLinux()
    {
        PlayerSettings.runInBackground = true;
        var r = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/Linux/Kostka.x86_64",
            BuildTarget.StandaloneLinux64, BuildOptions.None);
        Debug.Log("[Kostka] Linux build: " + r.summary.result);
        if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void BuildWebGL()
    {
        PlayerSettings.runInBackground = true;
        // GitHub Pages neumí hlavičky pro .gz/.br, proto bez komprese
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        var r = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/WebGL",
            BuildTarget.WebGL, BuildOptions.None);
        Debug.Log("[Kostka] WebGL build: " + r.summary.result);
        if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    // Jedno spuštění editoru: scéna + Linux přehrávač.
    public static void BuildSceneAndLinux()
    {
        TetrisSceneSetup.BuildScene();
        BuildLinux();
    }

    // Jedno spuštění editoru: scéna + WebGL.
    public static void BuildSceneAndWebGL()
    {
        TetrisSceneSetup.BuildScene();
        BuildWebGL();
    }
}
#endif
