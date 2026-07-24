using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Maiquetia.Nucleo;
using Maiquetia.Viajeros;
using Maiquetia.Reglas;
using Maiquetia.Economia;
using Maiquetia.Consecuencias;

namespace Maiquetia.UI
{
    /// <summary>
    /// Construye TODA la interfaz en runtime (sin prefabs ni escenas dibujadas
    /// a mano) y la conecta a los eventos del GestorJuego.
    /// </summary>
    public class ConstructorUI : MonoBehaviour
    {
        // ------- paleta "tropical desgastado" -------
        static readonly Color FondoOscuro = Hex("#25302B");
        static readonly Color Madera      = Hex("#6E5233");
        static readonly Color Papel       = Hex("#EDE4CE");
        static readonly Color PapelViejo  = Hex("#DFD3B8");
        static readonly Color AzulSaime   = Hex("#1F4E79");
        static readonly Color VerdeSello  = Hex("#2E7D32");
        static readonly Color RojoSello   = Hex("#B03A2E");
        static readonly Color Amarillo    = Hex("#C9A227");
        static readonly Color Naranja     = Hex("#E8842C");
        static readonly Color TintaOscura = Hex("#2B2620");

        private GestorJuego G => GestorJuego.I;
        private Font _fuente;
        private Canvas _canvas;
        private RectTransform _raiz;

        // referencias vivas
        private Image _retrato, _fotoPasaporte;
        private Text _globo, _barraInfo, _titularTexto, _reglasTexto, _feedback;
        private RectTransform _panelPasaporte, _panelBoleto, _panelExtras;
        private GameObject _panelAmanecer, _panelReglas, _panelCuadre, _panelTienda,
                           _panelFin, _panelTerremoto, _panelRemesa, _botonSoborno;
        private Text _cuadreTexto, _finTexto, _remesaTexto;
        private RectTransform _listaGastos, _listaTienda;
        private Image _barraRemesa;
        private readonly List<Text> _camposPasaporte = new List<Text>();
        private readonly List<Text> _camposBoleto = new List<Text>();
        private readonly List<Text> _camposExtras = new List<Text>();

        private void Start()
        {
            _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConstruirTodo();
            Suscribir();
            MostrarAmanecer();
        }

        // ================================================================
        //  CONSTRUCCIÓN
        // ================================================================
        private void ConstruirTodo()
        {
            var goCanvas = new GameObject("CanvasJuego");
            goCanvas.transform.SetParent(transform, false);
            _canvas = goCanvas.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var esc = goCanvas.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1280, 720);
            esc.matchWidthOrHeight = 0.5f; // teléfonos: balancear ancho/alto
            goCanvas.AddComponent<GraphicRaycaster>();
            _raiz = goCanvas.GetComponent<RectTransform>();

            Panel(_raiz, "Fondo", FondoOscuro, Vector2.zero, Vector2.one);

            // ---- barra superior ----
            var barra = Panel(_raiz, "BarraSup", TintaOscura, new Vector2(0, 0.925f), Vector2.one);
            _barraInfo = Texto(barra, "Info", "", 20, Papel, TextAnchor.MiddleCenter);
            Estirar(_barraInfo.rectTransform);

            // ---- ventanilla (izquierda) ----
            var vent = Panel(_raiz, "Ventanilla", Hex("#37453E"),
                             new Vector2(0.015f, 0.16f), new Vector2(0.36f, 0.915f));
            var marcoRetrato = Panel(vent, "Marco", TintaOscura,
                             new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.97f));
            _retrato = ImagenNueva(marcoRetrato, "Retrato");
            EstirarConMargen(_retrato.rectTransform, 6);

            var globoPanel = Panel(vent, "Globo", Papel,
                             new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.40f));
            _globo = Texto(globoPanel, "TxtGlobo", "...", 16, TintaOscura, TextAnchor.UpperLeft);
            EstirarConMargen(_globo.rectTransform, 10);

            Boton(vent, "INTERROGAR", Naranja, new Vector2(0.05f, 0.01f), new Vector2(0.48f, 0.11f),
                  () => { var v = G.ViajeroActual; if (v != null) _globo.text = v.DialogoExcusa; });
            _botonSoborno = Boton(vent, "ACEPTAR \"COLABORACIÓN\"", RojoSello,
                  new Vector2(0.52f, 0.01f), new Vector2(0.95f, 0.11f),
                  () => { G.AceptarSoborno(); _botonSoborno.SetActive(false);
                          _globo.text = "\"Un placer hacer negocios, jefe.\""; });

            // ---- mesa de documentos (derecha) ----
            var mesa = Panel(_raiz, "Mesa", Madera,
                             new Vector2(0.37f, 0.16f), new Vector2(0.985f, 0.915f));

            _panelPasaporte = DocumentoBase(mesa, "PASAPORTE — REPÚBLICA BOLIVARIANA",
                              AzulSaime, new Vector2(0.03f, 0.52f), new Vector2(0.60f, 0.97f));
            var marcoFoto = Panel(_panelPasaporte, "MarcoFoto", PapelViejo,
                              new Vector2(0.04f, 0.15f), new Vector2(0.30f, 0.75f));
            _fotoPasaporte = ImagenNueva(marcoFoto, "Foto");
            EstirarConMargen(_fotoPasaporte.rectTransform, 4);
            CamposDe(_panelPasaporte, _camposPasaporte, 6, xIni: 0.33f);

            _panelBoleto = DocumentoBase(mesa, "PASE DE ABORDAR",
                              Naranja, new Vector2(0.62f, 0.52f), new Vector2(0.97f, 0.97f));
            CamposDe(_panelBoleto, _camposBoleto, 5, xIni: 0.06f);

            _panelExtras = DocumentoBase(mesa, "DOCUMENTOS ANEXOS",
                              Hex("#5B7553"), new Vector2(0.03f, 0.03f), new Vector2(0.60f, 0.48f));
            CamposDe(_panelExtras, _camposExtras, 4, xIni: 0.06f);

            // feedback de decisión
            var fb = Panel(mesa, "Feedback", TintaOscura,
                           new Vector2(0.62f, 0.03f), new Vector2(0.97f, 0.48f));
            _feedback = Texto(fb, "TxtFeedback", "Esperando al primer viajero...",
                              15, Papel, TextAnchor.UpperLeft);
            EstirarConMargen(_feedback.rectTransform, 10);

            // ---- barra de decisión (abajo) ----
            var bd = Panel(_raiz, "BarraDecision", TintaOscura, Vector2.zero, new Vector2(1f, 0.15f));
            Boton(bd, "LIBRO DE\nREGLAS", PapelViejo, new Vector2(0.015f, 0.12f), new Vector2(0.135f, 0.88f),
                  () => _panelReglas.SetActive(!_panelReglas.activeSelf), TintaOscura);
            Boton(bd, "SIGA", VerdeSello, new Vector2(0.155f, 0.12f), new Vector2(0.36f, 0.88f),
                  () => Decidir(Decision.Siga));
            Boton(bd, "NEGADO ×", RojoSello, new Vector2(0.38f, 0.12f), new Vector2(0.585f, 0.88f),
                  () => Decidir(Decision.Negado));
            Boton(bd, "¡ALCABALA!", Amarillo, new Vector2(0.605f, 0.12f), new Vector2(0.775f, 0.88f),
                  () => Decidir(Decision.Alcabala), TintaOscura);
            Boton(bd, "REMESA $", AzulSaime, new Vector2(0.795f, 0.12f), new Vector2(0.985f, 0.88f),
                  PedirRemesa);

            // ---- overlays ----
            ConstruirPanelAmanecer();
            ConstruirPanelReglas();
            ConstruirPanelCuadre();
            ConstruirPanelTienda();
            ConstruirPanelTerremoto();
            ConstruirPanelRemesa();
            ConstruirPanelFin();
        }

        private RectTransform DocumentoBase(RectTransform padre, string titulo, Color colorSello,
                                            Vector2 min, Vector2 max)
        {
            var doc = Panel(padre, titulo, Papel, min, max);
            var cab = Panel(doc, "Cab", colorSello, new Vector2(0, 0.86f), Vector2.one);
            var t = Texto(cab, "Titulo", titulo, 13, Papel, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            Estirar(t.rectTransform);
            return doc;
        }

        private void CamposDe(RectTransform doc, List<Text> destino, int cantidad, float xIni)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float alto = 0.82f / cantidad;
                float yMax = 0.84f - i * alto;
                var txt = Texto(doc, $"Campo{i}", "", 14, TintaOscura, TextAnchor.MiddleLeft);
                Anclar(txt.rectTransform, new Vector2(xIni, yMax - alto + 0.01f), new Vector2(0.97f, yMax));
                destino.Add(txt);
            }
        }

        private void ConstruirPanelAmanecer()
        {
            _panelAmanecer = Overlay("Amanecer", Hex("#1A2420EE"));
            var caja = Panel(_panelAmanecer.GetComponent<RectTransform>(), "Caja", Papel,
                             new Vector2(0.14f, 0.18f), new Vector2(0.86f, 0.82f));
            _titularTexto = Texto(caja, "Titular", "", 20, TintaOscura, TextAnchor.UpperCenter);
            Anclar(_titularTexto.rectTransform, new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.95f));
            Boton(caja, "COMENZAR JORNADA", VerdeSello,
                  new Vector2(0.30f, 0.06f), new Vector2(0.70f, 0.22f),
                  () => { _panelAmanecer.SetActive(false); G.ComenzarDia(); });
        }

        private void ConstruirPanelReglas()
        {
            _panelReglas = Overlay("Reglas", Hex("#00000099"));
            var caja = Panel(_panelReglas.GetComponent<RectTransform>(), "Caja", PapelViejo,
                             new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f));
            var cab = Panel(caja, "Cab", AzulSaime, new Vector2(0, 0.9f), Vector2.one);
            var t = Texto(cab, "T", "LIBRO DE REGLAS — SAIME", 18, Papel, TextAnchor.MiddleCenter);
            Estirar(t.rectTransform);
            _reglasTexto = Texto(caja, "Cuerpo", "", 15, TintaOscura, TextAnchor.UpperLeft);
            Anclar(_reglasTexto.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f));
            Boton(caja, "CERRAR", RojoSello, new Vector2(0.38f, 0.02f), new Vector2(0.62f, 0.10f),
                  () => _panelReglas.SetActive(false));
            _panelReglas.SetActive(false);
        }

        private void ConstruirPanelCuadre()
        {
            _panelCuadre = Overlay("Cuadre", Hex("#1A2420F5"));
            var rt = _panelCuadre.GetComponent<RectTransform>();
            var caja = Panel(rt, "Caja", Papel, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.94f));
            var cab = Panel(caja, "Cab", TintaOscura, new Vector2(0, 0.91f), Vector2.one);
            var t = Texto(cab, "T", "CUADRE DEL DÍA — EL RANCHO", 18, Papel, TextAnchor.MiddleCenter);
            Estirar(t.rectTransform);
            _cuadreTexto = Texto(caja, "Resumen", "", 15, TintaOscura, TextAnchor.UpperLeft);
            Anclar(_cuadreTexto.rectTransform, new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.90f));
            _listaGastos = Panel(caja, "Gastos", PapelViejo,
                                 new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.64f));
            Boton(caja, "TIENDA DEL BUHONERO", Amarillo,
                  new Vector2(0.05f, 0.03f), new Vector2(0.32f, 0.13f),
                  () => { RefrescarTienda(); _panelTienda.SetActive(true); }, TintaOscura);
            Boton(caja, "REMESA $", AzulSaime,
                  new Vector2(0.36f, 0.03f), new Vector2(0.60f, 0.13f), PedirRemesa);
            Boton(caja, "DORMIR >>", VerdeSello,
                  new Vector2(0.64f, 0.03f), new Vector2(0.95f, 0.13f),
                  () => { _panelCuadre.SetActive(false); G.Dormir();
                          if (G.Fase == FaseJuego.Amanecer) MostrarAmanecer(); });
            _panelCuadre.SetActive(false);
        }

        private void ConstruirPanelTienda()
        {
            _panelTienda = Overlay("Tienda", Hex("#000000AA"));
            var caja = Panel(_panelTienda.GetComponent<RectTransform>(), "Caja", PapelViejo,
                             new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.9f));
            var cab = Panel(caja, "Cab", Amarillo, new Vector2(0, 0.9f), Vector2.one);
            var t = Texto(cab, "T", "EL BUHONERO DEL SÓTANO 2 — \"solo verdes, mi pana\"",
                          16, TintaOscura, TextAnchor.MiddleCenter);
            Estirar(t.rectTransform);
            _listaTienda = Panel(caja, "Lista", Papel, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.88f));
            Boton(caja, "CERRAR", RojoSello, new Vector2(0.38f, 0.02f), new Vector2(0.62f, 0.10f),
                  () => _panelTienda.SetActive(false));
            _panelTienda.SetActive(false);
        }

        private void ConstruirPanelTerremoto()
        {
            _panelTerremoto = Overlay("Terremoto", Hex("#7B1C12EE"));
            var t = Texto(_panelTerremoto.GetComponent<RectTransform>(), "T", "", 26, Papel,
                          TextAnchor.MiddleCenter);
            t.name = "TxtSismo";
            Estirar(t.rectTransform);
            Boton(_panelTerremoto.GetComponent<RectTransform>(), "SALIR DEL ESCOMBRO >>", TintaOscura,
                  new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f),
                  () => { _panelTerremoto.SetActive(false); G.TerminarJornada(); });
            _panelTerremoto.SetActive(false);
        }

        private void ConstruirPanelRemesa()
        {
            _panelRemesa = Overlay("Remesa", Hex("#000000CC"));
            var caja = Panel(_panelRemesa.GetComponent<RectTransform>(), "Caja", Papel,
                             new Vector2(0.28f, 0.32f), new Vector2(0.72f, 0.68f));
            _remesaTexto = Texto(caja, "T", "Llamando a la prima en Chile...", 17, TintaOscura,
                                 TextAnchor.UpperCenter);
            Anclar(_remesaTexto.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f));
            var marco = Panel(caja, "MarcoBarra", TintaOscura,
                              new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.42f));
            _barraRemesa = ImagenNueva(marco, "Barra");
            _barraRemesa.color = VerdeSello;
            _barraRemesa.rectTransform.anchorMin = new Vector2(0, 0);
            _barraRemesa.rectTransform.anchorMax = new Vector2(0, 1);
            _barraRemesa.rectTransform.offsetMin = Vector2.zero;
            _barraRemesa.rectTransform.offsetMax = Vector2.zero;
            _panelRemesa.SetActive(false);
        }

        private void ConstruirPanelFin()
        {
            _panelFin = Overlay("Fin", Hex("#0D0D0DF5"));
            var caja = Panel(_panelFin.GetComponent<RectTransform>(), "Caja", TintaOscura,
                             new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.8f));
            _finTexto = Texto(caja, "T", "", 19, Papel, TextAnchor.MiddleCenter);
            Anclar(_finTexto.rectTransform, new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.95f));
            Boton(caja, "OTRA VEZ DESDE EL PRINCIPIO", Naranja,
                  new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.18f),
                  () => { G.ReiniciarPartida(); _panelFin.SetActive(false); MostrarAmanecer(); });
            _panelFin.SetActive(false);
        }

        // ================================================================
        //  LÓGICA DE PRESENTACIÓN
        // ================================================================
        private void Suscribir()
        {
            G.AlLlegarViajero += MostrarViajero;
            G.AlDecidir += (v, d, res) =>
            {
                _feedback.text = res.Correcta
                    ? $"CORRECTO (+{10 * G.Estado.Tienda.MultiplicadorPuntos():0} pts)\n\n\"{G.Generador.Despedida(d == Decision.Siga)}\""
                    : $"× BOLETA DE MULTA ×\n{res.Explicacion}\nLo correcto era: {res.DecisionCorrecta}";
                RefrescarBarra();
            };
            G.AlTemblar += r =>
            {
                _panelTerremoto.SetActive(true);
                _panelTerremoto.transform.Find("TxtSismo").GetComponent<Text>().text =
                    $"¡¡TERREMOTO EN LA GUAIRA!!\n\nLa cola se dispersó gritando.\n" +
                    $"{r.PasaronSinControl} personas pasaron sin control en el caos." +
                    (r.Peligrosos > 0 ? "\n\nAlgo te dice que no todos eran inocentes..." : "");
                StartCoroutine(Sacudir());
            };
            G.AlCuadre += MostrarCuadre;
            G.AlCosecharEventos += evs =>
            {
                _feedback.text = "MIENTRAS DORMÍAS:\n" +
                    string.Join("\n— ", evs.Select(e => e.Texto));
            };
            G.AlTerminarPartida += (tipo, narrativa) =>
            {
                _panelFin.SetActive(true);
                _finTexto.text = narrativa;
            };
        }

        private void MostrarAmanecer()
        {
            var def = G.Reglas.Dia(G.Estado.DiaActual);
            _panelAmanecer.SetActive(true);
            _titularTexto.text =
                $"DÍA {def.numero} — {G.Reglas.FechaDelDia(def.numero)}\n" +
                $"CONTROL DE {def.tipo.ToUpper()}\n\n" +
                $"EL UNIVERSAL: \"{def.titular}\"\n\n" +
                "REGLAS NUEVAS:\n- " + string.Join("\n- ", def.reglasNuevas);
            _reglasTexto.text = TextoLibro(def);
            RefrescarBarra();
        }

        private string TextoLibro(DefDia def)
        {
            var visas = G.Reglas.Destinos.destinos.Where(d => d.exigeVisa)
                         .Select(d => d.ciudad);
            var fiebre = G.Reglas.Destinos.destinos.Where(d => d.exigeFiebreAmarilla)
                         .Select(d => d.ciudad);
            string alerta = def.listaAlerta.Length > 0
                ? "\n\n*** LISTA DE ALERTA: " + string.Join(", ", def.listaAlerta) + " = ¡ALCABALA! ***"
                : "";
            return "REGLAS VIGENTES HOY:\n- " + string.Join("\n- ", def.reglasNuevas) +
                   $"\n\nDESTINOS QUE EXIGEN VISA (desde día 3):\n{string.Join(", ", visas)}" +
                   $"\n\nEXIGEN FIEBRE AMARILLA (desde día 6):\n{string.Join(", ", fiebre)}" +
                   alerta +
                   "\n\nRECUERDE: pasaporte vencido CON prórroga vigente ES VÁLIDO (prórroga = vence + 2 años).";
        }

        private void MostrarViajero(Viajero v)
        {
            if (v == null) return;
            _retrato.sprite = CargarRetrato(v.RetratoId, false);
            _fotoPasaporte.sprite = CargarRetrato(v.Pasaporte.RetratoId, v.Pasaporte.FotoEsVariante);
            _fotoPasaporte.color = new Color(0.85f, 0.85f, 0.8f); // foto "impresa"
            _globo.text = $"“{v.DialogoSaludo}”";
            _botonSoborno.SetActive(v.OfreceSoborno);
            if (v.OfreceSoborno) _globo.text += $"\n\n“{G.Generador.FraseSoborno()}” (${v.MontoSobornoUsd})";

            var p = v.Pasaporte;
            Rellenar(_camposPasaporte,
                $"NOMBRE: {p.NombreCompleto}",
                $"CÉDULA: {p.Cedula}    SEXO: {p.Sexo}",
                $"NACIMIENTO: {p.FechaNacimiento}  (edad {v.Edad})",
                $"VENCE: {p.FechaVencimiento}",
                p.TieneProrroga ? $"PRÓRROGA HASTA: {p.ProrrogaHasta} [+]" : "PRÓRROGA: —",
                "");

            var b = v.Boleto;
            var dest = G.Reglas.Destino(b.Destino);
            Rellenar(_camposBoleto,
                $"PASAJERO: {b.NombreCompleto}",
                $"DESTINO: {dest.ciudad} ({dest.pais})",
                $"AEROLÍNEA: {b.Aerolinea}",
                $"FECHA VUELO: {b.FechaVuelo}",
                b.TasaPagada ? "TASA AEROPORTUARIA: PAGADA [SI]" : "TASA: SIN PAGAR [×]");

            var e = v.Extras;
            Rellenar(_camposExtras,
                $"CÉDULA SEGÚN BOLETO: {e.CedulaEnBoleto}",
                $"VISA: {(e.TraeVisa ? "PRESENTA [SI]" : "NO PRESENTA [×]")}",
                $"CERT. FIEBRE AMARILLA: {(e.TraeCertFiebreAmarilla ? "PRESENTA [SI]" : "NO PRESENTA [×]")}",
                v.EsMenor ? $"PERMISO DE MENOR: {(e.TraePermisoMenor ? "NOTARIADO [SI]" : "NO TRAE [×]")}"
                          : "PERMISO DE MENOR: no aplica (mayor de edad)");
            RefrescarBarra();
        }

        private void MostrarCuadre(List<GastoDia> gastos, float sueldoBs, float multasBs)
        {
            _panelCuadre.SetActive(true);
            var eco = G.Estado.Economia;
            string familia = string.Join("\n", eco.Familia.Select(m =>
                $"   {IconoSalud(m.Salud)} {m.Nombre} ({m.Rol}) — {m.Salud}"));
            _cuadreTexto.text =
                $"Sueldo: +Bs {sueldoBs:N0}   Multas: −Bs {multasBs:N0}   " +
                $"Puntos: {G.Estado.PuntosHoy:0}\n" +
                $"Caja: Bs {eco.DineroBs:N0}  +  ${eco.DineroUsd:0.00}   " +
                $"(tasa hoy: Bs {eco.Tasa.BsPorDolar:N0}/$)\n\nLA FAMILIA:\n{familia}";

            foreach (Transform h in _listaGastos) Destroy(h.gameObject);
            for (int i = 0; i < gastos.Count; i++)
            {
                var g = gastos[i];
                float alto = 1f / Mathf.Max(gastos.Count, 1);
                float yMax = 1f - i * alto;
                var fila = Panel(_listaGastos, $"G{i}", Papel,
                                 new Vector2(0.01f, yMax - alto + 0.015f), new Vector2(0.99f, yMax - 0.01f));
                float costoBs = g.MontoUsd * eco.Tasa.BsPorDolar;
                var t = Texto(fila, "T",
                    $"{g.Concepto}{(g.Obligatorio ? "  (OBLIGATORIO)" : "")} — Bs {costoBs:N0}",
                    14, TintaOscura, TextAnchor.MiddleLeft);
                Anclar(t.rectTransform, new Vector2(0.02f, 0), new Vector2(0.7f, 1));
                var gRef = g; var filaRef = fila;
                Boton(fila, g.MontoUsd <= 0 ? "GRATIS (CLAP)" : "PAGAR",
                      g.MontoUsd <= 0 ? Amarillo : VerdeSello,
                      new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f), () =>
                {
                    if (G.PagarGasto(gRef))
                        filaRef.GetComponent<Image>().color = Hex("#C7E5C4");
                    else
                        filaRef.GetComponent<Image>().color = Hex("#E8B7B0");
                    MostrarCuadreResumen();
                }, g.MontoUsd <= 0 ? TintaOscura : Papel);
            }
        }

        private void MostrarCuadreResumen()
        {
            var eco = G.Estado.Economia;
            string familia = string.Join("\n", eco.Familia.Select(m =>
                $"   {IconoSalud(m.Salud)} {m.Nombre} ({m.Rol}) — {m.Salud}"));
            _cuadreTexto.text =
                $"Puntos: {G.Estado.PuntosHoy:0}\n" +
                $"Caja: Bs {eco.DineroBs:N0}  +  ${eco.DineroUsd:0.00}   " +
                $"(tasa hoy: Bs {eco.Tasa.BsPorDolar:N0}/$)\n\nLA FAMILIA:\n{familia}";
        }

        private void RefrescarTienda()
        {
            foreach (Transform h in _listaTienda) Destroy(h.gameObject);
            var cat = G.Estado.Tienda.Catalogo;
            for (int i = 0; i < cat.Count; i++)
            {
                var eq = cat[i];
                float alto = 1f / cat.Count;
                float yMax = 1f - i * alto;
                var fila = Panel(_listaTienda, $"E{i}", eq.Comprado ? Hex("#C7E5C4") : Papel,
                                 new Vector2(0.01f, yMax - alto + 0.012f), new Vector2(0.99f, yMax - 0.008f));
                var t = Texto(fila, "T",
                    $"{eq.Nombre} — ${eq.PrecioUsd:0}\n{eq.Descripcion}",
                    13, TintaOscura, TextAnchor.MiddleLeft);
                Anclar(t.rectTransform, new Vector2(0.02f, 0), new Vector2(0.74f, 1));
                if (!eq.Comprado)
                {
                    var eqRef = eq;
                    Boton(fila, "COMPRAR", VerdeSello,
                          new Vector2(0.76f, 0.15f), new Vector2(0.98f, 0.85f), () =>
                    {
                        if (G.Estado.Tienda.Comprar(eqRef.Clave, G.Estado.Economia))
                            RefrescarTienda();
                    });
                }
            }
        }

        private void Decidir(Decision d)
        {
            if (G.Fase != FaseJuego.Jornada) return;
            G.Decidir(d);
        }

        private void PedirRemesa()
        {
            if (!G.Estado.Remesas.PuedeLlamar)
            {
                _feedback.text = "La prima no contesta más por hoy. (límite de remesas diario)";
                return;
            }
            _panelRemesa.SetActive(true);
            var prov = (Anuncios.ProveedorSimulado)G.Estado.Remesas.Proveedor;
            prov.AlProgresar += p => _barraRemesa.rectTransform.anchorMax = new Vector2(p, 1);
            StartCoroutine(G.Estado.Remesas.LlamarALaPrima(G.Estado.Economia, (ok, monto) =>
            {
                _remesaTexto.text = ok
                    ? $"“¡Llegó, prima!” +${monto:0} en la cuenta."
                    : "Se cayó la llamada...";
                Invoke(nameof(CerrarRemesa), 1.2f);
            }));
        }

        private void CerrarRemesa()
        {
            _panelRemesa.SetActive(false);
            _barraRemesa.rectTransform.anchorMax = new Vector2(0, 1);
            RefrescarBarra();
            if (_panelCuadre.activeSelf) MostrarCuadreResumen();
        }

        private System.Collections.IEnumerator Sacudir()
        {
            var rt = _raiz;
            Vector2 origen = rt.anchoredPosition;
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                rt.anchoredPosition = origen + Random.insideUnitCircle * 14f * (1.6f - t);
                yield return null;
            }
            rt.anchoredPosition = origen;
        }

        private void RefrescarBarra()
        {
            var e = G.Estado;
            var def = G.Reglas.Dia(e.DiaActual);
            _barraInfo.text =
                $"DÍA {e.DiaActual} ({def.tipo})   |   {G.Reglas.FechaDelDia(e.DiaActual)}   |   " +
                $"Bs {e.Economia.DineroBs:N0}  +  ${e.Economia.DineroUsd:0.00}   |   " +
                $"Tasa: Bs {e.Economia.Tasa.BsPorDolar:N0}/$   |   " +
                $"Bien: {e.AciertosHoy}  Mal: {e.ErroresHoy}   |   Puntos: {e.PuntosHoy:0}";
        }

        private string IconoSalud(EstadoSalud s) =>
            s == EstadoSalud.Bien ? "[OK]" : s == EstadoSalud.Debil ? "[DÉBIL]" :
            s == EstadoSalud.Enfermo ? "[ENFERMO]" : s == EstadoSalud.Critico ? "[CRÍTICO]" : "[+RIP+]";

        private Sprite CargarRetrato(int id, bool variante)
        {
            string nombre = $"Retratos/retrato_{id:000}" + (variante ? "_alt" : "");
            var sp = Resources.Load<Sprite>(nombre);
            return sp != null ? sp : Resources.Load<Sprite>($"Retratos/retrato_{id:000}");
        }

        // ================================================================
        //  HELPERS UGUI
        // ================================================================
        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        private RectTransform Panel(RectTransform padre, string nombre, Color color,
                                    Vector2 min, Vector2 max)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            go.GetComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private GameObject Overlay(string nombre, Color color)
        {
            var rt = Panel(_raiz, nombre, color, Vector2.zero, Vector2.one);
            return rt.gameObject;
        }

        private Text Texto(RectTransform padre, string nombre, string contenido,
                           int tamano, Color color, TextAnchor ancla)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
            var t = go.GetComponent<Text>();
            t.font = _fuente; t.text = contenido; t.fontSize = tamano;
            t.color = color; t.alignment = ancla;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Image ImagenNueva(RectTransform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            return go.GetComponent<Image>();
        }

        private GameObject Boton(RectTransform padre, string etiqueta, Color color,
                                 Vector2 min, Vector2 max, UnityEngine.Events.UnityAction accion,
                                 Color? colorTexto = null)
        {
            var rt = Panel(padre, "Btn_" + etiqueta, color, min, max);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(accion);
            var t = Texto(rt, "Txt", etiqueta, 15, colorTexto ?? Papel, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            Estirar(t.rectTransform);
            return rt.gameObject;
        }

        private static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void EstirarConMargen(RectTransform rt, float margen)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margen, margen);
            rt.offsetMax = new Vector2(-margen, -margen);
        }

        private static void Anclar(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void Rellenar(List<Text> campos, params string[] valores)
        {
            for (int i = 0; i < campos.Count; i++)
                campos[i].text = i < valores.Length ? valores[i] : "";
        }
    }
}
