using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Fabrica la escena Principal.unity completa: cámara, EventSystem,
/// GestorJuego + ConstructorUI + PuenteTest. La registra en Build Settings.
/// Correr con: -executeMethod CrearEscena.Ejecutar
/// </summary>
public static class CrearEscena
{
    public static void Ejecutar()
    {
        try
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Camara");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.145f, 0.188f, 0.169f);
            cam.orthographic = true;

            var ev = new GameObject("EventSystem");
            ev.AddComponent<EventSystem>();
            ev.AddComponent<StandaloneInputModule>();

            var juego = new GameObject("Juego");
            juego.AddComponent<Maiquetia.Nucleo.GestorJuego>();
            juego.AddComponent<Maiquetia.UI.ConstructorUI>();

            var puente = new GameObject("__mq");
            puente.AddComponent<PuenteTest>();

            System.IO.Directory.CreateDirectory("Assets/Escenas");
            bool ok = EditorSceneManager.SaveScene(escena, "Assets/Escenas/Principal.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Escenas/Principal.unity", true),
            };

            PlayerSettings.productName = "Maiquetía: Punto de Control";
            PlayerSettings.companyName = "Revivir Yicsi Games";

            if (ok) { Debug.Log("ESCENA_OK Assets/Escenas/Principal.unity"); EditorApplication.Exit(0); }
            else { Debug.LogError("ESCENA_FALLO no se pudo guardar"); EditorApplication.Exit(2); }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("ESCENA_EXCEPCION " + ex);
            EditorApplication.Exit(3);
        }
    }
}
