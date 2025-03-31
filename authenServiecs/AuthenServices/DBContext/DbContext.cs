using AuthenController.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
namespace AuthenServices.DBContext
{
    public class AuthenDb : DbContext
    {
        public AuthenDb(DbContextOptions<AuthenDb> options ) : base( options ){}

        private readonly string _dbPath;

        public AuthenDb(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                          .LogTo(x => Log.Logger.Debug(x),
                                    events: new[]
                                    {
                                        RelationalEventId.CommandExecuted,
                                    })
                          .EnableDetailedErrors()
                          .EnableSensitiveDataLogging();

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            }
        }

        public DbSet<User> users { get; set; }
    }
}