using Microsoft.EntityFrameworkCore;
using GriesserPresuSync.Controllers;

namespace GriesserPresuSync.Controllers
{
    // Contexto separado para mallorquinas
    public class MiGriesserMallorquinasContext : DbContext
    {
        public MiGriesserMallorquinasContext(DbContextOptions<MiGriesserMallorquinasContext> options) : base(options)
        {
        }

        public DbSet<MiGriesserMallorquinas.IG_GriesserSyncMallor2> IG_GriesserSyncMallor2 { get; set; }
        public DbSet<MiGriesserMallorquinas.IG_GriesserSyncMallor2_Lineas> IG_GriesserSyncMallor2_Lineas { get; set; }
        public DbSet<MiGriesserMallorquinas.IG_GriesserSyncMallor2_Mandos> IG_GriesserSyncMallor2_Mandos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureMallorquinasEntities(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureMallorquinasEntities(ModelBuilder modelBuilder)
        {
            // Configurar tabla principal
            modelBuilder.Entity<MiGriesserMallorquinas.IG_GriesserSyncMallor2>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor2");
                entity.HasKey(e => e.IdLinea);
                entity.Property(e => e.IdLinea).HasMaxLength(400);
            });

            // Configurar tabla líneas
            modelBuilder.Entity<MiGriesserMallorquinas.IG_GriesserSyncMallor2_Lineas>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor2_Lineas");
                entity.HasKey(e => new { e.IdLineaPadre, e.IdLinea });
                entity.Property(e => e.IdLineaPadre).HasMaxLength(400);
                entity.Property(e => e.pos).HasMaxLength(50);

                entity.HasOne<MiGriesserMallorquinas.IG_GriesserSyncMallor2>()
                      .WithMany()
                      .HasForeignKey(e => e.IdLineaPadre)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar tabla mandos
            modelBuilder.Entity<MiGriesserMallorquinas.IG_GriesserSyncMallor2_Mandos>(entity =>
            {
                entity.ToTable("IG_GriesserSyncMallor2_Mandos");
                entity.HasKey(e => new { e.IdLineaPadre, e.IdLinea, e.id_automatismo_mando });
                entity.Property(e => e.IdLineaPadre).HasMaxLength(400);

                entity.HasOne<MiGriesserMallorquinas.IG_GriesserSyncMallor2_Lineas>()
                      .WithMany()
                      .HasForeignKey(e => new { e.IdLineaPadre, e.IdLinea })
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}