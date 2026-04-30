using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static GriesserPresuSync.Controllers.MiGriesserContext;

namespace GriesserPresuSync.Tests
{
    /// <summary>
    /// Tests de robustez para los métodos GetLastSyncID y los chequeos de
    /// existencia (saveLine / guardaMallorquina).
    ///
    /// Background: en producción se observó SqlNullValueException al iterar
    /// resultados del DbContext. Causa: las consultas materializaban la
    /// entidad completa (ToListAsync / SingleOrDefaultAsync) y alguna columna
    /// no anulable contenía NULL en BD.
    ///
    /// Solución: usar proyección con MaxAsync e idempotencia con AnyAsync.
    /// EF traduce a SELECT MAX(...) y EXISTS(...) sin hidratar la entidad.
    ///
    /// Estos tests cubren:
    ///   1) Contrato funcional (vacío → -1, varias filas → MAX)
    ///   2) Idempotencia sin duplicar
    ///   3) Anti-regresión por análisis estático del fichero fuente
    ///      (si alguien vuelve a meter ToListAsync/SingleOrDefaultAsync el
    ///      test cae en el acto).
    /// </summary>
    public class GetLastSyncIDTests
    {
        private static (IServiceProvider sp, MiGriesserContext db) BuildSp(string dbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<MiGriesserContext>(opts => opts.UseInMemoryDatabase(dbName));
            var sp = services.BuildServiceProvider();
            var db = sp.GetRequiredService<MiGriesserContext>();
            db.Database.EnsureCreated();
            return (sp, db);
        }

        private static Task<int> InvokeGetLastSyncID_Mallor(GriesserSyncMallorController ctrl)
        {
            var m = typeof(GriesserSyncMallorController)
                .GetMethod("GetLastSyncID", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task<int>)m.Invoke(ctrl, null);
        }

        private static Task<int> InvokeGetLastSyncID_Presu(GriesserSyncPresuController ctrl)
        {
            var m = typeof(GriesserSyncPresuController)
                .GetMethod("GetLastSyncID", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task<int>)m.Invoke(ctrl, null);
        }

        // -----------------------------------------------------------------
        // Mallorquinas
        // -----------------------------------------------------------------

        [Fact]
        public async Task Mallor_GetLastSyncID_TablaVacia_DevuelveMenosUno()
        {
            var (sp, _) = BuildSp(nameof(Mallor_GetLastSyncID_TablaVacia_DevuelveMenosUno));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var result = await InvokeGetLastSyncID_Mallor(ctrl);
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task Mallor_GetLastSyncID_VariasFilas_DevuelveMaximo()
        {
            var (sp, db) = BuildSp(nameof(Mallor_GetLastSyncID_VariasFilas_DevuelveMaximo));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            db.IG_GriesserSyncMallorquinas.AddRange(
                new IG_Mallorquina_Cabecera { IdLinea = "MALLOR_10", id_budget = 10 },
                new IG_Mallorquina_Cabecera { IdLinea = "MALLOR_55", id_budget = 55 },
                new IG_Mallorquina_Cabecera { IdLinea = "MALLOR_22", id_budget = 22 });
            db.SaveChanges();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var result = await InvokeGetLastSyncID_Mallor(ctrl);
            Assert.Equal(55, result);
        }

        [Fact]
        public async Task Mallor_GetLastSyncID_UnaFila_DevuelveEseId()
        {
            var (sp, db) = BuildSp(nameof(Mallor_GetLastSyncID_UnaFila_DevuelveEseId));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            db.IG_GriesserSyncMallorquinas.Add(
                new IG_Mallorquina_Cabecera { IdLinea = "MALLOR_777", id_budget = 777 });
            db.SaveChanges();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var result = await InvokeGetLastSyncID_Mallor(ctrl);
            Assert.Equal(777, result);
        }

        // -----------------------------------------------------------------
        // Presupuestos
        // -----------------------------------------------------------------

        [Fact]
        public async Task Presu_GetLastSyncID_TablaVacia_DevuelveMenosUno()
        {
            var (sp, _) = BuildSp(nameof(Presu_GetLastSyncID_TablaVacia_DevuelveMenosUno));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncPresuController(
                new MiGriesserApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var result = await InvokeGetLastSyncID_Presu(ctrl);
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task Presu_GetLastSyncID_VariasFilas_DevuelveMaximo()
        {
            var (sp, db) = BuildSp(nameof(Presu_GetLastSyncID_VariasFilas_DevuelveMaximo));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            db.IG_GriesserSyncPresupuestos.AddRange(
                new IG_Presupuesto_LineaSincronizacion { IdLinea = "P-1", IdBudget = 100 },
                new IG_Presupuesto_LineaSincronizacion { IdLinea = "P-2", IdBudget = 350 },
                new IG_Presupuesto_LineaSincronizacion { IdLinea = "P-3", IdBudget = 200 });
            db.SaveChanges();

            var ctrl = new GriesserSyncPresuController(
                new MiGriesserApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var result = await InvokeGetLastSyncID_Presu(ctrl);
            Assert.Equal(350, result);
        }

        // -----------------------------------------------------------------
        // Idempotencia / comprobación de existencia con AnyAsync
        // -----------------------------------------------------------------

        [Fact]
        public async Task Mallor_NoVuelveAGuardar_SiCabeceraYaExiste()
        {
            var (sp, db) = BuildSp(nameof(Mallor_NoVuelveAGuardar_SiCabeceraYaExiste));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            // Pre-cargamos la cabecera. Lo importante: la idempotencia debe
            // detectar que ya existe SIN materializar la entidad (AnyAsync).
            db.IG_GriesserSyncMallorquinas.Add(new IG_Mallorquina_Cabecera
            {
                IdLinea = "MALLOR_4242",
                id_budget = 4242
            });
            db.SaveChanges();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var budget = new MallorquinaBudget
            {
                id_budget = 4242,
                presupuesto = "PRES-4242",
                line_n = new[]
                {
                    new MallorquinaLinea { pos = "P1", units = 1 }
                }
            };

            var method = typeof(GriesserSyncMallorController)
                .GetMethod("guardaMallorquina", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method.Invoke(ctrl, new object[] { budget });

            // No debe duplicar la cabecera ni añadir líneas (se cortocircuita).
            using var verifyScope = scopeFactory.CreateScope();
            var verify = verifyScope.ServiceProvider.GetRequiredService<MiGriesserContext>();
            Assert.Equal(1, verify.IG_GriesserSyncMallorquinas.Count());
            Assert.Equal(0, verify.IG_GriesserSyncMallorquinas_Lineas.Count());
        }

        [Fact]
        public async Task Presu_SaveLine_NoDuplicaSiYaExiste()
        {
            var (sp, db) = BuildSp(nameof(Presu_SaveLine_NoDuplicaSiYaExiste));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            db.IG_GriesserSyncPresupuestos.Add(new IG_Presupuesto_LineaSincronizacion
            {
                IdLinea = "P-DUP-1",
                IdBudget = 999
            });
            db.SaveChanges();

            var ctrl = new GriesserSyncPresuController(
                new MiGriesserApiController(),
                NullLogger<GetLastSyncIDTests>.Instance,
                scopeFactory);

            var saveLine = typeof(GriesserSyncPresuController).GetMethod(
                "saveLine", BindingFlags.NonPublic | BindingFlags.Instance);

            var dup = new IG_Presupuesto_LineaSincronizacion { IdLinea = "P-DUP-1", IdBudget = 999 };
            await (Task<IG_Presupuesto_LineaSincronizacion>)saveLine.Invoke(ctrl, new object[] { dup });

            using var verifyScope = scopeFactory.CreateScope();
            var verify = verifyScope.ServiceProvider.GetRequiredService<MiGriesserContext>();
            Assert.Equal(1, verify.IG_GriesserSyncPresupuestos.Count());
        }

        // -----------------------------------------------------------------
        // Guarda anti-regresión: análisis estático del código fuente.
        // Si alguien vuelve a meter ToListAsync/SingleOrDefaultAsync en
        // los métodos críticos, el test cae sin necesidad de reproducir
        // el fallo en BD. Es lectura literal del fichero, no IL, para que
        // sea fiable y sin dependencias externas.
        // -----------------------------------------------------------------

        private static string LeerFuente(string nombreArchivo)
        {
            // Subimos por el árbol de directorios desde el bin de tests hasta
            // localizar la carpeta del solution que contiene el archivo fuente.
            // Excluimos bin/obj para evitar copias compiladas o duplicados.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var hit = dir.EnumerateFiles(nombreArchivo, SearchOption.AllDirectories)
                    .Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                             && !f.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    .FirstOrDefault();
                if (hit != null) return File.ReadAllText(hit.FullName);
                dir = dir.Parent;
            }
            throw new FileNotFoundException(
                $"No se ha podido localizar {nombreArchivo} desde {AppContext.BaseDirectory}");
        }

        [Fact]
        public void AntiRegresion_PresuController_NoUsaToListAsyncNiSingleOrDefaultAsync()
        {
            var src = LeerFuente("GriesserSyncPresuController.cs");
            Assert.DoesNotContain("ToListAsync", src);
            Assert.DoesNotContain("SingleOrDefaultAsync", src);
        }

        [Fact]
        public void AntiRegresion_MallorController_NoUsaToListAsyncNiSingleOrDefaultAsync()
        {
            var src = LeerFuente("GriesserSyncMallorController.cs");
            Assert.DoesNotContain("ToListAsync", src);
            Assert.DoesNotContain("SingleOrDefaultAsync", src);
        }
    }
}
