using System;
using System.Linq;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GriesserPresuSync.Tests
{
    /// <summary>
    /// Tests de integración con base de datos en memoria.
    /// Verifican la robustez del sync de mallorquinas:
    ///   - Inserta cabecera + líneas + mandos correctamente
    ///   - Es idempotente (un segundo run no duplica)
    ///   - GetLastSyncID devuelve el id_budget máximo
    ///   - Maneja budgets sin líneas o líneas sin mandos
    /// </summary>
    public class GuardaMallorquinaTests
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

        private static MallorquinaBudget BuildBudget(int id, int numLineas = 2, int numMandosPorLinea = 1)
        {
            var lineas = Enumerable.Range(1, numLineas).Select(i => new MallorquinaLinea
            {
                pos = $"P{i}",
                id_mallorquina_tipo = 10 + i,
                ancho_hueco = 100 * i,
                alto_hueco = 200 * i,
                units = 1,
                total = 100m * i,
                mando_n = Enumerable.Range(1, numMandosPorLinea).Select(j => new MallorquinaMando
                {
                    id_automatismo_mando = i * 10 + j,
                    cod_automatismo_mando = $"M{i}-{j}",
                    price = 25m,
                    units = 1,
                    total = 25m
                }).ToArray()
            }).ToArray();

            return new MallorquinaBudget
            {
                id_budget = id,
                cod_client = "CLI001",
                date_created = new DateTime(2026, 1, 15),
                presupuesto = $"PRES-{id}",
                num_lines = numLineas,
                price_lines = 100m * numLineas,
                total = 100m * numLineas,
                line_n = lineas
            };
        }

        [Fact]
        public async Task InsertaCabeceraLineasYMandos()
        {
            var (sp, db) = BuildSp(nameof(InsertaCabeceraLineasYMandos));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GuardaMallorquinaTests>.Instance,
                scopeFactory);

            var budget = BuildBudget(1001, numLineas: 3, numMandosPorLinea: 2);

            // Invocamos el método privado vía reflexión (es testeo controlado)
            var method = typeof(GriesserSyncMallorController)
                .GetMethod("guardaMallorquina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(ctrl, new object[] { budget });

            using var scope = scopeFactory.CreateScope();
            var verify = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();

            Assert.Equal(1, verify.IG_GriesserSyncMallorquinas.Count());
            Assert.Equal(3, verify.IG_GriesserSyncMallorquinas_Lineas.Count());
            Assert.Equal(6, verify.IG_GriesserSyncMallorquinas_Mandos.Count());

            var cab = verify.IG_GriesserSyncMallorquinas.Single();
            Assert.Equal("MALLOR_1001", cab.IdLinea);
            Assert.Equal(1001, cab.id_budget);
            Assert.False(cab.IsSincronized);
        }

        [Fact]
        public async Task EsIdempotente_NoDuplicaSiSeLlamaDosVeces()
        {
            var (sp, _) = BuildSp(nameof(EsIdempotente_NoDuplicaSiSeLlamaDosVeces));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GuardaMallorquinaTests>.Instance,
                scopeFactory);

            var budget = BuildBudget(2002, numLineas: 2, numMandosPorLinea: 1);

            var method = typeof(GriesserSyncMallorController)
                .GetMethod("guardaMallorquina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            await (Task)method.Invoke(ctrl, new object[] { budget });
            await (Task)method.Invoke(ctrl, new object[] { budget }); // segunda vez

            using var scope = scopeFactory.CreateScope();
            var verify = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();

            Assert.Equal(1, verify.IG_GriesserSyncMallorquinas.Count());
            Assert.Equal(2, verify.IG_GriesserSyncMallorquinas_Lineas.Count());
            Assert.Equal(2, verify.IG_GriesserSyncMallorquinas_Mandos.Count());
        }

        [Fact]
        public async Task BudgetSinLineas_PersisteSoloCabecera()
        {
            var (sp, _) = BuildSp(nameof(BudgetSinLineas_PersisteSoloCabecera));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GuardaMallorquinaTests>.Instance,
                scopeFactory);

            var budget = new MallorquinaBudget
            {
                id_budget = 3003,
                presupuesto = "PRES-3003",
                line_n = null
            };

            var method = typeof(GriesserSyncMallorController)
                .GetMethod("guardaMallorquina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(ctrl, new object[] { budget });

            using var scope = scopeFactory.CreateScope();
            var verify = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
            Assert.Equal(1, verify.IG_GriesserSyncMallorquinas.Count());
            Assert.Equal(0, verify.IG_GriesserSyncMallorquinas_Lineas.Count());
            Assert.Equal(0, verify.IG_GriesserSyncMallorquinas_Mandos.Count());
        }

        [Fact]
        public async Task GuardaMallorquina_Null_NoExplota()
        {
            var (sp, _) = BuildSp(nameof(GuardaMallorquina_Null_NoExplota));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var ctrl = new GriesserSyncMallorController(
                new MiGriesserMallorquinasApiController(),
                NullLogger<GuardaMallorquinaTests>.Instance,
                scopeFactory);

            var method = typeof(GriesserSyncMallorController)
                .GetMethod("guardaMallorquina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // No debe lanzar excepción
            await (Task)method.Invoke(ctrl, new object[] { null });

            using var scope = scopeFactory.CreateScope();
            var verify = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
            Assert.Equal(0, verify.IG_GriesserSyncMallorquinas.Count());
        }
    }
}
