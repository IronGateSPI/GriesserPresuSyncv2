using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using GriesserPresuSync.Models;

namespace GriesserPresuSync.Controllers
{
    public class MiGriesserApiController
    {
        private readonly GriesserSyncSettings _syncSettings;

        static HttpClient client = new HttpClient();

        public MiGriesserApiController(GriesserSyncSettings syncSettings)
        {
            _syncSettings = syncSettings;
        }

        public async Task<T> GetAsync<T>()
        {
            string queryUrl = GetQueryUrl();
            HttpResponseMessage response = await client.GetAsync(queryUrl);
            if(response.IsSuccessStatusCode)
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
            return _syncSettings.ApiUrl + "/?" + queryString.ToString();
        }
    }
}
