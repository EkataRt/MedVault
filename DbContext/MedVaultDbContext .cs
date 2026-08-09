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
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Place> Place { get; set; }
        public DbSet<Doctor> Doctor { get; set; }
        public DbSet<MedicineNotification> MedicineNotification { get; set; }

        public DbSet<MedDocument> MedDocument { get; set; }
        public DbSet<Folder> Folder { get; set; }


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
            // Folder
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(f => f.UserId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(f => f.CreatedAt)
                      .IsRequired();

                entity.Property(f => f.ParentId)
                      .HasMaxLength(100);
            });

            // MedDocument
            modelBuilder.Entity<MedDocument>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(d => d.FileName)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(d => d.Url)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(d => d.FolderId)
                      .HasMaxLength(100);

                entity.Property(d => d.UserId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(d => d.CreatedAt)
                      .IsRequired();
            });
        }
    }
}