using Microsoft.Extensions.DependencyInjection;
using ProjectHub.Application.Projects;

namespace ProjectHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();

        return services;
    }
}