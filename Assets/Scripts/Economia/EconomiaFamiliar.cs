using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Maiquetia.Economia
{
    public enum EstadoSalud { Bien, Debil, Enfermo, Critico, Fallecido }

    [Serializable]
    public class MiembroFamilia
    {
        public string Nombre;
        public string Rol;            // "esposa", "hijo", "mamá", "hermano"
        public EstadoSalud Salud = EstadoSalud.Bien;
        public bool NecesitaMedicina; // crónico (mamá) o eventual (hijo)

        public void Empeorar()
        {
            if (Salud < EstadoSalud.Fallecido) Salud++;
        }

        public void Mejorar()
        {
            if (Salud > EstadoSalud.Bien && Salud < EstadoSalud.Fallecido) Salud--;
        }
    }

    /// <summary>Tasa de cambio Bs/$ con inflación diaria y saltos por evento.</summary>
    [Serializable]
    public class TasaCambio
    {
        public float BsPorDolar = 3300f;          // arranque estilo 2019
        public const float InflacionDiaria = 1.06f; // 6% diario: brutal pero jugable

        public void NuevoDia(bool salto)
        {
            BsPorDolar *= InflacionDiaria;
            if (salto) BsPorDolar *= 1.35f;        // "el paralelo pegó un salto"
        }
    }

    [Serializable]
    public class GastoDia
    {
        public string Concepto;
        public float MontoUsd;      // se cobra en Bs a la tasa del día
        public bool Obligatorio;    // alquiler sí o sí; medicina si hay enfermo
        public bool Pagado;
    }

    /// <summary>
    /// El cuadre de fin de jornada: sueldo, gastos, salud familiar.
    /// Todo el dinero se guarda en Bs + USD por separado (los USD no se inflan).
    /// </summary>
    [Serializable]
    public class EconomiaFamiliar
    {
        public float DineroBs = 25000f;
        public float DineroUsd = 0f;
        public TasaCambio Tasa = new TasaCambio();
        public List<MiembroFamilia> Familia = new List<MiembroFamilia>
        {
            new MiembroFamilia { Nombre = "Yubraska",   Rol = "esposa" },
            new MiembroFamilia { Nombre = "Santiaguito", Rol = "hijo" },
            new MiembroFamilia { Nombre = "Mamá Carmen", Rol = "mamá", NecesitaMedicina = true },
            new MiembroFamilia { Nombre = "Jeikel",      Rol = "hermano" },
        };

        public const float SueldoPorViajeroUsd = 1.2f;  // pagado en Bs a tasa del día
        public const float MultaPorErrorUsd = 3.0f;

        public int DiasSinPagarAlquiler;
        public bool HuboEscasezMedicina;                // se sortea cada día

        public float SueldoDelDia(int procesadosBien) =>
            procesadosBien * SueldoPorViajeroUsd * Tasa.BsPorDolar;

        public float MultasDelDia(int errores) =>
            errores * MultaPorErrorUsd * Tasa.BsPorDolar;

        public List<GastoDia> GastosDelDia(bool huboApagon, System.Random rng)
        {
            HuboEscasezMedicina = rng.NextDouble() < 0.25; // 1 de 4 días no hay ni pagando
            var gastos = new List<GastoDia>
            {
                new GastoDia { Concepto = "Alquiler",          MontoUsd = 4.0f, Obligatorio = true },
                new GastoDia { Concepto = "Comida",            MontoUsd = 3.5f, Obligatorio = true },
                new GastoDia { Concepto = "Bombona de gas",    MontoUsd = 1.0f, Obligatorio = false },
            };
            if (Familia.Any(m => m.NecesitaMedicina && m.Salud != EstadoSalud.Fallecido))
                gastos.Add(new GastoDia
                {
                    Concepto = HuboEscasezMedicina
                        ? "Medicinas (¡NO HAY EN FARMACIA!)"
                        : "Medicinas de mamá",
                    MontoUsd = 2.5f,
                    Obligatorio = false
                });
            if (huboApagon)
                gastos.Add(new GastoDia { Concepto = "Gasolina pa' la planta", MontoUsd = 2.0f, Obligatorio = false });
            return gastos;
        }

        /// <summary>Intenta pagar un gasto. Devuelve true si alcanzó.</summary>
        public bool Pagar(GastoDia gasto)
        {
            float costoBs = gasto.MontoUsd * Tasa.BsPorDolar;
            if (gasto.Concepto.StartsWith("Medicinas") && HuboEscasezMedicina)
                return false; // no hay, ni con plata

            if (DineroBs >= costoBs) { DineroBs -= costoBs; gasto.Pagado = true; return true; }
            // completar con dólares si hay
            float faltanUsd = (costoBs - DineroBs) / Tasa.BsPorDolar;
            if (DineroUsd >= faltanUsd)
            {
                DineroUsd -= faltanUsd;
                DineroBs = 0;
                gasto.Pagado = true;
                return true;
            }
            return false;
        }

        /// <summary>Aplica las consecuencias nocturnas de lo pagado y lo no pagado.</summary>
        public void CerrarNoche(List<GastoDia> gastos)
        {
            bool comieron = gastos.First(g => g.Concepto == "Comida").Pagado;
            bool medicina = gastos.Any(g => g.Concepto.StartsWith("Medicinas") && g.Pagado);

            DiasSinPagarAlquiler = gastos.First(g => g.Concepto == "Alquiler").Pagado
                ? 0 : DiasSinPagarAlquiler + 1;

            foreach (var m in Familia.Where(m => m.Salud != EstadoSalud.Fallecido))
            {
                if (!comieron) m.Empeorar();
                if (m.NecesitaMedicina) { if (medicina) m.Mejorar(); else m.Empeorar(); }
                else if (comieron && m.Salud > EstadoSalud.Bien && UnityEngine.Random.value < 0.5f)
                    m.Mejorar();
            }
        }

        public int FamiliaresVivos() =>
            Familia.Count(m => m.Salud != EstadoSalud.Fallecido);

        public int FamiliaresFallecidos() =>
            Familia.Count(m => m.Salud == EstadoSalud.Fallecido);
    }
}
