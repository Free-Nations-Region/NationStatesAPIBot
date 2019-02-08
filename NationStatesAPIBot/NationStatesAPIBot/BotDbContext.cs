using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using System.IO;
using System.Linq;

namespace NationStatesAPIBot
{
    public class BotDbContext : DbContext
    {
        public DbSet<Nation> Nations { get; set; }
        public DbSet<NationStatus> NationStatuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(GetConnectionString());
        }

        private string GetConnectionString()
        {
            if (File.Exists("keys.config"))
            {
                var lines = File.ReadAllLines("keys.config").ToList();
                if (lines.Exists(cl => cl.StartsWith("dbConnection=")))
                {
                    var line = lines.FirstOrDefault(cl => cl.StartsWith("dbConnection="));
                    var dbString = line.Remove(0, "dbConnection=".Length);
                    return dbString;
                }
                else
                {
                    throw new InvalidDataException("The 'keys.config' does not contain a dbConnection string");
                }
            }
            else
            {
                throw new FileNotFoundException("The 'keys.config' file could not be found.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermissions>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            modelBuilder.Entity<UserPermissions>()
                .HasOne(up => up.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPermissions>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);
        }
    }
}
