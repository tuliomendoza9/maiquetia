using Maiquetia.Viajeros;

namespace Maiquetia.Reglas
{
    public class ResultadoDecision
    {
        public bool Correcta;
        public Decision DecisionCorrecta;
        public string Explicacion;   // para la boleta de multa / feedback
    }

    /// <summary>
    /// La única fuente de verdad sobre qué decisión era la correcta.
    /// Regla general: discrepancia => NEGADO, salvo lista de alerta => ALCABALA.
    /// Sin discrepancia => SIGA.
    /// </summary>
    public class ValidadorDecision
    {
        private readonly LibroReglas _reglas;

        public ValidadorDecision(LibroReglas reglas) { _reglas = reglas; }

        public Decision Correcta(Viajero v, int dia)
        {
            // La lista de alerta manda sobre todo lo demás
            if (v.Discrepancia == TipoDiscrepancia.EnListaAlerta)
                return Decision.Alcabala;

            if (v.Discrepancia == TipoDiscrepancia.Ninguna)
                return Decision.Siga;

            // Discrepancias solo punibles si la regla del día ya está activa;
            // si la regla aún no existe (p.ej. visa antes del día 3), pasa.
            return _reglas.DiscrepanciaActiva(dia, v.Discrepancia)
                ? Decision.Negado
                : Decision.Siga;
        }

        public ResultadoDecision Evaluar(Viajero v, Decision tomada, int dia)
        {
            var correcta = Correcta(v, dia);
            v.DecisionCorrecta = correcta;
            return new ResultadoDecision
            {
                Correcta = tomada == correcta,
                DecisionCorrecta = correcta,
                Explicacion = Explicar(v, correcta),
            };
        }

        private string Explicar(Viajero v, Decision correcta)
        {
            switch (v.Discrepancia)
            {
                case TipoDiscrepancia.Ninguna:
                    return "Los documentos estaban en regla. Debía SEGUIR.";
                case TipoDiscrepancia.NombreNoCoincide:
                    return "El nombre del boleto no coincidía con el pasaporte.";
                case TipoDiscrepancia.PasaporteVencido:
                    return "Pasaporte vencido sin prórroga.";
                case TipoDiscrepancia.ProrrogaVencida:
                    return "La prórroga del pasaporte estaba vencida (revise las fechas).";
                case TipoDiscrepancia.FotoNoCoincide:
                    return "La foto del pasaporte no correspondía con el viajero.";
                case TipoDiscrepancia.CedulaAlterada:
                    return "La cédula difería entre documentos: documento alterado.";
                case TipoDiscrepancia.SinVisaRequerida:
                    return "El destino exigía visa y el viajero no la presentó.";
                case TipoDiscrepancia.MenorSinPermiso:
                    return "Menor de edad sin permiso notariado.";
                case TipoDiscrepancia.BoletoOtraFecha:
                    return "El boleto no era para el día de hoy.";
                case TipoDiscrepancia.EnListaAlerta:
                    return "El viajero estaba en la LISTA DE ALERTA: correspondía ALCABALA.";
                case TipoDiscrepancia.SinCertFiebre:
                    return "El destino exigía certificado de fiebre amarilla.";
                default:
                    return "";
            }
        }
    }
}
