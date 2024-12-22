using System;
using Microsoft.EntityFrameworkCore;
using EaststarServiceAPI.HttpListeners;
using EaststarServiceAPI.Agents;
using EaststarServiceAPI.Operators;
using EaststarServiceAPI.Tasking;

namespace EaststarServiceAPI
{
    public class EaststarContext : DbContext
    {
        public DbSet<HttpListenersInfo> HttpListenersInfo { get; set; }
        public DbSet<OperatorsInfo> OperatorsInfo { get; set; }
        public DbSet<AgentsInfo> AgentsInfo { get; set; }
        public DbSet<TaskOutput> TaskOutput { get; set; }
        public DbSet<Tasks> Tasks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=eaststar.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HttpListenersInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Host).IsRequired();
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.Headers);
                entity.Property(e => e.Active).IsRequired();
            });

            modelBuilder.Entity<OperatorsInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Password).HasMaxLength(100);
                entity.Property(e => e.AgentId);
            });

            modelBuilder.Entity<AgentsInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.OperatingSystem).HasMaxLength(50);
                entity.Property(e => e.Architecture).HasMaxLength(50);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Hostname).HasMaxLength(100);
                entity.Property(e => e.Domain).HasMaxLength(100);
                entity.Property(e => e.LastPing);
                entity.Property(e => e.ProcessName).HasMaxLength(100);
                entity.Property(e => e.ProcessId);
                entity.Property(e => e.IPv4).HasMaxLength(15);
            });
        }
    }

    public class DatabaseConnection
    {
        public DatabaseConnection GetDatabaseContext()
        {
            return new DatabaseConnection();
        }
    }
}
