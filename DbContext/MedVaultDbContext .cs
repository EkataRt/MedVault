using MedVaultAPI.Model;
using MedVaultAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Data
{
    public class MedVaultDbContext : DbContext
    {
        public MedVaultDbContext(DbContextOptions<MedVaultDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<HealthProfile> HealthProfiles { get; set; }
        public DbSet<BmiHistory> BmiHistories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Place> Place { get; set; }
        public DbSet<Doctor> Doctor { get; set; }
        public DbSet<MedicineNotification> MedicineNotification { get; set; }

        public DbSet<MedDocument> MedDocument { get; set; }
        public DbSet<Folder> Folder { get; set; }
        public DbSet<MedicalTopic> MedicalTopic { get; set; }

        public DbSet<MedicalMeasurement> MedicalMeasurement { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Password).IsRequired();
            });

            // Appointment
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // HealthProfile
            modelBuilder.Entity<HealthProfile>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.HasOne(h => h.User)
                      .WithMany()
                      .HasForeignKey(h => h.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Medicine
            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Name).IsRequired().HasMaxLength(200);
                entity.HasOne(m => m.User)
                      .WithMany()
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Name).IsRequired().HasMaxLength(200);               
            });
            modelBuilder.Entity<Place>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Name).IsRequired().HasMaxLength(200);
            });
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<MedDocument>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<MedicalTopic>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Topic).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<MedicalMeasurement>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.MeasurementType).IsRequired().HasMaxLength(100);
            });
        }
    }
}