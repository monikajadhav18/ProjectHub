using Microsoft.Extensions.DependencyInjection;
using ProjectHub.Application.Projects;
using ProjectHub.Application.Tasks;

namespace ProjectHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();

        return services;
    }
}