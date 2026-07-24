using System;
using Maiquetia.Viajeros;

namespace Maiquetia.Consecuencias
{
    /// <summary>
    /// MODO TERREMOTO: evento raro de intervalo largo. Cuando ocurre,
    /// suena la alarma, tiembla la pantalla, la cola se dispersa y TODOS
    /// los viajeros restantes del día pasan sin inspección.
    /// El jugador no cobra por esos viajeros — y si alguien de la lista de
    /// alerta se coló en el caos, habrá consecuencias en días siguientes.
    /// </summary>
    [Serializable]
    public class EventoTerremoto
    {
        public int UltimoDiaConSismo = -99;
        public const int MinDiasEntreSismos = 3;     // intervalo largo garantizado
        public const double ProbabilidadPorDia = 0.15;

        /// <summary>¿Tiembla hoy? Se sortea una sola vez por día, a mitad de jornada.</summary>
        public bool SorteaSismo(int dia, Random rng)
        {
            if (dia - UltimoDiaConSismo < MinDiasEntreSismos) return false;
            if (dia <= 2) return false;               // días de tutorial protegidos
            if (rng.NextDouble() >= ProbabilidadPorDia) return false;
            UltimoDiaConSismo = dia;
            return true;
        }

        /// <summary>
        /// Aplica el caos: marca a los viajeros restantes como "pasaron en el sismo"
        /// y devuelve cuántos colados peligrosos había entre ellos.
        /// </summary>
        public ResultadoSismo Aplicar(Viajero[] restantes, SistemaFlags flags, int dia)
        {
            int peligrosos = 0;
            foreach (var v in restantes)
                if (v.Discrepancia == TipoDiscrepancia.EnListaAlerta ||
                    v.Discrepancia == TipoDiscrepancia.CedulaAlterada)
                    peligrosos++;

            if (peligrosos > 0)
                flags.Programar("sismo_colados", dia, 2, "inspector",
                    $"INSPECTORÍA: durante el sismo del día {dia} se colaron {peligrosos} " +
                    "persona(s) con documentación irregular. Se le descuenta la responsabilidad.",
                    multaUsd: peligrosos * 4f);
            else
                flags.Programar("sismo_limpio", dia, 1, "titular",
                    "MILAGRO EN MAIQUETÍA: el sismo no dejó heridos. El aeropuerto reanuda operaciones.");

            return new ResultadoSismo { PasaronSinControl = restantes.Length, Peligrosos = peligrosos };
        }
    }

    [Serializable]
    public class ResultadoSismo
    {
        public int PasaronSinControl;
        public int Peligrosos;
    }
}
