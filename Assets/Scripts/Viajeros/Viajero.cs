using System;
using System.Collections.Generic;

namespace Maiquetia.Viajeros
{
    /// <summary>Tipos de discrepancia que el generador puede inyectar.</summary>
    public enum TipoDiscrepancia
    {
        Ninguna,            // documentos perfectos (puede estar nervioso igual)
        NombreNoCoincide,   // typo entre pasaporte y boleto
        PasaporteVencido,   // vencido y SIN prórroga válida
        ProrrogaVencida,    // la prórroga existe pero ya venció (sumar fechas)
        FotoNoCoincide,     // el retrato del pasaporte es la variante _alt
        CedulaAlterada,     // un dígito cambiado entre documentos
        SinVisaRequerida,   // el destino exige visa y no la trae
        MenorSinPermiso,    // menor de edad sin permiso notariado
        BoletoOtraFecha,    // el vuelo no es hoy
        EnListaAlerta,      // su nombre está en la lista del día
        SinCertFiebre,      // destino exige fiebre amarilla y no lo trae
    }

    /// <summary>Decisiones que puede tomar el jugador.</summary>
    public enum Decision { Siga, Negado, Alcabala }

    [Serializable]
    public class Pasaporte
    {
        public string NombreCompleto;
        public string Cedula;           // "V-12.345.678"
        public string Sexo;             // "F" / "M"
        public FechaJuego FechaNacimiento;
        public FechaJuego FechaVencimiento;
        public bool TieneProrroga;      // la famosa prórroga por decreto
        public FechaJuego ProrrogaHasta;
        public int RetratoId;           // índice del PNG renderizado en Blender
        public bool FotoEsVariante;     // true => usa retrato_NNN_alt (discrepancia)

        /// <summary>¿El pasaporte permite viajar HOY según sus fechas?</summary>
        public bool VigenteEn(FechaJuego hoy)
        {
            if (FechaVencimiento.EsPosteriorOIgual(hoy)) return true;
            return TieneProrroga && ProrrogaHasta.EsPosteriorOIgual(hoy);
        }
    }

    [Serializable]
    public class Boleto
    {
        public string NombreCompleto;   // puede tener typo inyectado
        public string Destino;          // clave de destinos.json
        public string Aerolinea;
        public FechaJuego FechaVuelo;
        public bool TasaPagada;         // sello de la tasa aeroportuaria
    }

    [Serializable]
    public class DocumentosExtra
    {
        public bool TraeVisa;
        public bool TraeCertFiebreAmarilla;
        public bool TraePermisoMenor;   // solo relevante si es menor
        public string CedulaEnBoleto;   // para discrepancia de cédula
    }

    /// <summary>
    /// Fecha simple del mundo del juego (sin DateTime para control total
    /// y serialización limpia). El juego arranca el 15/03/2019 ficticio.
    /// </summary>
    [Serializable]
    public struct FechaJuego : IEquatable<FechaJuego>
    {
        public int Dia, Mes, Anio;

        public FechaJuego(int dia, int mes, int anio) { Dia = dia; Mes = mes; Anio = anio; }

        public int ComoNumero() => Anio * 10000 + Mes * 100 + Dia;
        public bool EsPosteriorOIgual(FechaJuego otra) => ComoNumero() >= otra.ComoNumero();
        public bool Equals(FechaJuego otra) => ComoNumero() == otra.ComoNumero();
        public override string ToString() => $"{Dia:00}/{Mes:00}/{Anio}";

        private static readonly int[] DiasMes = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public FechaJuego MasDias(int dias)
        {
            int d = Dia + dias, m = Mes, a = Anio;
            while (d > DiasMes[m - 1]) { d -= DiasMes[m - 1]; m++; if (m > 12) { m = 1; a++; } }
            while (d < 1) { m--; if (m < 1) { m = 12; a--; } d += DiasMes[m - 1]; }
            return new FechaJuego(d, m, a);
        }
    }

    /// <summary>Un viajero en la cola, con toda su verdad y sus papeles.</summary>
    [Serializable]
    public class Viajero
    {
        // La VERDAD del viajero (lo que ES)
        public string NombreReal;
        public string CedulaReal;
        public string Sexo;
        public int Edad;
        public int RetratoId;
        public bool EsMenor => Edad < 18;

        // Sus papeles (lo que PRESENTA — puede mentir)
        public Pasaporte Pasaporte;
        public Boleto Boleto;
        public DocumentosExtra Extras;

        // Metadatos de juego
        public TipoDiscrepancia Discrepancia;
        public string DialogoSaludo;
        public string DialogoExcusa;    // qué dice si lo confrontas
        public bool OfreceSoborno;
        public int MontoSobornoUsd;
        public string EncuentroEspecialId; // null = viajero normal

        /// <summary>La decisión CORRECTA según reglas del día. La calcula el validador.</summary>
        public Decision DecisionCorrecta;
    }
}
