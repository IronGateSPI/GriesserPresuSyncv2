using System.Reflection;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Xunit;

namespace GriesserPresuSync.Tests
{
    /// <summary>
    /// Tests del ApiController. No tocan red: solo verifican la construcción de la URL
    /// y que id_from se sobreescribe correctamente.
    /// </summary>
    public class MiGriesserMallorquinasApiControllerTests
    {
        private static string InvokeGetQueryUrl(MiGriesserMallorquinasApiController c)
        {
            var m = typeof(MiGriesserMallorquinasApiController)
                .GetMethod("GetQueryUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)m.Invoke(c, null);
        }

        [Fact]
        public void GetQueryUrl_ContieneTodosLosParametros()
        {
            var c = new MiGriesserMallorquinasApiController(new MallorquinasSyncSettings
            {
                ApiUrl = "https://example.com/api",
                details = 1,
                regs_per_page = 50,
                page = 2,
                id_from = 1234
            });

            var url = InvokeGetQueryUrl(c);

            Assert.Contains("details=1", url);
            Assert.Contains("regs_per_page=50", url);
            Assert.Contains("page=2", url);
            Assert.Contains("id_from=1234", url);
            Assert.StartsWith("https://example.com/api/?", url);
        }

        [Fact]
        public void ApiUrlConBarraFinal_NoDuplicaSeparador()
        {
            var c = new MiGriesserMallorquinasApiController(new MallorquinasSyncSettings
            {
                ApiUrl = "https://example.com/api/" // con / final
            });
            var url = InvokeGetQueryUrl(c);
            Assert.StartsWith("https://example.com/api/?", url);
            Assert.DoesNotContain("//?", url.Replace("https://", ""));
        }
    }
}
