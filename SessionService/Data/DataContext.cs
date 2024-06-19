using Microsoft.EntityFrameworkCore;
using SessionService.Models;
using SessionService.Services.Test;

namespace SessionService.Data
{
    public class DataContext : DbContext
    {
        private readonly string _connectionString;

        public DataContext(DbContextOptions<DataContext> options, IConfiguration configuration)
            : base(options)
        {
            string hostName = configuration["POSTGRES_HOST_NAME"];
            string username = configuration["POSTGRES_USERNAME"];
            string password = configuration["POSTGRES_PASSWORD"];

            if (string.IsNullOrEmpty(hostName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Database connection information is not fully configured.");
            }

            _connectionString = $"Host={hostName};Port=5432;Database=artist;Username={username};Password={password}";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure your models here
        }

        public DbSet<Artist> artist { get; set; } // Changed property name to follow convention
    }
}
