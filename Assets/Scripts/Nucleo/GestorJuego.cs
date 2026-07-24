using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Maiquetia.Viajeros;
using Maiquetia.Reglas;
using Maiquetia.Economia;
using Maiquetia.Consecuencias;

namespace Maiquetia.Nucleo
{
    public enum FaseJuego { Amanecer, Jornada, Terremoto, Cuadre, Tienda, Noche, FinPartida }

    /// <summary>
    /// Orquestador central. Sin lógica de UI: emite eventos y la UI se
    /// suscribe. Así el núcleo se puede probar headless por completo.
    /// </summary>
    public class GestorJuego : MonoBehaviour
    {
        public static GestorJuego I { get; private set; }

        public EstadoPartida Estado { get; private set; }
        public LibroReglas Reglas { get; private set; }
        public GeneradorViajeros Generador { get; private set; }
        public ValidadorDecision Validador { get; private set; }
        public FaseJuego Fase { get; private set; } = FaseJuego.Amanecer;

        // Cola del día
        private List<Viajero> _colaDia = new List<Viajero>();
        private int _indiceViajero;
        private System.Random _rngDia;
        private List<GastoDia> _gastosHoy;

        public Viajero ViajeroActual =>
            _indiceViajero < _colaDia.Count ? _colaDia[_indiceViajero] : null;

        // ------------------------- eventos para la UI -------------------------
        public event Action<int, DefDia> AlAmanecer;              // día, definición
        public event Action<Viajero> AlLlegarViajero;
        public event Action<Viajero, Decision, ResultadoDecision> AlDecidir;
        public event Action<ResultadoSismo> AlTemblar;
        public event Action<List<GastoDia>, float, float> AlCuadre; // gastos, sueldoBs, multasBs
        public event Action<List<EventoDiferido>> AlCosecharEventos;
        public event Action<TipoFinal, string> AlTerminarPartida;

        private void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Inicializar();
        }

        public void Inicializar(int? semillaForzada = null)
        {
            Reglas = new LibroReglas();
            Reglas.Cargar();
            Generador = new GeneradorViajeros(Reglas);
            Validador = new ValidadorDecision(Reglas);
            Estado = EstadoPartida.Cargar() ?? EstadoPartida.Nueva();
            if (semillaForzada.HasValue)
            {
                Estado = EstadoPartida.Nueva();
                Estado.SemillaPartida = semillaForzada.Value;
            }
        }

        // ============================ CICLO DE DÍA ============================
        public void ComenzarDia()
        {
            var def = Reglas.Dia(Estado.DiaActual);
            Estado.AmanecerNuevoDia();
            _rngDia = new System.Random(Estado.SemillaPartida * 7919 + Estado.DiaActual);
            Generador.SembrarDia(Estado.DiaActual, Estado.SemillaPartida);

            // tasa del día (con salto si el evento lo dice)
            Estado.Economia.Tasa.NuevoDia(def.eventos.Contains("tasa_salto"));

            // cola de viajeros
            _colaDia = new List<Viajero>();
            for (int i = 0; i < def.viajeros; i++)
                _colaDia.Add(Generador.Generar(Estado.DiaActual));
            _indiceViajero = 0;

            // eventos diferidos que explotan hoy (multas de inspectoría, titulares...)
            var cosecha = Estado.Flags.Cosechar(Estado.DiaActual);
            foreach (var ev in cosecha.Where(e => e.MultaUsd > 0))
                Estado.Economia.DineroBs -= ev.MultaUsd * Estado.Economia.Tasa.BsPorDolar;
            if (cosecha.Count > 0) AlCosecharEventos?.Invoke(cosecha);

            Fase = FaseJuego.Jornada;
            AlAmanecer?.Invoke(Estado.DiaActual, def);
            AlLlegarViajero?.Invoke(ViajeroActual);
        }

        // ========================== DECISIÓN JUGADOR ==========================
        public ResultadoDecision Decidir(Decision d)
        {
            var v = ViajeroActual;
            if (v == null || Fase != FaseJuego.Jornada) return null;

            var res = Validador.Evaluar(v, d, Estado.DiaActual);
            Estado.ProcesadosHoy++;

            if (res.Correcta)
            {
                Estado.AciertosHoy++;
                Estado.PuntosHoy += 10f * Estado.Tienda.MultiplicadorPuntos();
            }
            else
            {
                Estado.ErroresHoy++;
                Estado.Flags.ContadorErroresTotales++;
            }

            if (d == Decision.Alcabala && res.Correcta) Estado.Flags.ContadorReportes++;

            // piedad: lo dejó SEGUIR sabiendo que la decisión correcta era NEGADO
            if (d == Decision.Siga && res.DecisionCorrecta == Decision.Negado)
            {
                Estado.Flags.ContadorPiedad++;
                Estado.Flags.Programar($"piedad_d{Estado.DiaActual}_{_indiceViajero}",
                    Estado.DiaActual, 3, "titular",
                    "PRENSA: siguen pasando viajeros con papeles irregulares por Maiquetía. ¿Quién revisa?");
            }

            AlDecidir?.Invoke(v, d, res);
            AvanzarCola();
            return res;
        }

        /// <summary>Aceptar el soborno del viajero actual: plata sucia + riesgo.</summary>
        public void AceptarSoborno()
        {
            var v = ViajeroActual;
            if (v == null || !v.OfreceSoborno) return;
            Estado.Economia.DineroUsd += v.MontoSobornoUsd;
            // ¿te vieron? 40% de que quede anotado
            if (_rngDia.NextDouble() < 0.4)
            {
                Estado.Flags.ContadorSobornos++;
                Estado.Flags.Programar($"matraca_d{Estado.DiaActual}", Estado.DiaActual, 2,
                    "carta", "Un sobre sin remitente: \"Te vimos. La próxima cuesta el doble... o hablamos.\"");
            }
        }

        private void AvanzarCola()
        {
            _indiceViajero++;

            // ---- ¿TERREMOTO? Se sortea al pasar la mitad de la cola ----
            if (Fase == FaseJuego.Jornada &&
                _indiceViajero == _colaDia.Count / 2 &&
                Estado.Terremoto.SorteaSismo(Estado.DiaActual, _rngDia))
            {
                var restantes = _colaDia.Skip(_indiceViajero).ToArray();
                var resultado = Estado.Terremoto.Aplicar(restantes, Estado.Flags, Estado.DiaActual);
                _indiceViajero = _colaDia.Count;   // la cola se dispersó: pasaron todos
                Fase = FaseJuego.Terremoto;
                AlTemblar?.Invoke(resultado);
                return; // la UI llama TerminarJornada() cuando pase el temblor
            }

            if (_indiceViajero >= _colaDia.Count) TerminarJornada();
            else AlLlegarViajero?.Invoke(ViajeroActual);
        }

        // ============================== CUADRE ================================
        public void TerminarJornada()
        {
            var def = Reglas.Dia(Estado.DiaActual);
            bool apagon = def.eventos.Contains("apagon");

            float sueldo = Estado.Economia.SueldoDelDia(Estado.AciertosHoy)
                         * Estado.Tienda.MultiplicadorPuntos();
            float multas = Estado.Economia.MultasDelDia(Estado.ErroresHoy);
            Estado.Economia.DineroBs += sueldo - multas;

            _gastosHoy = Estado.Economia.GastosDelDia(apagon, _rngDia);

            // evento CLAP: la comida sale gratis hoy (pero deja flag)
            if (def.eventos.Contains("clap"))
            {
                var comida = _gastosHoy.First(g => g.Concepto == "Comida");
                comida.MontoUsd = 0;
                Estado.Flags.Sembrar("acepto_clap");
            }

            Fase = FaseJuego.Cuadre;
            AlCuadre?.Invoke(_gastosHoy, sueldo, multas);
        }

        public bool PagarGasto(GastoDia g) => Estado.Economia.Pagar(g);

        /// <summary>Cambiar Bs a USD "con el pana de la 7": mejor tasa, deja flag.</summary>
        public void CambiarConElPana(float montoBs)
        {
            if (Estado.Economia.DineroBs < montoBs) return;
            Estado.Economia.DineroBs -= montoBs;
            Estado.Economia.DineroUsd += montoBs / (Estado.Economia.Tasa.BsPorDolar * 0.92f);
            Estado.Flags.Sembrar("cambio_paralelo");
        }

        // =============================== NOCHE ================================
        public void Dormir()
        {
            Estado.Economia.CerrarNoche(_gastosHoy);
            var final = GestorFinales.Evaluar(Estado.Economia, Estado.Flags, Estado.DiaActual);

            if (final != TipoFinal.Ninguno)
            {
                Estado.FinalAlcanzado = final;
                Fase = FaseJuego.FinPartida;
                Estado.Guardar();
                AlTerminarPartida?.Invoke(final, GestorFinales.Narrativa(final));
                return;
            }

            Estado.DiaActual++;
            Estado.Guardar();
            Fase = FaseJuego.Amanecer;
        }

        public void ReiniciarPartida()
        {
            EstadoPartida.Borrar();
            Estado = EstadoPartida.Nueva();
            Fase = FaseJuego.Amanecer;
        }
    }
}
