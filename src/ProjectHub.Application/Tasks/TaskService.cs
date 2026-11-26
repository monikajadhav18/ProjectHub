using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Application.Common.Interfaces;
using ProjectHub.Application.Common.Models;
using ProjectHub.Application.Common.Results;
using ProjectHub.Application.Tasks.DTOs;
using ProjectHub.Domain.Entities;
using ProjectHub.Domain.Enums;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Application.Tasks;

public sealed class TaskService : ITaskService
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _userDirectory;
    private readonly TimeProvider _timeProvider;

    public TaskService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IUserDirectory userDirectory,
        TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _userDirectory = userDirectory;
        _timeProvider = timeProvider;
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

    public async Task<Result<PagedResult<TaskDto>>> GetProjectTasksAsync(
        Guid projectId,
        GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectIsVisibleAsync(projectId, cancellationToken))
        {
            return Result<PagedResult<TaskDto>>.Failure(Error.NotFound("Project was not found."));
        }

        var tasks = ScopeToCurrentUser(_db.Tasks.AsNoTracking())
            .Where(t => t.ProjectId == projectId);

        return await ExecuteQueryAsync(tasks, request, cancellationToken);
    }

    public async Task<Result<PagedResult<TaskDto>>> GetMyTasksAsync(
        GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<PagedResult<TaskDto>>.Failure(Error.Unauthorized("You must be logged in."));
        }

        var id = userId.Value;
        var tasks = _db.Tasks.AsNoTracking().Where(t => t.AssignedToUserId == id);

        return await ExecuteQueryAsync(tasks, request, cancellationToken);
    }

    public async Task<Result<TaskDto>> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await ScopeToCurrentUser(_db.Tasks.AsNoTracking())
            .Where(t => t.Id == id)
            .Select(ToDto(Today))
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Task was not found."));
        }

        var withNames = await AddAssigneeNamesAsync(new[] { task }, cancellationToken);

        return Result<TaskDto>.Success(withNames[0]);
    }

    public async Task<Result<TaskDto>> CreateTaskAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<TaskDto>.Failure(Error.Unauthorized("You must be logged in."));
        }

        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Project was not found."));
        }

        if (project.IsClosed)
        {
            return Result<TaskDto>.Failure(Error.Validation(ClosedProjectMessage));
        }

        if (request.AssigneeUserId.HasValue
            && !await _userDirectory.IsEmployeeAsync(request.AssigneeUserId.Value, cancellationToken))
        {
            return Result<TaskDto>.Failure(Error.Validation("The assignee must be an existing employee."));
        }

        TaskItem task;

        try
        {
            task = TaskItem.Create(
                projectId,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                userId.Value);

            if (request.AssigneeUserId.HasValue)
            {
                task.AssignTo(request.AssigneeUserId.Value);
            }
        }
        catch (DomainException ex)
        {
            return Result<TaskDto>.Failure(Error.Validation(ex.Message));
        }

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(task.Id, cancellationToken);
    }

    public async Task<Result<TaskDto>> UpdateTaskAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Priority is null)
        {
            return Result<TaskDto>.Failure(Error.Validation("Priority is required."));
        }

        var loaded = await LoadTaskForChangeAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result<TaskDto>.Failure(loaded.Error!);
        }

        try
        {
            loaded.Value!.Update(request.Title, request.Description, request.Priority.Value, request.DueDate);
        }
        catch (DomainException ex)
        {
            return Result<TaskDto>.Failure(Error.Validation(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(id, cancellationToken);
    }

    public async Task<Result<TaskDto>> ChangeTaskStatusAsync(
        Guid id,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is null)
        {
            return Result<TaskDto>.Failure(Error.Validation("Status is required."));
        }

        // For employees, LoadTaskForChangeAsync only finds tasks assigned to them.
        var loaded = await LoadTaskForChangeAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result<TaskDto>.Failure(loaded.Error!);
        }

        try
        {
            loaded.Value!.ChangeStatus(request.Status.Value);
        }
        catch (DomainException ex)
        {
            return Result<TaskDto>.Failure(Error.Validation(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(id, cancellationToken);
    }

    public async Task<Result<TaskDto>> AssignTaskAsync(
        Guid id,
        AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadTaskForChangeAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result<TaskDto>.Failure(loaded.Error!);
        }

        var task = loaded.Value!;

        if (request.AssigneeUserId.HasValue)
        {
            if (!await _userDirectory.IsEmployeeAsync(request.AssigneeUserId.Value, cancellationToken))
            {
                return Result<TaskDto>.Failure(Error.Validation("The assignee must be an existing employee."));
            }

            task.AssignTo(request.AssigneeUserId.Value);
        }
        else
        {
            task.Unassign();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadTaskForChangeAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result.Failure(loaded.Error!);
        }

        _db.Tasks.Remove(loaded.Value!);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private const string ClosedProjectMessage =
        "Tasks in a completed or cancelled project cannot be changed.";

    /// <summary>The "today" value is passed in, so the mapping contains no hidden clock.</summary>
    private static Expression<Func<TaskItem, TaskDto>> ToDto(DateOnly today) => t => new TaskDto(
        t.Id,
        t.ProjectId,
        t.Project.Name,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CompletedAtUtc,
        t.AssignedToUserId,
        null,
        t.CreatedByUserId,
        t.CreatedAtUtc,
        t.UpdatedAtUtc,
        t.DueDate != null && t.DueDate < today && t.Status != TaskItemStatus.Done);

    /// <summary>
    /// Admins see every task. Everyone else sees only tasks assigned to them.
    /// If we don't know who the caller is, nothing is visible. (Comparing to a null id would
    /// accidentally match every UNASSIGNED task.)
    /// </summary>
    private IQueryable<TaskItem> ScopeToCurrentUser(IQueryable<TaskItem> tasks)
    {
        if (_currentUser.IsAdmin)
        {
            return tasks;
        }

        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return tasks.Where(_ => false);
        }

        var id = userId.Value;

        return tasks.Where(t => t.AssignedToUserId == id);
    }

    private async Task<bool> ProjectIsVisibleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin)
        {
            return await _db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
        }

        // An employee can see a project only if they have a task in it.
        return await ScopeToCurrentUser(_db.Tasks).AnyAsync(t => t.ProjectId == projectId, cancellationToken);
    }

    /// <summary>Loads a TRACKED task (with its project) that the caller may change.</summary>
    private async Task<Result<TaskItem>> LoadTaskForChangeAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await ScopeToCurrentUser(_db.Tasks.Include(t => t.Project))
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return Result<TaskItem>.Failure(Error.NotFound("Task was not found."));
        }

        if (task.Project.IsClosed)
        {
            return Result<TaskItem>.Failure(Error.Validation(ClosedProjectMessage));
        }

        return Result<TaskItem>.Success(task);
    }

    private async Task<Result<PagedResult<TaskDto>>> ExecuteQueryAsync(
        IQueryable<TaskItem> query,
        GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DueDateFrom.HasValue && request.DueDateTo.HasValue
            && request.DueDateFrom.Value > request.DueDateTo.Value)
        {
            return Result<PagedResult<TaskDto>>.Failure(
                Error.Validation("DueDateFrom cannot be later than DueDateTo."));
        }

        var today = Today;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Title.Contains(term)
                                     || (t.Description != null && t.Description.Contains(term)));
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(t => t.Status == status);
        }

        if (request.Priority.HasValue)
        {
            var priority = request.Priority.Value;
            query = query.Where(t => t.Priority == priority);
        }

        if (request.AssignedToUserId.HasValue)
        {
            var assignee = request.AssignedToUserId.Value;
            query = query.Where(t => t.AssignedToUserId == assignee);
        }

        if (request.OverdueOnly)
        {
            query = query.Where(t => t.DueDate != null && t.DueDate < today && t.Status != TaskItemStatus.Done);
        }

        if (request.DueDateFrom.HasValue)
        {
            var from = request.DueDateFrom.Value;
            query = query.Where(t => t.DueDate >= from);
        }

        if (request.DueDateTo.HasValue)
        {
            var to = request.DueDateTo.Value;
            query = query.Where(t => t.DueDate <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplySorting(query, request.SortBy, request.SortDescending)
            .ThenBy(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto(today))
            .ToListAsync(cancellationToken);

        var withNames = await AddAssigneeNamesAsync(items, cancellationToken);

        return Result<PagedResult<TaskDto>>.Success(
            new PagedResult<TaskDto>(withNames, pageNumber, pageSize, totalCount));
    }

    /// <summary>One batch lookup for the whole page, instead of one query per task (the "N+1" problem).</summary>
    private async Task<IReadOnlyList<TaskDto>> AddAssigneeNamesAsync(
        IReadOnlyList<TaskDto> tasks,
        CancellationToken cancellationToken)
    {
        var assigneeIds = tasks
            .Where(t => t.AssignedToUserId.HasValue)
            .Select(t => t.AssignedToUserId!.Value)
            .Distinct()
            .ToArray();

        if (assigneeIds.Length == 0)
        {
            return tasks;
        }

        var users = await _userDirectory.GetUsersByIdsAsync(assigneeIds, cancellationToken);

        return tasks
            .Select(t => t.AssignedToUserId is { } assigneeId && users.TryGetValue(assigneeId, out var user)
                ? t with { AssignedToName = user.FullName }
                : t)
            .ToList();
    }

    private static IOrderedQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> query,
        TaskSortBy sortBy,
        bool descending)
    {
        return sortBy switch
        {
            TaskSortBy.Title => descending
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            TaskSortBy.DueDate => descending
                ? query.OrderByDescending(t => t.DueDate)
                : query.OrderBy(t => t.DueDate),
            TaskSortBy.Priority => descending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            TaskSortBy.Status => descending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            _ => descending
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc)
        };
    }
}