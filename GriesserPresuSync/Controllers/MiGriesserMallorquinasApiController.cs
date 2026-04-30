using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using GriesserPresuSync.Models;

namespace GriesserPresuSync.Controllers
{
    /// <summary>
    /// Cliente HTTP genérico para el endpoint de mallorquinas.
    /// Espejo de MiGriesserApiController (presupuestos).
    /// </summary>
    public class MiGriesserMallorquinasApiController
    {
        static HttpClient client = new HttpClient();
        private MallorquinasSyncSettings _syncSettings;

        public MiGriesserMallorquinasApiController()
        {
            _syncSettings = new MallorquinasSyncSettings();
        }

        public MiGriesserMallorquinasApiController(MallorquinasSyncSettings settings)
        {
            _syncSettings = settings ?? new MallorquinasSyncSettings();
        }

        public async Task<T> GetAsync<T>(int initialId = -1)
        {
            if (initialId != -1)
                _syncSettings.id_from = initialId;

            string queryUrl = GetQueryUrl();
            HttpResponseMessage response = await client.GetAsync(queryUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<T>();
            }
            return default(T);
        }

        private string GetQueryUrl()
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            queryString.Add("details", _syncSettings.details.ToString());
            queryString.Add("regs_per_page", _syncSettings.regs_per_page.ToString());
            queryString.Add("page", _syncSettings.page.ToString());
            queryString.Add("id_from", _syncSettings.id_from.ToString());

            // El endpoint puede venir con o sin "/" final
            var baseUrl = _syncSettings.ApiUrl.TrimEnd('/');
            return baseUrl + "/?" + queryString.ToString();
        }
    }
}
