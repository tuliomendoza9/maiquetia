using System;
using System.Linq;
using UnityEngine;
using Maiquetia.Reglas;

namespace Maiquetia.Viajeros
{
    // ----- Espejos de nombres.json y dialogos.json -----
    [Serializable]
    public class ArchivoNombres
    {
        public string[] nombresF;
        public string[] nombresM;
        public string[] apellidos;
    }

    [Serializable]
    public class GrupoExcusas { public string tipo; public string[] lineas; }

    [Serializable]
    public class ArchivoDialogos
    {
        public string[] saludos;
        public GrupoExcusas[] excusas;
        public string[] sobornos;
        public string[] despedidas_siga;
        public string[] despedidas_negado;
    }

    /// <summary>
    /// Fabrica viajeros: primero uno PERFECTO y luego, según el día,
    /// le inyecta (o no) una discrepancia. Determinista por semilla.
    /// </summary>
    public class GeneradorViajeros
    {
        private readonly LibroReglas _reglas;
        private readonly ArchivoNombres _nombres;
        private readonly ArchivoDialogos _dialogos;
        private System.Random _rng;
        public const int TotalRetratos = 64;

        public GeneradorViajeros(LibroReglas reglas)
        {
            _reglas = reglas;
            _nombres = JsonUtility.FromJson<ArchivoNombres>(LibroReglas.LeerDato("nombres.json"));
            _dialogos = JsonUtility.FromJson<ArchivoDialogos>(LibroReglas.LeerDato("dialogos.json"));
        }

        public void SembrarDia(int dia, int semillaPartida) =>
            _rng = new System.Random(semillaPartida * 1000 + dia);

        private T Al<T>(T[] arr) => arr[_rng.Next(arr.Length)];

        // ------------------------------------------------------------------
        public Viajero Generar(int dia)
        {
            var defDia = _reglas.Dia(dia);
            var hoy = _reglas.FechaDelDia(dia);
            var v = GenerarPerfecto(dia, hoy);

            // ¿Le toca discrepancia? Curva: día 1 ~45%, sube hasta ~75%
            float prob = Mathf.Min(0.45f + dia * 0.05f, 0.75f);
            if (_rng.NextDouble() < prob && defDia.discrepanciasActivas.Length > 0)
            {
                var tipo = (TipoDiscrepancia)Enum.Parse(typeof(TipoDiscrepancia),
                            Al(defDia.discrepanciasActivas));
                InyectarDiscrepancia(v, tipo, hoy, dia);
            }

            // ¿Ofrece soborno? (independiente de si es válido o no)
            if (_rng.NextDouble() < defDia.probSoborno)
            {
                v.OfreceSoborno = true;
                v.MontoSobornoUsd = 5 + _rng.Next(1, 8) * 5; // $10..$40
            }

            v.DialogoSaludo = Al(_dialogos.saludos);
            v.DialogoExcusa = ExcusaPara(v.Discrepancia);
            return v;
        }

        // ------------------------------------------------------------------
        private Viajero GenerarPerfecto(int dia, FechaJuego hoy)
        {
            bool esF = _rng.NextDouble() < 0.5;
            string nombre = esF ? Al(_nombres.nombresF) : Al(_nombres.nombresM);
            string nombreCompleto = $"{nombre} {Al(_nombres.apellidos)}";
            int edad = PesoEdad();
            string cedula = $"V-{_rng.Next(4, 31)}.{_rng.Next(100, 999)}.{_rng.Next(100, 999)}";

            var destino = Al(_reglas.Destinos.destinos);
            var v = new Viajero
            {
                NombreReal = nombreCompleto,
                CedulaReal = cedula,
                Sexo = esF ? "F" : "M",
                Edad = edad,
                RetratoId = _rng.Next(TotalRetratos),
                Discrepancia = TipoDiscrepancia.Ninguna,
            };

            // Pasaporte vigente (a veces vencido pero con prórroga VÁLIDA: trampa buena)
            bool usaProrroga = _rng.NextDouble() < 0.30;
            var vence = usaProrroga ? hoy.MasDias(-_rng.Next(30, 400))   // ya venció...
                                    : hoy.MasDias(_rng.Next(60, 1500));  // vigente normal
            v.Pasaporte = new Pasaporte
            {
                NombreCompleto = nombreCompleto,
                Cedula = cedula,
                Sexo = v.Sexo,
                FechaNacimiento = hoy.MasDias(-(edad * 365 + _rng.Next(0, 300))),
                FechaVencimiento = vence,
                TieneProrroga = usaProrroga,
                ProrrogaHasta = usaProrroga ? vence.MasDias(730) : default, // decreto: +2 años
                RetratoId = v.RetratoId,
                FotoEsVariante = false,
            };
            // Garantía: si usa prórroga, que esté VIGENTE en el caso perfecto
            if (usaProrroga && !v.Pasaporte.ProrrogaHasta.EsPosteriorOIgual(hoy))
                v.Pasaporte.FechaVencimiento = hoy.MasDias(_rng.Next(60, 1500));

            v.Boleto = new Boleto
            {
                NombreCompleto = nombreCompleto,
                Destino = destino.clave,
                Aerolinea = Al(_reglas.Destinos.aerolineas),
                FechaVuelo = hoy,
                TasaPagada = true,
            };

            v.Extras = new DocumentosExtra
            {
                TraeVisa = true,                 // si el destino la pide, la trae
                TraeCertFiebreAmarilla = true,
                TraePermisoMenor = true,
                CedulaEnBoleto = cedula,
            };

            // Un adulto acompañando NO aplica; los menores viajan "solos" en el MVP
            if (v.EsMenor) v.Edad = Mathf.Max(v.Edad, 10); // menores de 10 no viajan solos ni en el juego
            return v;
        }

        /// <summary>Edades con distribución realista de la diáspora: mucho joven adulto.</summary>
        private int PesoEdad()
        {
            double r = _rng.NextDouble();
            if (r < 0.12) return _rng.Next(10, 18);   // menores
            if (r < 0.62) return _rng.Next(18, 36);   // el grueso
            if (r < 0.88) return _rng.Next(36, 60);
            return _rng.Next(60, 82);                 // abuelos que van a conocer nietos
        }

        // ------------------------------------------------------------------
        private void InyectarDiscrepancia(Viajero v, TipoDiscrepancia tipo, FechaJuego hoy, int dia)
        {
            v.Discrepancia = tipo;
            switch (tipo)
            {
                case TipoDiscrepancia.NombreNoCoincide:
                    v.Boleto.NombreCompleto = Typo(v.NombreReal);
                    break;

                case TipoDiscrepancia.PasaporteVencido:
                    v.Pasaporte.FechaVencimiento = hoy.MasDias(-_rng.Next(10, 900));
                    v.Pasaporte.TieneProrroga = false;
                    break;

                case TipoDiscrepancia.ProrrogaVencida:
                    // Venció hace más de 2 años => la prórroga (+730d) también venció
                    v.Pasaporte.FechaVencimiento = hoy.MasDias(-_rng.Next(760, 1400));
                    v.Pasaporte.TieneProrroga = true;
                    v.Pasaporte.ProrrogaHasta = v.Pasaporte.FechaVencimiento.MasDias(730);
                    break;

                case TipoDiscrepancia.FotoNoCoincide:
                    v.Pasaporte.FotoEsVariante = true;
                    break;

                case TipoDiscrepancia.CedulaAlterada:
                    v.Extras.CedulaEnBoleto = TypoDigito(v.CedulaReal);
                    break;

                case TipoDiscrepancia.SinVisaRequerida:
                    v.Boleto.Destino = Al(_reglas.Destinos.destinos
                        .Where(d => d.exigeVisa).ToArray()).clave;
                    v.Extras.TraeVisa = false;
                    break;

                case TipoDiscrepancia.MenorSinPermiso:
                    v.Edad = _rng.Next(10, 18);
                    v.Extras.TraePermisoMenor = false;
                    break;

                case TipoDiscrepancia.BoletoOtraFecha:
                    v.Boleto.FechaVuelo = hoy.MasDias(_rng.Next(1, 15) * (_rng.NextDouble() < 0.5 ? 1 : -1));
                    break;

                case TipoDiscrepancia.EnListaAlerta:
                    var alerta = _reglas.Dia(dia).listaAlerta;
                    if (alerta.Length > 0)
                    {
                        string nuevoNombre = $"{v.NombreReal.Split(' ')[0]} {Al(alerta)}";
                        v.NombreReal = nuevoNombre;
                        v.Pasaporte.NombreCompleto = nuevoNombre;
                        v.Boleto.NombreCompleto = nuevoNombre;
                    }
                    break;

                case TipoDiscrepancia.SinCertFiebre:
                    v.Boleto.Destino = Al(_reglas.Destinos.destinos
                        .Where(d => d.exigeFiebreAmarilla).ToArray()).clave;
                    v.Extras.TraeCertFiebreAmarilla = false;
                    break;
            }
        }

        /// <summary>Cambia una letra del nombre (típico error "de agencia").</summary>
        private string Typo(string nombre)
        {
            var chars = nombre.ToCharArray();
            int i;
            do { i = _rng.Next(chars.Length); } while (!char.IsLetter(chars[i]));
            chars[i] = chars[i] == 'a' ? 'e' : chars[i] == 'o' ? 'a' :
                       chars[i] == 'e' ? 'i' : chars[i] == 'n' ? 'm' : 'a';
            return new string(chars);
        }

        private string TypoDigito(string cedula)
        {
            var chars = cedula.ToCharArray();
            int i;
            do { i = _rng.Next(chars.Length); } while (!char.IsDigit(chars[i]));
            chars[i] = (char)('0' + (chars[i] - '0' + 1 + _rng.Next(8)) % 10);
            return new string(chars);
        }

        private string ExcusaPara(TipoDiscrepancia tipo)
        {
            if (tipo == TipoDiscrepancia.Ninguna)
                return "¿Algún problema? Yo... yo tengo todo en regla, señor.";
            var grupo = _dialogos.excusas.FirstOrDefault(g => g.tipo == tipo.ToString());
            return grupo != null ? Al(grupo.lineas) : "Eso debe ser un error del sistema...";
        }

        public string Despedida(bool paso) =>
            paso ? Al(_dialogos.despedidas_siga) : Al(_dialogos.despedidas_negado);

        public string FraseSoborno() => Al(_dialogos.sobornos);
    }
}
