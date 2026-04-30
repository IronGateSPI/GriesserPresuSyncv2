using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GriesserPresuSync.Controllers
{
    /// <summary>
    /// Contexto EF Core unificado.
    /// Contiene tanto presupuestos como mallorquinas (cabecera + líneas + mandos).
    /// </summary>
    public class MiGriesserContext : DbContext
    {
        // === PRESUPUESTOS ===
        public DbSet<IG_Presupuesto_LineaSincronizacion> IG_GriesserSyncPresupuestos { get; set; }

        // === MALLORQUINAS ===
        public DbSet<IG_Mallorquina_Cabecera> IG_GriesserSyncMallorquinas { get; set; }
        public DbSet<IG_Mallorquina_Linea> IG_GriesserSyncMallorquinas_Lineas { get; set; }
        public DbSet<IG_Mallorquina_Mando> IG_GriesserSyncMallorquinas_Mandos { get; set; }

        public MiGriesserContext(DbContextOptions<MiGriesserContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseSqlServer(...)  -> se configura desde Program.cs vía DI
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Mallorquinas: cabecera ===
            modelBuilder.Entity<IG_Mallorquina_Cabecera>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor");
                entity.HasKey(e => e.IdLinea);
                entity.Property(e => e.IdLinea).HasMaxLength(400);
            });

            // === Mallorquinas: líneas ===
            modelBuilder.Entity<IG_Mallorquina_Linea>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor_Lineas");
                entity.HasKey(e => new { e.IdLineaPadre, e.IdLinea });
                entity.Property(e => e.IdLineaPadre).HasMaxLength(400);
                entity.Property(e => e.pos).HasMaxLength(50);

                entity.HasOne<IG_Mallorquina_Cabecera>()
                      .WithMany()
                      .HasForeignKey(e => e.IdLineaPadre)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === Mallorquinas: mandos ===
            modelBuilder.Entity<IG_Mallorquina_Mando>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor_Mandos");
                entity.HasKey(e => new { e.IdLineaPadre, e.IdLinea, e.id_automatismo_mando });
                entity.Property(e => e.IdLineaPadre).HasMaxLength(400);

                entity.HasOne<IG_Mallorquina_Linea>()
                      .WithMany()
                      .HasForeignKey(e => new { e.IdLineaPadre, e.IdLinea })
                      .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }

        // ==========================================================
        // === ENTIDAD: Presupuesto (sin cambios respecto a antes) ===
        // ==========================================================
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
            public string POS { get; set; }
            public float BK { get; set; }
            public float? HL { get; set; }
            public float? GH { get; set; }
            public string Accion { get; set; }
            public float TL { get; set; }
            public int Uni { get; set; }
            public float PUnidad { get; set; }
            public float PUnidad2 { get; set; }
            public float TEUR { get; set; }
            public string Color { get; set; }
            public bool IsSincronized { get; set; }
            // Nuevos Campos Linea
            public string title { get; set; }
            public float price { get; set; }
            public float price_tapa { get; set; }
            public string con_testero { get; set; }
            public float price_testero { get; set; }
            public string tipo { get; set; }
            // Nuevos Campos Presupuesto
            public float? superficie { get; set; }
            public float? importe_color { get; set; }
            public float? importe_tejido { get; set; }
            public float importe_lineas { get; set; }
            public float? importe_tapas_y_testeros { get; set; }
            public float? importe_automatismos { get; set; }
            public float? importe_incrementos { get; set; }
            public float? importe_transporte { get; set; }
            public float importe_total { get; set; }
            public bool weinor_family { get; set; }
            public string accionamiento { get; set; }
            public int i_line { get; set; }
            public string Client_ref { get; set; }
        }

        // ==========================================================
        // === ENTIDADES: Mallorquinas                              ===
        // ==========================================================
        public class IG_Mallorquina_Cabecera
        {
            [Key]
            [MaxLength(400)]
            public string IdLinea { get; set; }
            public int id_budget { get; set; }
            public string cod_client { get; set; }
            public DateTime? date_created { get; set; }
            public string presupuesto { get; set; }
            public DateTime? expiration_date { get; set; }
            public int? id_mallorquina_color { get; set; }
            public string cod_mallorquina_color { get; set; }
            public int? id_mallorquina_acabado { get; set; }
            public string cod_mallorquina_acabado { get; set; }
            public int? num_lines { get; set; }
            public decimal? price_lines { get; set; }
            public int? num_incrementos { get; set; }
            public decimal? price_incrementos { get; set; }
            public decimal? price_color { get; set; }
            public decimal? importe_transporte { get; set; }
            public decimal? importe_instalacion { get; set; }
            public decimal? total { get; set; }
            public bool IsSincronized { get; set; }
        }

        public class IG_Mallorquina_Linea
        {
            [MaxLength(400)]
            public string IdLineaPadre { get; set; }
            public int IdLinea { get; set; }
            [MaxLength(50)]
            public string pos { get; set; }
            public int? id_mallorquina_tipo { get; set; }
            public string cod_mallorquina_tipo { get; set; }
            public int? id_mallorquina_modelo { get; set; }
            public string cod_mallorquina_modelo { get; set; }
            public int? ancho_hueco { get; set; }
            public int? alto_hueco { get; set; }
            public int? id_mallorquina_esquema { get; set; }
            public string cod_mallorquina_esquema { get; set; }
            public int? id_mallorquina_tipologia_instalacion { get; set; }
            public string cod_mallorquina_tipologia_instalacion { get; set; }
            public bool? motorizado { get; set; }
            public int? ancho_hoja { get; set; }
            public int? alto_hoja { get; set; }
            public decimal? precio_por_hoja { get; set; }
            public int? num_hojas { get; set; }
            public decimal? precio_herraje { get; set; }
            public decimal? precio_incrementos { get; set; }
            public decimal? precio_automatismos { get; set; }
            public int? units { get; set; }
            public decimal? total { get; set; }
            public int? id_automatismo_marca { get; set; }
            public string cod_automatismo_marca { get; set; }
            public int? id_automatismo_receptor { get; set; }
            public string cod_automatismo_receptor { get; set; }
            public decimal? precio_receptor { get; set; }
            public decimal? precio_mandos { get; set; }
        }

        public class IG_Mallorquina_Mando
        {
            [MaxLength(400)]
            public string IdLineaPadre { get; set; }
            public int IdLinea { get; set; }
            public int id_automatismo_mando { get; set; }
            public string cod_automatismo_mando { get; set; }
            public decimal? price { get; set; }
            public int? units { get; set; }
            public decimal? total { get; set; }
        }
    }

    /*
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
			public string? Cliente { get; set; }
			public string NPresupuesto { get; set; }
			public int NPersianas { get; set; }
			public float TotalSup { get; set; }
			public float TotalAncho { get; set; }
			public float TotalLargo { get; set; }
			public float LargoTapas { get; set; }
			public int TotalTapas { get; set; }
			//public string Embalaje { get; set; }
			public string POS { get; set; }
			public float BK { get; set; }
			public float? HL { get; set; }
			public float? GH { get; set; }
			public string Accion { get; set; }
			public float TL { get; set; }
			public int Uni { get; set; }
			public float PUnidad { get; set; }
			public float PUnidad2 { get; set; }
			public float TEUR { get; set; }
			//public string POS1 { get; set; }
			public string Color { get; set; }
			public bool IsSincronized { get; set; }
			// Nuevos Campos Linea
			public string title { get; set; }
			public float price { get; set; }
			public float price_tapa { get; set; }
			public string con_testero { get; set; }
			public float price_testero { get; set; }
			public string tipo { get; set; }
			// Nuevos Campos Presupuesto
			public float? superficie { get; set; }
			public float? importe_color { get; set; }
			public float? importe_tejido { get; set; }
			public float importe_lineas { get; set; }
			public float? importe_tapas_y_testeros { get; set; }
			public float? importe_automatismos { get; set; }
			public float? importe_incrementos { get; set; }
			public float? importe_transporte { get; set; }
			public float importe_total { get; set; }
			//weinor family
			public bool weinor_family { get; set; }
			//Modificaciones campos nuevos
			public string accionamiento { get; set; }
			//Campo i_line
			public int i_line { get; set; }
            public string? Client_ref { get; set; }
        }
    }
	*/
}
