using Hello_World.Application.Interfaces;
using Hello_World.Persistence.Contexts;
using Hello_World.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hello_World.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {

            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        });

        // <AppGen-Repositories>
        // </AppGen-Repositories>
        return services;
    }
}
