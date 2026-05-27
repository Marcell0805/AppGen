using Hello_World.Domain.Entities;
using Hello_World.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Hello_World.Persistence.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // <AppGen-DbSets>
    // </AppGen-DbSets>

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // <AppGen-Configurations>
        // </AppGen-Configurations>
    }
}
