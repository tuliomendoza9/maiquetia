# MAIQUETÍA: PUNTO DE CONTROL
### Documento de Diseño del Juego (GDD) — v0.1
*Género: simulador de burocracia con dilemas morales (inspirado en Papers, Please)*
*Motor: Unity 6.5 · Arte: Blender 5.2 (pre-renderizado) · Plataforma inicial: WebGL + Windows*

---

## 1. Concepto

Eres **funcionario de migración del SAIME** en el Aeropuerto Internacional Simón Bolívar de Maiquetía. Tu trabajo: revisar pasaportes, encontrar errores y decidir quién pasa. Tu problema: el sueldo es en bolívares, la inflación no perdona, y tu familia depende de ti.

**El giro venezolano:** en Papers, Please la gente lucha por *entrar* a Arstotzka. En Maiquetía, el drama es doble:
- **Días de SALIDA**: controlas a los que se van — la diáspora. Familias despidiéndose, chamos con la maleta llena de sueños, abuelas que viajan a conocer nietos que nacieron lejos.
- **Días de ENTRADA**: controlas a los que llegan — retornados, deportados, extranjeros de negocios turbios, y de vez en cuando... gente muy importante.

Cada tipo de día tiene su propio libro de reglas.

## 2. Bucle principal (core loop)

```
AMANECE → Titular de prensa del día → Libro de reglas actualizado
   ↓
JORNADA: viajero por viajero
   · Llega a la ventanilla, saluda (diálogo con jerga real)
   · Entrega documentos (pasaporte, boleto, y lo que la regla del día exija)
   · Jugador COMPARA: datos vs documentos vs reglas vs apariencia
   · Puede INTERROGAR (señalar discrepancia → el viajero responde/se delata)
   · SELLO: ✅ SIGA … o ❌ NEGADO … o botón ALCABALA (detener/reportar)
   ↓
ATARDECE → Cuadre del día:
   · Sueldo (por viajero bien procesado) − multas (errores)
   · La TASA DEL DÓLAR subió → tus bolívares valen menos que ayer
   · Gastos: alquiler, comida, medicinas de mamá, la bombona, la planta
   · Decisiones familiares (¿medicina o comida? ¿pagar la luz o ahorrar?)
   ↓
DUERME → eventos nocturnos según tus FLAGS (consecuencias) → AMANECE
```

## 3. Mecánica de inspección

### Documentos del juego (props estilizados, NO réplicas)
| Documento | Campos que se cruzan |
|---|---|
| **Pasaporte** | nombre, cédula, foto, fecha nac., vencimiento, **prórroga** |
| **Boleto aéreo** | nombre, destino, aerolínea, fecha, sello de tasa aeroportuaria |
| **Cert. fiebre amarilla** | requerido para ciertos destinos (regla cambia por día) |
| **Permiso de menor** | notariado, requerido si viaja menor sin ambos padres |
| **Visa del destino** | requerida según país (tabla en el libro de reglas) |
| **Carnet especial** | algunos viajeros lo presentan… y esperan trato especial |

### Tipos de discrepancia (generadas proceduralmente, dificultad creciente)
1. Nombre no coincide entre documentos (typo de una letra)
2. Pasaporte vencido **pero con prórroga válida** (¡trampa! la prórroga ES válida — negar es error)
3. Prórroga vencida (parece válida, hay que sumar fechas)
4. Foto no corresponde (pelo/lentes/edad — tolerancia configurable)
5. Cédula con dígito cambiado
6. Destino exige visa y no la tiene
7. Menor sin permiso notariado
8. Fecha del boleto ≠ hoy
9. Nombre en la **lista de alerta** del día
10. Documento perfecto — el viajero solo está nervioso (falso positivo, la trampa moral)

### El libro de reglas
Objeto hojeable en pantalla. Cambia cada día. Ejemplos de reglas satíricas pero funcionales:
- *"Desde hoy la prórroga solo vale si fue emitida antes del 15."*
- *"Los que viajan a Chile, Perú o Ecuador: exigir visa."* (eco de 2019)
- *"Por decreto, hoy no paga tasa aeroportuaria quien porte el carnet especial."*
- *"Alerta: se busca ciudadano de apellido ______."*

## 4. Economía: la inflación es un enemigo

- Sueldo en **bolívares**; los gastos suben cada día con la tasa.
- Pantalla de cuadre muestra: `Tasa BCV de hoy: Bs. X / $` — sube sola, a veces PEGA UN SALTO (evento).
- Opción arriesgada: cambiar bolívares a dólares "con el pana de la 7" (mejor tasa, pero deja FLAG de actividad sospechosa).
- Gastos: alquiler, comida, **medicinas de mamá** (escasez: a veces no hay ni pagando), bombona de gas, agua de cisterna, y cuando hay apagón: **la planta** (gasolina).
- **Evento CLAP**: a veces llega la caja — comida gratis ese día, pero aceptar deja flag.

## 5. La familia

| Miembro | Necesidad | Riesgo |
|---|---|---|
| **Yubraska** (esposa) | comida, moral | se deprime → eventos |
| **Santiaguito** (hijo, 8) | comida, escuela | enferma fácil |
| **Mamá Carmen** | MEDICINAS (hipertensión) | crítica sin tratamiento |
| **Jeikel** (hermano, 24) | quiere irse del país | pide plata para el pasaje → dilema recurrente |

Estados: bien → débil → enfermo → crítico → 💀. La medicina falta aunque tengas plata (escasez aleatoria) → ahí entra el módulo de anuncios (§8).

## 6. Sistema de consecuencias (flags)

Toda decisión notable escribe un **flag persistente**. Los flags disparan eventos días después:

- Dejaste pasar a la señora que lloraba sin permiso del menor → día +3: titular de prensa; día +5: te visita un inspector.
- Aceptaste la "colaboración" (matraca) de un viajero → contador de corrupción; al 3er soborno detectado: arresto.
- Negaste al funcionario del carnet especial → tu expediente "no colabora" → te recortan el sueldo.
- Ayudaste al grupo clandestino **"Los Hijos de la Guacamaya"** (nuestra versión de EZIC) → ruta de finales alternos.
- Reportaste a la persona equivocada en la alcabala → su familia aparece después en tu cola.

## 7. Personajes especiales (encuentros guionados, sátira)

> Tratamiento: caricatura política de figuras públicas, basada solo en hechos notorios de dominio público. Nunca aparecen con documentos "reales" — todo es estilizado y absurdo a propósito.

1. **"El Presidente"** (caricatura de Maduro) — llega por la fila VIP con pasaporte donde la profesión dice *"conductor de autobús"*. Todos sus papeles están "en orden por decreto", pero hay UNA discrepancia real. Negarlo o aprobarlo abre ramas opuestas de consecuencias. Bigote imposible de no reconocer.
2. **"La Primera Combatiente"** (caricatura de Cilia Flores) — viaja con equipaje diplomático que "no se revisa". El libro de reglas del día dice que TODO equipaje se revisa. ¿Qué pesa más?
3. **"La Ingeniera"** (caricatura de María Corina Machado) — sus documentos están perfectos. Pero en tu pantalla aparece una orden: *"INHABILITADA — NO PUEDE SALIR"*. Es el espejo moral del juego: la regla dice no; todo lo demás dice sí.
4. **Stefano Savani** — cameo del universo *"Stefano: Motorizado de Caracas"* (nuestro juego anterior): llega con el casco puesto (¡pídele que se lo quite para comparar la foto!) y una caja de delivery. Easter egg amistoso.

## 8. Módulo de anuncios recompensados: "LA REMESA"

**Diegético**: en Venezuela se sobrevive con remesas. En el juego, ver un video publicitario = *"llamaste a tu prima en Chile y te mandó una remesa"*.

- Botón 📱 **"Llamar a la prima"** disponible en momentos de crisis (familiar crítico, no alcanza para el alquiler, multa impagable).
- Al ver el video (simulado por ahora): recibes $ de emergencia o la medicina que no se conseguía.
- Límite: N llamadas por día (configurable) para no romper la economía.
- **Arquitectura**: interfaz `IProveedorAnuncios` con `ProveedorSimulado` (placeholder de 5 s con barra de progreso). Cuando existan los videos reales: se implementa `ProveedorUnityAds` o `ProveedorAdMob` sin tocar una línea del juego.

## 9. Finales (10 al lanzamiento, ampliable)

| # | Final | Cómo se llega |
|---|---|---|
| 1 | **Bancarrota** — desalojados, fin | dinero < 0 dos días seguidos |
| 2 | **Luto** — la familia se apaga | 2+ familiares 💀 |
| 3 | **Arrestado por matraquero** | 3 sobornos detectados |
| 4 | **Despedido** | demasiadas multas acumuladas |
| 5 | **Preso político** | ayudar torpemente a la Guacamaya |
| 6 | **La revolución de la Guacamaya** | completar sus 5 encargos sin fallar |
| 7 | **El buen soldado** | 30 días sin un solo error ni piedad |
| 8 | **El pasaje de Jeikel** | financiar la ida del hermano… y la tuya |
| 9 | **Familia completa en Madrid** | ahorrar $2.000 en dólares sin flags |
| 10 | **El funcionario eterno** | sobrevivir 30 días en la mediocridad — el final "neutro" |

## 10. Dirección de arte

- **Estilo**: low-poly 3D renderizado a sprites 2D en Blender (headless) con sombreado plano/toon → estética "Papers, Please tropicalizado": paleta caribeña desgastada — ocres, verde militar, el azul SAIME, cielo naranja de La Guaira.
- **Retratos de viajeros**: sistema modular en Blender (cabezas × tonos de piel × pelos × ropa × accesorios) → cientos de combinaciones renderizadas a PNG. La foto del pasaporte usa el MISMO render (o uno alterado cuando hay discrepancia de foto).
- **Fondo**: cabina de migración modelada en 3D — ventanilla, cola de gente, avisos de "SEÑOR USUARIO…", ventilador de techo que sí gira, mar al fondo.
- **Documentos**: UI nativa de Unity (texto dinámico nítido) con marcos/texturas de Blender.
- **Audio** (fase 2): murmullo de aeropuerto, sello *KA-CHUNK*, ventilador, tambores de La Guaira lejanos.

## 11. Arquitectura técnica

```
Assets/
  Scripts/
    Nucleo/        GestorJuego, EstadoPartida (save/load JSON), RelojJornada
    Viajeros/      GeneradorViajeros, Viajero, Documento*, Discrepancia
    Reglas/        LibroReglas, Regla, ValidadorDecision
    Economia/      EconomiaFamiliar, Familia, MiembroFamilia, TasaCambio
    Consecuencias/ SistemaFlags, EventoDiferido, GestorFinales
    Anuncios/      IProveedorAnuncios, ProveedorSimulado, GestorRemesas
    UI/            MesaTrabajo, DocumentoArrastrable, LibroUI, SelloUI, CuadreUI
    Especiales/    EncuentroGuionado (data-driven desde JSON)
  StreamingAssets/Datos/
    dias.json        ← reglas y eventos por día
    nombres.json     ← nombres/apellidos venezolanos
    destinos.json    ← países, visas, requisitos
    dialogos.json    ← saludos, excusas, jerga
    especiales.json  ← guiones de los 4 encuentros
  Modelos/   ← FBX/GLB desde Blender
  UI/Retratos/ ← PNGs renderizados por Blender
blender/
  generar_retratos.py, generar_cabina.py, generar_props.py
```

**Todo el contenido es data-driven (JSON)**: días, reglas, diálogos y especiales se editan sin recompilar.

**Hooks de prueba**: objeto global `__mq` en WebGL (como `__mc`/`__zm` de los juegos anteriores) — avanzar día, forzar viajero, forzar discrepancia, leer estado — para que Claude pruebe solo en el Browser.

## 12. Alcance del MVP (primera versión jugable)

- [x] 7 días de campaña con reglas crecientes
- [x] 6 tipos de discrepancia
- [x] Economía + familia completa
- [x] 1 personaje especial (Stefano, el de menor riesgo) — los otros 3 en fase 2
- [x] Módulo remesa con proveedor simulado
- [x] 4 finales (bancarrota, luto, despedido, funcionario eterno)
