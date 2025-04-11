using Microsoft.EntityFrameworkCore;
using BackEnd_WebSocket.Models;

namespace BackEnd_WebSocket.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<VentanaDb> Ventanas { get; set; }

    }
}
