using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GriesserPresuSync.Models;
using Newtonsoft.Json;

namespace GriesserPresuSync.Controllers
{
    /// <summary>
    /// Cliente HTTP para el CRM Griesser. Espejo de MiGriesser*ApiController pero
    /// con dos diferencias importantes:
    ///
    ///   1) El verbo principal es PUT (no GET) con body
    ///      application/x-www-form-urlencoded — el CRM espera form fields, no JSON.
    ///
    ///   2) El código del cliente va en la URL (path), no como query string:
    ///        PUT {ApiUrl}/{codigoCliente}/sync_sage_distributor
    ///
    /// El HttpClient se mantiene estático (recomendación oficial de Microsoft
    /// para evitar agotar el pool de sockets — el mismo patrón que usa el resto
    /// del servicio).
    /// </summary>
    public class ClientesCrmApiController
    {
        // HttpClient compartido entre instancias y entre llamadas.
        // Se construye una vez por proceso; el timeout sí lo aplicamos por
        // request a través de CancellationToken — no tocamos client.Timeout
        // global porque eso obligaría a cargarlo antes de la primera llamada.
        private static readonly HttpClient _client = new HttpClient();

        private readonly ClientesCrmSyncSettings _settings;

        public ClientesCrmApiController() : this(new ClientesCrmSyncSettings()) { }

        public ClientesCrmApiController(ClientesCrmSyncSettings settings)
        {
            _settings = settings ?? new ClientesCrmSyncSettings();
        }

        // ====================================================================
        // EXPORT: PUT /es/{codigoCliente}/sync_sage_distributor
        // ====================================================================

        /// <summary>
        /// Envía al CRM los datos de un cliente vía PUT form-urlencoded.
        /// Devuelve un par (ok, payloadEnviado, errorMsg). Nunca lanza
        /// excepción: el controlador llamante decide si reintentar.
        /// </summary>
        public async Task<EnvioResult> PutClienteAsync(ClienteCrmPayload p)
        {
            if (p == null)
                return new EnvioResult { Ok = false, Error = "Payload null" };

            if (string.IsNullOrWhiteSpace(p.CodigoCliente))
                return new EnvioResult { Ok = false, Error = "CodigoCliente vacío" };

            // Construye el body. Mapeo según el curl que dio el cliente:
            //   sync_sage_distributor (sí, el field se llama así) -> nombre partner
            //   nif, via_tipo, address, zip_code, geo_municipality, geo_province,
            //   facturacion_anual, descubierto, credito_y_caucion,
            //   credito_y_caucion_descubierto, descuento
            var form = new Dictionary<string, string>
            {
                ["sync_sage_distributor"] = Safe(p.Nombre),
                ["nif"]                   = Safe(p.Nif),
                ["via_tipo"]              = Safe(p.TipoVia),
                ["address"]               = Safe(p.Direccion),
                ["zip_code"]              = Safe(p.CodigoPostal),
                ["geo_municipality"]      = Safe(p.Municipio),
                ["geo_province"]          = Safe(p.Provincia),
                ["facturacion_anual"]     = Money(p.FacturacionAnual),
                ["descubierto"]           = Money(p.Descubierto),
                ["credito_y_caucion"]     = Money(p.CyC),
                ["credito_y_caucion_descubierto"] = Money(p.CyCDescubierto),
                ["descuento"]             = Money(p.Descuento)
            };

            // Snapshot textual para auditoría (se guardará en PayloadEnviado).
            // No es la única fuente de verdad — es debug-friendly — porque
            // FormUrlEncodedContent ya hace su propio escape al enviar.
            var sb = new StringBuilder();
            foreach (var kv in form)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(kv.Key).Append('=').Append(kv.Value);
            }
            var snapshot = sb.ToString();

            try
            {
                var url = BuildPutUrl(p.CodigoCliente);
                using (var content = new FormUrlEncodedContent(form))
                {
                    // Timeout por request usando CancellationToken
                    using (var cts = new System.Threading.CancellationTokenSource(
                        TimeSpan.FromSeconds(Math.Max(1, _settings.HttpTimeoutSeconds))))
                    {
                        var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
                        var resp = await _client.SendAsync(req, cts.Token).ConfigureAwait(false);

                        var body = resp.Content != null
                            ? await resp.Content.ReadAsStringAsync().ConfigureAwait(false)
                            : string.Empty;

                        if (resp.IsSuccessStatusCode)
                        {
                            return new EnvioResult
                            {
                                Ok = true,
                                Payload = snapshot,
                                ResponseBody = body
                            };
                        }

                        return new EnvioResult
                        {
                            Ok = false,
                            Payload = snapshot,
                            ResponseBody = body,
                            Error = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}"
                        };
                    }
                }
            }
            catch (TaskCanceledException tex)
            {
                return new EnvioResult { Ok = false, Payload = snapshot, Error = $"Timeout: {tex.Message}" };
            }
            catch (HttpRequestException hex)
            {
                return new EnvioResult { Ok = false, Payload = snapshot, Error = $"HttpRequestException: {hex.Message}" };
            }
            catch (Exception ex)
            {
                return new EnvioResult { Ok = false, Payload = snapshot, Error = ex.Message };
            }
        }

        // ====================================================================
        // IMPORT: GET /es/all/sync_sage_distributor
        // ====================================================================

        /// <summary>
        /// Descarga la lista completa de partners desde el CRM. Es el job nocturno.
        /// Devuelve una lista vacía si la respuesta es null o no parseable, sin lanzar.
        /// </summary>
        public async Task<List<ClienteCrmImportItem>> GetAllAsync()
        {
            try
            {
                var url = BuildGetAllUrl();
                using (var cts = new System.Threading.CancellationTokenSource(
                    TimeSpan.FromSeconds(Math.Max(5, _settings.HttpTimeoutSeconds))))
                {
                    var resp = await _client.GetAsync(url, cts.Token).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        return new List<ClienteCrmImportItem>();

                    var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                        return new List<ClienteCrmImportItem>();

                    var arr = JsonConvert.DeserializeObject<List<ClienteCrmImportItem>>(json);
                    return arr ?? new List<ClienteCrmImportItem>();
                }
            }
            catch
            {
                // El controlador llamante loguea el contexto completo;
                // aquí mantenemos la firma simple (devolver lista vacía
                // es una salida segura para que el worker no aborte).
                return new List<ClienteCrmImportItem>();
            }
        }

        // ====================================================================
        // Helpers privados
        // ====================================================================

        private string BuildPutUrl(string codigoCliente)
        {
            var baseUrl = (_settings.ApiUrl ?? "").TrimEnd('/');
            // El CodigoCliente de Sage normalmente es numérico; aun así
            // se escapa por seguridad (espacios, caracteres especiales).
            var cod = Uri.EscapeDataString(codigoCliente.Trim());
            return $"{baseUrl}/{cod}/sync_sage_distributor";
        }

        private string BuildGetAllUrl()
        {
            var baseUrl = (_settings.ApiUrl ?? "").TrimEnd('/');
            return $"{baseUrl}/all/sync_sage_distributor";
        }

        /// <summary>Convierte null a cadena vacía y trimma.</summary>
        private static string Safe(string s) => (s ?? string.Empty).Trim();

        /// <summary>
        /// Formatea importes/decimales con punto decimal (cultura invariante)
        /// para que el CRM no se encuentre comas españolas.
        /// Si el valor es null devuelve "0" (el CRM espera siempre el field).
        /// </summary>
        private static string Money(decimal? d)
        {
            if (!d.HasValue) return "0";
            return d.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>Resultado de un PUT al CRM.</summary>
        public class EnvioResult
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public string Payload { get; set; }
            public string ResponseBody { get; set; }
        }
    }
}
