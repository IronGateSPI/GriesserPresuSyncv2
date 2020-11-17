using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GriesserPresuSync.Controllers
{
    public class MiGriesserContext : DbContext
	{

		public DbSet<IG_Presupuesto_LineaSincronizacion> IG_GriesserSyncPresupuestos { get; set;}



		public MiGriesserContext(DbContextOptions<MiGriesserContext> options) : base(options)
        {

        }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			//optionsBuilder.UseSqlServer(MiGriesserDataStore.Instance.SyncSettings.SageConnectionString);
		}

		public class IG_Presupuesto_LineaSincronizacion
        {
			[Key]
			public string IdLinea { get; set; }
			public int IdBudget { get; set; }
			public DateTime date { get; set; }
			public string Articulo { get; set; }
			public string Cliente { get; set; }
			public string NPresupuesto { get; set; }
			public int NPersianas { get; set; }
			public float TotalSup { get; set; }
			public float TotalAncho { get; set; }
			public float TotalLargo { get; set; }
			public float LargoTapas { get; set; }
			public int TotalTapas { get; set; }
			public string Embalaje { get; set; }
			public int POS { get; set; }
			public float BK { get; set; }
			public float HL { get; set; }

			public string Accion { get; set; }
			public float TL { get; set; }
			public int Uni { get; set; }
			public float PUnidad { get; set; }
			public float PUnidad2 { get; set; }
			public float TEUR { get; set; }
			public string POS1 { get; set; }
			public string Color { get; set; }
			public bool IsSincronized { get; set; }
		}
    }
}
