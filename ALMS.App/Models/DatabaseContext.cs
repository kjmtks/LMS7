using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using ALMS.App.Models.Entities;

namespace ALMS.App.Models
{

    public static class DbInitializer
    {
        public static void Initialize(DatabaseContext context)
        {
            context.Database.EnsureCreated();
        }
    }

    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<SandboxTemplate> SandboxTemplates { get; set; }
        public DbSet<LectureSandbox> LectureSandboxes { get; set; }
        public DbSet<Sandbox> Sandboxes { get; set; }
        public DbSet<LectureUser> LectureUsers { get; set; }
        public DbSet<ActivityActionHistory> ActivityActionHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<User>().HasKey(x => x.Id);
            modelBuilder.Entity<User>().HasIndex(x => x.Account).IsUnique();
            modelBuilder.Entity<User>().HasMany(x => x.Lectures);
            modelBuilder.Entity<User>().HasMany(x => x.Sandboxes);
            modelBuilder.Entity<User>().HasMany(x => x.LectureUsers).WithOne(x => x.User).HasForeignKey(x => x.UserId);

            modelBuilder.Entity<SandboxTemplate>().ToTable("SandboxTemplates");
            modelBuilder.Entity<SandboxTemplate>().HasKey(x => x.Id);
            modelBuilder.Entity<SandboxTemplate>().HasIndex(x => x.Name).IsUnique();

            modelBuilder.Entity<Sandbox>().ToTable("Sandboxes");
            modelBuilder.Entity<Sandbox>().HasKey(x => x.Id);
            modelBuilder.Entity<Sandbox>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<Sandbox>().HasOne(x => x.Owner);

            modelBuilder.Entity<Lecture>().ToTable("Lecture");
            modelBuilder.Entity<Lecture>().HasKey(x => x.Id);
            modelBuilder.Entity<Lecture>().HasIndex(x => new { x.Name, x.OwnerId } ).IsUnique();
            modelBuilder.Entity<Lecture>().HasOne(x => x.Owner);
            modelBuilder.Entity<Lecture>().HasMany(x => x.Sandboxes);
            modelBuilder.Entity<Lecture>().HasMany(x => x.LectureUsers).WithOne(x => x.Lecture).HasForeignKey(x => x.LectureId);

            modelBuilder.Entity<LectureSandbox>().ToTable("LectureSandboxes");
            modelBuilder.Entity<LectureSandbox>().HasKey(x => x.Id);
            modelBuilder.Entity<LectureSandbox>().HasIndex(x => new { x.Name, x.LectureId} ).IsUnique();
            modelBuilder.Entity<LectureSandbox>().HasOne(x => x.Lecture);

            modelBuilder.Entity<LectureUser>().ToTable("LectureUser");
            modelBuilder.Entity<LectureUser>().HasKey(a => new { a.UserId, a.LectureId, a.Role });
            modelBuilder.Entity<LectureUser>().HasOne(a => a.Lecture).WithMany(l => l.LectureUsers).HasForeignKey(a => a.LectureId);
            modelBuilder.Entity<LectureUser>().HasOne(a => a.User).WithMany(u => u.LectureUsers).HasForeignKey(a => a.UserId);

            modelBuilder.Entity<ActivityActionHistory>().ToTable("ActivityActionHistory");
            modelBuilder.Entity<ActivityActionHistory>().HasKey(x => x.Id);
            modelBuilder.Entity<ActivityActionHistory>().HasOne(x => x.User);
            modelBuilder.Entity<ActivityActionHistory>().HasOne(x => x.Lecture);
        }
    }
}
