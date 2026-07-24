using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Maiquetia.Viajeros;

namespace Maiquetia.Reglas
{
    // ----- Clases espejo de dias.json (JsonUtility) -----
    [Serializable]
    public class DefDia
    {
        public int numero;
        public string tipo;                    // "salida" | "entrada"
        public string titular;
        public int viajeros;
        public string[] discrepanciasActivas;
        public string[] reglasNuevas;
        public string[] listaAlerta;
        public string[] eventos;
        public float probSoborno;
        public string encuentroEspecial;
    }

    [Serializable]
    public class DefFecha { public int dia, mes, anio; }

    [Serializable]
    public class ArchivoDias
    {
        public DefFecha fechaInicio;
        public DefDia[] dias;
    }

    [Serializable]
    public class DefDestino
    {
        public string clave, ciudad, pais;
        public bool exigeVisa, exigeFiebreAmarilla;
    }

    [Serializable]
    public class ArchivoDestinos
    {
        public DefDestino[] destinos;
        public string[] aerolineas;
    }

    /// <summary>
    /// Carga y expone las reglas del día: qué discrepancias existen, qué
    /// destinos exigen qué, la lista de alerta y los eventos programados.
    /// </summary>
    public class LibroReglas
    {
        public ArchivoDias Campania { get; private set; }
        public ArchivoDestinos Destinos { get; private set; }
        public FechaJuego FechaInicio { get; private set; }

        public void Cargar()
        {
            Campania = JsonUtility.FromJson<ArchivoDias>(LeerDato("dias.json"));
            Destinos = JsonUtility.FromJson<ArchivoDestinos>(LeerDato("destinos.json"));
            FechaInicio = new FechaJuego(Campania.fechaInicio.dia,
                                         Campania.fechaInicio.mes,
                                         Campania.fechaInicio.anio);
        }

        public static string LeerDato(string archivo)
        {
            // Los datos viven en Resources/Datos como TextAssets: lectura
            // síncrona idéntica en editor, Windows y WebGL (teléfono).
            string clave = "Datos/" + Path.GetFileNameWithoutExtension(archivo);
            var ta = Resources.Load<TextAsset>(clave);
            if (ta == null) throw new FileNotFoundException($"Recurso no encontrado: {clave}");
            return ta.text;
        }

        public DefDia Dia(int numero) =>
            Campania.dias.FirstOrDefault(d => d.numero == numero) ?? Campania.dias.Last();

        public FechaJuego FechaDelDia(int numero) => FechaInicio.MasDias(numero - 1);

        public DefDestino Destino(string clave) =>
            Destinos.destinos.First(d => d.clave == clave);

        public bool DiscrepanciaActiva(int dia, TipoDiscrepancia t) =>
            Dia(dia).discrepanciasActivas.Contains(t.ToString());

        /// <summary>La fiebre amarilla solo se exige a partir del día 6 (regla nueva).</summary>
        public bool ExigeFiebre(int dia, string claveDestino) =>
            DiscrepanciaActiva(dia, TipoDiscrepancia.SinCertFiebre) &&
            Destino(claveDestino).exigeFiebreAmarilla;

        /// <summary>Las visas se exigen a partir del día 3 (regla nueva).</summary>
        public bool ExigeVisa(int dia, string claveDestino) =>
            DiscrepanciaActiva(dia, TipoDiscrepancia.SinVisaRequerida) &&
            Destino(claveDestino).exigeVisa;

        public bool EnListaAlerta(int dia, string nombreCompleto) =>
            Dia(dia).listaAlerta.Any(a => nombreCompleto.Contains(a));
    }
}
