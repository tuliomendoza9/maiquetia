using System;
using System.IO;
using UnityEngine;
using Maiquetia.Economia;
using Maiquetia.Consecuencias;

namespace Maiquetia.Nucleo
{
    /// <summary>
    /// Todo lo que persiste entre sesiones. Se serializa a JSON en
    /// Application.persistentDataPath (en WebGL: IndexedDB vía PlayerPrefs).
    /// </summary>
    [Serializable]
    public class EstadoPartida
    {
        public int SemillaPartida;
        public int DiaActual = 1;
        public EconomiaFamiliar Economia = new EconomiaFamiliar();
        public SistemaFlags Flags = new SistemaFlags();
        public Tienda Tienda = new Tienda();
        public Anuncios.GestorRemesas Remesas = new Anuncios.GestorRemesas();
        public EventoTerremoto Terremoto = new EventoTerremoto();
        public TipoFinal FinalAlcanzado = TipoFinal.Ninguno;

        // Estadísticas por día (se resetean al amanecer)
        public int ProcesadosHoy;
        public int AciertosHoy;
        public int ErroresHoy;
        public float PuntosHoy;

        private const string ClavePrefs = "maiquetia_partida";

        public static EstadoPartida Nueva()
        {
            return new EstadoPartida
            {
                SemillaPartida = Environment.TickCount & 0x7FFFFFFF,
            };
        }

        public void Guardar()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(ClavePrefs, json);  // funciona igual en WebGL
            PlayerPrefs.Save();
        }

        public static EstadoPartida Cargar()
        {
            if (!PlayerPrefs.HasKey(ClavePrefs)) return null;
            try { return JsonUtility.FromJson<EstadoPartida>(PlayerPrefs.GetString(ClavePrefs)); }
            catch { return null; }
        }

        public static void Borrar() => PlayerPrefs.DeleteKey(ClavePrefs);

        public void AmanecerNuevoDia()
        {
            ProcesadosHoy = AciertosHoy = ErroresHoy = 0;
            PuntosHoy = 0;
            Remesas.NuevoDia();
        }
    }
}
