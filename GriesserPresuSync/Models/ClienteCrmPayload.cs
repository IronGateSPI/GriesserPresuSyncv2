using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// DTO con los datos consolidados que el worker envía por PUT al CRM.
    /// Se rellena ejecutando, por cada CodigoCliente pendiente, esta consulta
    /// (la que dio el cliente):
    ///
    ///   SELECT cli.RazonSocial AS nombre,
    ///          cli.cifdni      AS nif,
    ///          cli.CodigoSigla AS tipovia,
    ///          cli.ViaPublica + ' ' + cli.Numero1 + ' ' + cli.Numero2 + ' '
    ///                        + cli.Escalera + ' ' + cli.Piso + ' '
    ///                        + cli.Puerta + ' ' + cli.Letra AS direccion,
    ///          cli.CodigoPostal, cli.Municipio, cli.Provincia,
    ///          fac.Baseanual,
    ///          r.riesgo                            AS descubierto,
    ///          cli.RiesgoMaximo                    AS CyC,
    ///          cli.RiesgoMaximo - r.riesgo         AS CyCDescubierto,
    ///          cli.[%Descuento]                    AS descuento
    ///   FROM   Clientes cli
    ///   OUTER APPLY (SELECT SUM(Baseimponible) AS Baseanual
    ///                FROM ResumenCliente
    ///                WHERE codigoempresa = 1
    ///                  AND codigocliente = cli.codigocliente
    ///                  AND EjercicioFactura = YEAR(GETDATE()) - 1) fac
    ///   OUTER APPLY (SELECT SUM(ImportePendienteDoc) AS riesgo
    ///                FROM ClientesImportesRiesgo
    ///                WHERE codigoempresa = 1
    ///                  AND CodigoCliente = cli.CodigoCliente) r
    ///   WHERE  cli.CodigoEmpresa = @empresa
    ///     AND  cli.CodigoCategoriaCliente_ = 'CLI'
    ///     AND  cli.zzpartner = -1
    ///     AND  cli.CodigoCliente = @codigo;
    ///
    /// Todos los campos son nullable porque la consulta puede devolver NULL
    /// (clientes sin facturación previa, sin riesgo, sin algunos campos de
    /// dirección, etc.). El cliente HTTP convierte null → cadena vacía / 0
    /// según el campo, sin romperse.
    /// </summary>
    public class ClienteCrmPayload
    {
        public string CodigoCliente { get; set; }
        public string Nombre { get; set; }
        public string Nif { get; set; }
        public string TipoVia { get; set; }
        public string Direccion { get; set; }
        public string CodigoPostal { get; set; }
        public string Municipio { get; set; }
        public string Provincia { get; set; }
        public decimal? FacturacionAnual { get; set; }
        public decimal? Descubierto { get; set; }
        public decimal? CyC { get; set; }
        public decimal? CyCDescubierto { get; set; }
        public decimal? Descuento { get; set; }
    }
}
