using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Application.Common.Interfaces;
using ProjectHub.Application.Common.Models;
using ProjectHub.Application.Common.Results;
using ProjectHub.Application.Projects.DTOs;
using ProjectHub.Domain.Entities;
using ProjectHub.Domain.Enums;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Application.Projects;

public sealed class ProjectService : IProjectService
{
    private const int MaxPageSize = 100;

    // One shared mapping, translated to SQL by EF Core.
    // It is an Expression (not a Func) so EF can turn it into a SELECT with the two COUNT subqueries.
    private static readonly Expression<Func<Project, ProjectDto>> ToDto = p => new ProjectDto(
        p.Id,
        p.Name,
        p.Description,
        p.Status,
        p.StartDate,
        p.EndDate,
        p.CreatedByUserId,
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.Tasks.Count,
        p.Tasks.Count(t => t.Status == TaskItemStatus.Done));

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ProjectService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ProjectDto>>> GetProjectsAsync(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.StartDateFrom.HasValue && request.StartDateTo.HasValue
            && request.StartDateFrom.Value > request.StartDateTo.Value)
        {
            return Result<PagedResult<ProjectDto>>.Failure(
                Error.Validation("StartDateFrom cannot be later than StartDateTo."));
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = VisibleProjects();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term)
                                     || (p.Description != null && p.Description.Contains(term)));
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(p => p.Status == status);
        }

        if (request.StartDateFrom.HasValue)
        {
            var from = request.StartDateFrom.Value;
            query = query.Where(p => p.StartDate >= from);
        }

        if (request.StartDateTo.HasValue)
        {
            var to = request.StartDateTo.Value;
            query = query.Where(p => p.StartDate <= to);
        }

        // Query 1: how many rows match (needed for the paging metadata).
        var totalCount = await query.CountAsync(cancellationToken);

        // Query 2: only the requested page.
        var items = await ApplySorting(query, request.SortBy, request.SortDescending)
            .ThenBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ProjectDto>>.Success(
            new PagedResult<ProjectDto>(items, pageNumber, pageSize, totalCount));
    }

    public async Task<Result<ProjectDto>> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await VisibleProjects()
            .Where(p => p.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return project is null
            ? Result<ProjectDto>.Failure(Error.NotFound("Project was not found."))
            : Result<ProjectDto>.Success(project);
    }

    public async Task<Result<ProjectDto>> CreateProjectAsync(SaveProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<ProjectDto>.Failure(Error.Unauthorized("You must be logged in."));
        }

        if (request.StartDate is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("Start date is required."));
        }

        Project project;

        try
        {
            project = Project.Create(
                request.Name,
                request.Description,
                request.StartDate.Value,
                request.EndDate,
                userId.Value);
        }
        catch (DomainException ex)
        {
            return Result<ProjectDto>.Failure(Error.Validation(ex.Message));
        }

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetProjectByIdAsync(project.Id, cancellationToken);
    }

    public async Task<Result<ProjectDto>> UpdateProjectAsync(
        Guid id,
        SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("Start date is required."));
        }

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result<ProjectDto>.Failure(Error.NotFound("Project was not found."));
        }

        try
        {
            project.Update(request.Name, request.Description, request.StartDate.Value, request.EndDate);
        }
        catch (DomainException ex)
        {
            return Result<ProjectDto>.Failure(Error.Validation(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetProjectByIdAsync(id, cancellationToken);
    }

    public async Task<Result<ProjectDto>> ChangeProjectStatusAsync(
        Guid id,
        ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is null)
        {
            return Result<ProjectDto>.Failure(Error.Validation("Status is required."));
        }

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result<ProjectDto>.Failure(Error.NotFound("Project was not found."));
        }

        try
        {
            project.ChangeStatus(request.Status.Value);
        }
        catch (DomainException ex)
        {
            return Result<ProjectDto>.Failure(Error.Validation(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetProjectByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result.Failure(Error.NotFound("Project was not found."));
        }

        // The database cascades the delete to the project's tasks (configured in Phase 3).
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Admins see every project. Everyone else sees only projects that contain a task assigned to them.
    /// This is applied INSIDE the query, so rows the user may not see never leave the database.
    /// </summary>
    private IQueryable<Project> VisibleProjects()
    {
        var projects = _db.Projects.AsNoTracking();

        if (_currentUser.IsAdmin)
        {
            return projects;
        }

        var userId = _currentUser.UserId;

        return userId is null
            ? projects.Where(_ => false)
            : projects.Where(p => p.Tasks.Any(t => t.AssignedToUserId == userId));
    }

    private static IOrderedQueryable<Project> ApplySorting(
        IQueryable<Project> query,
        ProjectSortBy sortBy,
        bool descending)
    {
        return sortBy switch
        {
            ProjectSortBy.Name => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            ProjectSortBy.StartDate => descending
                ? query.OrderByDescending(p => p.StartDate)
                : query.OrderBy(p => p.StartDate),
            ProjectSortBy.Status => descending
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            _ => descending
                ? query.OrderByDescending(p => p.CreatedAtUtc)
                : query.OrderBy(p => p.CreatedAtUtc)
        };
    }
}