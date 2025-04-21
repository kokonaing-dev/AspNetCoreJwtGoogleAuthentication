using JwtGoogleAuthDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtGoogleAuthDemo;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

   
}