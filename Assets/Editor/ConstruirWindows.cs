using UnityEditor;
using UnityEngine;

/// <summary>Build Windows standalone por CLI: -executeMethod ConstruirWindows.Ejecutar</summary>
public static class ConstruirWindows
{
    public static void Ejecutar()
    {
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Escenas/Principal.unity" },
            locationPathName = "Builds/Windows/maiquetia.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"BUILD_OK {reporte.summary.totalSize / (1024 * 1024)} MB");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"BUILD_FALLO {reporte.summary.result}");
            EditorApplication.Exit(2);
        }
    }
}
