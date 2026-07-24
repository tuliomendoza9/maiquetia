using UnityEngine;
using UnityEditor;
using System.Linq;
using Maiquetia.Nucleo;
using Maiquetia.Viajeros;
using Maiquetia.Reglas;
using Maiquetia.Consecuencias;

/// <summary>
/// Prueba headless del núcleo: simula partidas completas con un "jugador
/// perfecto" y un "jugador torpe" y verifica que el juego se comporte.
/// Correr con: -executeMethod PruebaNucleo.Ejecutar
/// </summary>
public static class PruebaNucleo
{
    private static int _fallos;

    public static void Ejecutar()
    {
        try
        {
            var go = new GameObject("GestorPrueba");
            var gestor = go.AddComponent<GestorJuego>();
            gestor.Inicializar(semillaForzada: 42);

            Verificar(gestor.Reglas.Campania.dias.Length == 7, "campaña de 7 días");
            Verificar(gestor.Reglas.Destinos.destinos.Length == 12, "12 destinos");

            // ---------- Partida 1: jugador PERFECTO ----------
            SimularPartida(gestor, perfecto: true, out var finalPerfecto, out var dineroFinal);
            Verificar(finalPerfecto == TipoFinal.FuncionarioEterno,
                      $"jugador perfecto sobrevive (final={finalPerfecto})");
            Verificar(dineroFinal > 0, $"jugador perfecto no quiebra (Bs={dineroFinal:0})");

            // ---------- Partida 2: jugador TORPE (todo SIGA) ----------
            gestor.Inicializar(semillaForzada: 42);
            SimularPartida(gestor, perfecto: false, out var finalTorpe, out _);
            Verificar(finalTorpe == TipoFinal.Despedido || finalTorpe == TipoFinal.Bancarrota
                      || finalTorpe == TipoFinal.Luto,
                      $"jugador torpe pierde (final={finalTorpe})");

            // ---------- Generador: distribución de discrepancias ----------
            gestor.Inicializar(semillaForzada: 7);
            gestor.Generador.SembrarDia(3, 7);
            int conDiscrepancia = 0;
            for (int i = 0; i < 100; i++)
                if (gestor.Generador.Generar(3).Discrepancia != TipoDiscrepancia.Ninguna)
                    conDiscrepancia++;
            Verificar(conDiscrepancia > 35 && conDiscrepancia < 85,
                      $"proporción de discrepancias razonable ({conDiscrepancia}/100)");

            // ---------- Validador: la prórroga válida NO se niega ----------
            var libro = gestor.Reglas;
            var hoy = libro.FechaDelDia(2);
            var vip = new Viajero
            {
                NombreReal = "Prueba Prórroga", Edad = 30,
                Discrepancia = TipoDiscrepancia.Ninguna,
                Pasaporte = new Pasaporte
                {
                    NombreCompleto = "Prueba Prórroga",
                    FechaVencimiento = hoy.MasDias(-100),
                    TieneProrroga = true,
                    ProrrogaHasta = hoy.MasDias(630),
                },
            };
            Verificar(vip.Pasaporte.VigenteEn(hoy), "pasaporte vencido + prórroga vigente = válido");

            if (_fallos == 0) { Debug.Log("NUCLEO_OK todas las verificaciones pasaron"); EditorApplication.Exit(0); }
            else { Debug.LogError($"NUCLEO_FALLO {_fallos} verificaciones fallidas"); EditorApplication.Exit(3); }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"NUCLEO_EXCEPCION {ex}");
            EditorApplication.Exit(4);
        }
    }

    private static void SimularPartida(GestorJuego gestor, bool perfecto,
                                       out TipoFinal final, out float dineroBs)
    {
        var f = TipoFinal.Ninguno;
        System.Collections.Generic.List<Maiquetia.Economia.GastoDia> gastosHoy = null;
        gestor.AlTerminarPartida += (tipo, _) => f = tipo;
        gestor.AlCuadre += (g, _, __) => gastosHoy = g;   // ANTES de jugar: captura la lista real

        int seguridad = 0;
        while (gestor.Estado.FinalAlcanzado == TipoFinal.Ninguno && seguridad++ < 20)
        {
            gastosHoy = null;
            gestor.ComenzarDia();

            while (gestor.Fase == FaseJuego.Jornada && gestor.ViajeroActual != null)
            {
                var v = gestor.ViajeroActual;
                var decision = perfecto
                    ? gestor.Validador.Correcta(v, gestor.Estado.DiaActual)
                    : Decision.Siga; // el torpe deja pasar a todo el mundo
                gestor.Decidir(decision);
            }

            if (gestor.Fase == FaseJuego.Terremoto) gestor.TerminarJornada();

            Verificar(gastosHoy != null, $"el cuadre del día {gestor.Estado.DiaActual} entregó gastos");
            if (gastosHoy == null) break;

            // pagar lo que alcance, obligatorios primero
            foreach (var g in gastosHoy.OrderByDescending(x => x.Obligatorio))
                gestor.PagarGasto(g);

            gestor.Dormir();
        }

        final = f != TipoFinal.Ninguno ? f : gestor.Estado.FinalAlcanzado;
        dineroBs = gestor.Estado.Economia.DineroBs;
    }

    private static void Verificar(bool ok, string nombre)
    {
        if (ok) Debug.Log($"  ✓ {nombre}");
        else { Debug.LogError($"  ✗ FALLO: {nombre}"); _fallos++; }
    }
}
