using System;
using System.Collections.Generic;
using System.Linq;

namespace Maiquetia.Economia
{
    [Serializable]
    public class Equipo
    {
        public string Clave;
        public string Nombre;
        public string Descripcion;
        public float PrecioUsd;
        public bool Comprado;
        public float BonoPuntos;   // multiplicador extra por acierto
    }

    /// <summary>
    /// "El buhonero del sótano 2": tienda entre jornadas. Los equipos dan
    /// ayudas reales de inspección y suben el puntaje por acierto.
    /// </summary>
    [Serializable]
    public class Tienda
    {
        public List<Equipo> Catalogo = new List<Equipo>
        {
            new Equipo {
                Clave = "lupa", Nombre = "Lupa cuentahilos",
                Descripcion = "Resalta en rojo las letras que no coinciden entre documentos.",
                PrecioUsd = 8, BonoPuntos = 0.10f },
            new Equipo {
                Clave = "uv", Nombre = "Lámpara UV china",
                Descripcion = "Detecta cédulas alteradas: el dígito cambiado brilla.",
                PrecioUsd = 14, BonoPuntos = 0.15f },
            new Equipo {
                Clave = "calendario", Nombre = "Calendario SAIME",
                Descripcion = "Calcula solo si la prórroga está vigente. Adiós matemática.",
                PrecioUsd = 10, BonoPuntos = 0.10f },
            new Equipo {
                Clave = "biometrico", Nombre = "Lector biométrico (usado)",
                Descripcion = "Compara la foto del pasaporte con la cara. A veces se cuelga.",
                PrecioUsd = 25, BonoPuntos = 0.25f },
            new Equipo {
                Clave = "sello_doble", Nombre = "Sello de doble tinta",
                Descripcion = "+20% de comisión si decides en menos de 20 segundos.",
                PrecioUsd = 18, BonoPuntos = 0.20f },
            new Equipo {
                Clave = "ventilador", Nombre = "Ventilador nuevo pa' la casa",
                Descripcion = "La familia duerme mejor: se enferman menos.",
                PrecioUsd = 12, BonoPuntos = 0f },
        };

        public bool Tiene(string clave) =>
            Catalogo.Any(e => e.Clave == clave && e.Comprado);

        public Equipo Ver(string clave) =>
            Catalogo.First(e => e.Clave == clave);

        /// <summary>Multiplicador total de puntos según lo comprado (1.0 = base).</summary>
        public float MultiplicadorPuntos() =>
            1f + Catalogo.Where(e => e.Comprado).Sum(e => e.BonoPuntos);

        /// <summary>Compra con dólares (los equipos solo se venden "en verde").</summary>
        public bool Comprar(string clave, EconomiaFamiliar eco)
        {
            var eq = Ver(clave);
            if (eq.Comprado || eco.DineroUsd < eq.PrecioUsd) return false;
            eco.DineroUsd -= eq.PrecioUsd;
            eq.Comprado = true;
            return true;
        }
    }
}
