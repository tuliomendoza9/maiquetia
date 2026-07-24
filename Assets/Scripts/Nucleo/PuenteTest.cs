using UnityEngine;
using Maiquetia.Viajeros;
using Maiquetia.Nucleo;

/// <summary>
/// Hooks de prueba para WebGL/Browser: desde JS se llama
///   unityInstance.SendMessage('__mq', 'Metodo', arg)
/// y el estado se lee por console.log (prefijo __MQ__).
/// El GameObject se llama "__mq" — igual filosofía que __mc/__zm.
/// </summary>
public class PuenteTest : MonoBehaviour
{
    private GestorJuego G => GestorJuego.I;

    private void Awake() { gameObject.name = "__mq"; }

#if !UNITY_WEBGL
    private void Start()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autotest") >= 0)
            StartCoroutine(Autopiloto());
    }

    /// <summary>
    /// Juega solo apretando los botones REALES de la UI y capturando pantalla:
    /// verifica el juego completo sin editor ni navegador.
    /// </summary>
    private System.Collections.IEnumerator Autopiloto()
    {
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "capturas");
        System.IO.Directory.CreateDirectory(dir);
        var rng = new System.Random(123);
        int foto = 0;
        System.Action<string> cap = nombre =>
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, $"{foto++:00}_{nombre}.png"));

        yield return new WaitForSeconds(2f);
        cap("amanecer"); yield return null; yield return null;

        for (int dia = 0; dia < 3 && G.Fase != FaseJuego.FinPartida; dia++)
        {
            ClickBoton("COMENZAR JORNADA");
            yield return new WaitForSeconds(0.4f);
            if (dia == 0) { cap("jornada_viajero"); yield return null; yield return null; }

            int viajeros = 0;
            while (G.Fase == FaseJuego.Jornada && viajeros++ < 30)
            {
                if (dia == 0 && viajeros == 2)
                {
                    ClickBoton("INTERROGAR");
                    yield return null;
                    cap("interrogatorio"); yield return null; yield return null;
                }
                double r = rng.NextDouble();
                ClickBoton(r < 0.5 ? "SIGA" : r < 0.85 ? "NEGADO ×" : "¡ALCABALA!");
                yield return new WaitForSeconds(0.15f);
            }

            if (G.Fase == FaseJuego.Terremoto)
            {
                cap("terremoto"); yield return null; yield return null;
                ClickBoton("SALIR DEL ESCOMBRO >>");
                yield return new WaitForSeconds(0.3f);
            }

            if (dia == 0)
            {
                ClickBoton("LIBRO DE\nREGLAS"); yield return null;
                cap("libro_reglas"); yield return null; yield return null;
                ClickBoton("CERRAR"); yield return null;
            }

            // cuadre: pagar todo lo pagable
            cap($"cuadre_dia{dia + 1}"); yield return null; yield return null;
            foreach (var btn in GameObject.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (btn.name == "Btn_PAGAR" && btn.gameObject.activeInHierarchy)
                { btn.onClick.Invoke(); yield return null; }

            if (dia == 0)
            {
                ClickBoton("TIENDA DEL BUHONERO"); yield return null;
                cap("tienda"); yield return null; yield return null;
                ClickBoton("CERRAR"); yield return null;
            }

            ClickBoton("DORMIR >>");
            yield return new WaitForSeconds(0.4f);
        }

        cap("final_o_dia4"); yield return null; yield return null;
        Debug.Log($"AUTOTEST_OK dias_jugados fase={G.Fase} dia={G.Estado.DiaActual} " +
                  $"bs={G.Estado.Economia.DineroBs:0} capturas={foto}");
        yield return new WaitForSeconds(0.5f);
        Application.Quit(0);
    }

    private void ClickBoton(string etiqueta)
    {
        foreach (var btn in GameObject.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            if (btn.name == "Btn_" + etiqueta && btn.gameObject.activeInHierarchy)
            { btn.onClick.Invoke(); return; }
        Debug.LogWarning($"AUTOTEST boton no encontrado: {etiqueta}");
    }
#endif

    public void Estado(string _ = "")
    {
        var e = G.Estado;
        Debug.Log("__MQ__ " + JsonUtility.ToJson(new Resumen
        {
            dia = e.DiaActual,
            fase = G.Fase.ToString(),
            bs = e.Economia.DineroBs,
            usd = e.Economia.DineroUsd,
            tasa = e.Economia.Tasa.BsPorDolar,
            aciertos = e.AciertosHoy,
            errores = e.ErroresHoy,
            puntos = e.PuntosHoy,
            viajero = G.ViajeroActual?.NombreReal ?? "",
            discrepancia = G.ViajeroActual?.Discrepancia.ToString() ?? "",
            final = e.FinalAlcanzado.ToString(),
        }));
    }

    public void Comenzar(string _ = "") => G.ComenzarDia();
    public void DecidirSiga(string _ = "") => G.Decidir(Decision.Siga);
    public void DecidirNegado(string _ = "") => G.Decidir(Decision.Negado);
    public void DecidirAlcabala(string _ = "") => G.Decidir(Decision.Alcabala);
    public void DecidirCorrecto(string _ = "")
    {
        if (G.ViajeroActual != null)
            G.Decidir(G.Validador.Correcta(G.ViajeroActual, G.Estado.DiaActual));
    }
    public void Dormir(string _ = "") => G.Dormir();
    public void Reiniciar(string _ = "") => G.ReiniciarPartida();

    [System.Serializable]
    private class Resumen
    {
        public int dia; public string fase;
        public float bs, usd, tasa, puntos;
        public int aciertos, errores;
        public string viajero, discrepancia, final;
    }
}
