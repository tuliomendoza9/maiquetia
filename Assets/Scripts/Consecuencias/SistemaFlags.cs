using System;
using System.Collections.Generic;
using System.Linq;

namespace Maiquetia.Consecuencias
{
    /// <summary>Un evento que se dispara N días después de sembrado el flag.</summary>
    [Serializable]
    public class EventoDiferido
    {
        public string Flag;          // qué lo causó
        public int DiaDisparo;       // en qué día del juego explota
        public string TipoEvento;    // "titular", "inspector", "multa", "visita", "carta"
        public string Texto;         // lo que ve el jugador
        public float MultaUsd;       // si aplica
        public bool Consumido;
    }

    /// <summary>
    /// Memoria moral del juego: cada decisión notable deja un flag,
    /// y los flags maduran en consecuencias días después.
    /// </summary>
    [Serializable]
    public class SistemaFlags
    {
        public List<string> Flags = new List<string>();
        public List<EventoDiferido> Pendientes = new List<EventoDiferido>();

        public int ContadorSobornos;
        public int ContadorErroresTotales;
        public int ContadorPiedad;        // dejó pasar gente inválida a propósito (interrogó y aprobó)
        public int ContadorReportes;      // alcabalas correctas

        public void Sembrar(string flag) { if (!Flags.Contains(flag)) Flags.Add(flag); }
        public bool Tiene(string flag) => Flags.Contains(flag);

        public void Programar(string flag, int diaActual, int enDias,
                              string tipo, string texto, float multaUsd = 0)
        {
            Sembrar(flag);
            Pendientes.Add(new EventoDiferido
            {
                Flag = flag,
                DiaDisparo = diaActual + enDias,
                TipoEvento = tipo,
                Texto = texto,
                MultaUsd = multaUsd,
            });
        }

        /// <summary>Eventos que explotan HOY (y quedan consumidos).</summary>
        public List<EventoDiferido> Cosechar(int dia)
        {
            var hoy = Pendientes.Where(e => !e.Consumido && e.DiaDisparo == dia).ToList();
            foreach (var e in hoy) e.Consumido = true;
            return hoy;
        }
    }

    public enum TipoFinal
    {
        Ninguno,
        Bancarrota,        // sin plata dos días seguidos / desalojo
        Luto,              // 2+ familiares fallecidos
        ArrestadoCorrupto, // 3 sobornos detectados
        Despedido,         // errores acumulados
        FuncionarioEterno, // sobrevivió la campaña — final neutro
    }

    /// <summary>Evalúa al cierre de cada noche si la partida terminó.</summary>
    public static class GestorFinales
    {
        public const int MaxErrores = 12;
        public const int MaxSobornosDetectados = 3;
        public const int UltimoDia = 7;

        public static TipoFinal Evaluar(Economia.EconomiaFamiliar eco, SistemaFlags flags, int diaRecienCerrado)
        {
            if (eco.FamiliaresFallecidos() >= 2) return TipoFinal.Luto;
            if (eco.DiasSinPagarAlquiler >= 2) return TipoFinal.Bancarrota;
            if (flags.ContadorSobornos >= MaxSobornosDetectados) return TipoFinal.ArrestadoCorrupto;
            if (flags.ContadorErroresTotales >= MaxErrores) return TipoFinal.Despedido;
            if (diaRecienCerrado >= UltimoDia) return TipoFinal.FuncionarioEterno;
            return TipoFinal.Ninguno;
        }

        public static string Narrativa(TipoFinal final)
        {
            switch (final)
            {
                case TipoFinal.Bancarrota:
                    return "El dueño del rancho no esperó más. Amanecieron con los corotos en la acera. " +
                           "Yubraska no dijo nada. No hizo falta.\n\nFIN — LA CALLE";
                case TipoFinal.Luto:
                    return "El cementerio queda lejos y el pasaje está caro. Fuiste caminando.\n\nFIN — EL LUTO";
                case TipoFinal.ArrestadoCorrupto:
                    return "Los del Ministerio no tocaron la puerta: la tumbaron. Alguien llevaba la cuenta " +
                           "de cada 'colaboración'.\n\nFIN — EL MATRAQUERO";
                case TipoFinal.Despedido:
                    return "\"Por bajo rendimiento e ineptitud manifiesta...\" El sello que te despidió " +
                           "era igualito al que usabas tú.\n\nFIN — DESPEDIDO";
                case TipoFinal.FuncionarioEterno:
                    return "Sobreviviste la semana. Nadie te dio las gracias. Mañana hay más cola.\n\n" +
                           "FIN — EL FUNCIONARIO ETERNO";
                default:
                    return "";
            }
        }
    }
}
