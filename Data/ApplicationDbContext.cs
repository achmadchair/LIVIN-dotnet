using Livin.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Livin.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Site> Sites { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<InspectionTask> InspectionTasks { get; set; }
        public DbSet<TaskStandard> TaskStandards { get; set; }
        public DbSet<InspectionRecord> InspectionRecords { get; set; }
        public DbSet<InspectionDetail> InspectionDetails { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure HAC Code to be unique
            modelBuilder.Entity<Equipment>()
                .HasIndex(e => e.HACCode)
                .IsUnique();

            // Explicitly set NoAction/Restrict for all relationships to avoid cycles
            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.Site)
                .WithMany(s => s.Equipments)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Site)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionTask>()
                .HasOne(t => t.Equipment)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskStandard>()
                .HasOne(s => s.InspectionTask)
                .WithMany(t => t.Standards)
                .HasForeignKey(s => s.InspectionTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionRecord>()
                .HasOne(r => r.Site)
                .WithMany()
                .HasForeignKey(r => r.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionRecord>()
                .HasOne(r => r.Equipment)
                .WithMany()
                .HasForeignKey(r => r.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionRecord>()
                .HasOne(r => r.Inspector)
                .WithMany()
                .HasForeignKey(r => r.InspectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionDetail>()
                .HasOne(d => d.InspectionRecord)
                .WithMany(r => r.Details)
                .HasForeignKey(d => d.InspectionRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionDetail>()
                .HasOne(d => d.InspectionTask)
                .WithMany()
                .HasForeignKey(d => d.InspectionTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
