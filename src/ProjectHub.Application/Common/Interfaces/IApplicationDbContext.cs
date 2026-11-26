using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Domain.Entities;

namespace ProjectHub.Application.Common.Interfaces;

/// <summary>
/// What the Application layer needs from the database. Implemented in Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }

    DbSet<TaskItem> Tasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}