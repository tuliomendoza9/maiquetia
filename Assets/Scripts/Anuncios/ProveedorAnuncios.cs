using System;
using System.Collections;
using UnityEngine;

namespace Maiquetia.Anuncios
{
    /// <summary>
    /// Contrato del sistema de anuncios recompensados ("La Remesa").
    /// Hoy: ProveedorSimulado. Mañana: ProveedorUnityAds / ProveedorAdMob
    /// implementan esta MISMA interfaz y el juego no cambia ni una línea.
    /// </summary>
    public interface IProveedorAnuncios
    {
        bool HayAnuncioDisponible { get; }
        /// <param name="alTerminar">true = vio el video completo (paga recompensa)</param>
        IEnumerator MostrarAnuncio(Action<bool> alTerminar);
    }

    /// <summary>
    /// Simulador: "video" de N segundos con barra de progreso.
    /// Cuando existan los videos publicitarios reales, se sustituye.
    /// </summary>
    public class ProveedorSimulado : IProveedorAnuncios
    {
        public float DuracionSegundos = 5f;
        public bool HayAnuncioDisponible => true;

        public event Action<float> AlProgresar;   // 0..1 para la barra de la UI

        public IEnumerator MostrarAnuncio(Action<bool> alTerminar)
        {
            float t = 0;
            while (t < DuracionSegundos)
            {
                t += Time.deltaTime;
                AlProgresar?.Invoke(Mathf.Clamp01(t / DuracionSegundos));
                yield return null;
            }
            alTerminar?.Invoke(true);
        }
    }

    /// <summary>
    /// "Llamar a la prima en Chile": ver un anuncio => llega una remesa.
    /// Límite diario para no romper la economía del juego.
    /// </summary>
    [Serializable]
    public class GestorRemesas
    {
        public int LlamadasHoy;
        public const int MaxLlamadasPorDia = 2;
        public const float RemesaUsd = 5f;

        // JsonUtility NO corre inicializadores al deserializar: siempre
        // acceder por la propiedad, que garantiza instancia viva.
        [NonSerialized] private IProveedorAnuncios _proveedor;
        public IProveedorAnuncios Proveedor
        {
            get => _proveedor ?? (_proveedor = new ProveedorSimulado());
            set => _proveedor = value;
        }

        public bool PuedeLlamar =>
            LlamadasHoy < MaxLlamadasPorDia && Proveedor.HayAnuncioDisponible;

        public void NuevoDia() => LlamadasHoy = 0;

        /// <summary>Corrutina completa: anuncio + recompensa si lo vio entero.</summary>
        public IEnumerator LlamarALaPrima(Economia.EconomiaFamiliar eco, Action<bool, float> resultado)
        {
            if (!PuedeLlamar) { resultado?.Invoke(false, 0); yield break; }
            LlamadasHoy++;
            bool completo = false;
            yield return Proveedor.MostrarAnuncio(ok => completo = ok);
            if (completo) eco.DineroUsd += RemesaUsd;
            resultado?.Invoke(completo, completo ? RemesaUsd : 0);
        }
    }
}
