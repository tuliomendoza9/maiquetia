using UnityEditor;
using UnityEngine;

/// <summary>
/// Build WebGL para teléfono + GitHub Pages:
///   -executeMethod ConstruirWebGL.Ejecutar
/// Sale directo a docs/ (la carpeta que GitHub Pages sirve).
/// </summary>
public static class ConstruirWebGL
{
    public static void Ejecutar()
    {
        // GitHub Pages no manda Content-Encoding: usar Brotli + fallback JS
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;

        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Escenas/Principal.unity" },
            locationPathName = "docs",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"WEBGL_OK {reporte.summary.totalSize / (1024 * 1024)} MB");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"WEBGL_FALLO {reporte.summary.result}");
            EditorApplication.Exit(2);
        }
    }
}
