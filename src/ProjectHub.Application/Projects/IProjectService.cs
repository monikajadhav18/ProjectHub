using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectHub.Application.Common.Models;
using ProjectHub.Application.Common.Results;
using ProjectHub.Application.Projects.DTOs;

namespace ProjectHub.Application.Projects;

public interface IProjectService
{
    Task<Result<PagedResult<ProjectDto>>> GetProjectsAsync(GetProjectsQuery query, CancellationToken cancellationToken);

    Task<Result<ProjectDto>> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<ProjectDto>> CreateProjectAsync(SaveProjectRequest request, CancellationToken cancellationToken);

    Task<Result<ProjectDto>> UpdateProjectAsync(Guid id, SaveProjectRequest request, CancellationToken cancellationToken);

    Task<Result<ProjectDto>> ChangeProjectStatusAsync(Guid id, ChangeProjectStatusRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteProjectAsync(Guid id, CancellationToken cancellationToken);
}