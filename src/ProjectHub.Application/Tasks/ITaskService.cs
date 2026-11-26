using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectHub.Application.Common.Models;
using ProjectHub.Application.Common.Results;
using ProjectHub.Application.Tasks.DTOs;

namespace ProjectHub.Application.Tasks;

public interface ITaskService
{
    Task<Result<PagedResult<TaskDto>>> GetProjectTasksAsync(Guid projectId, GetTasksQuery query, CancellationToken cancellationToken);

    Task<Result<PagedResult<TaskDto>>> GetMyTasksAsync(GetTasksQuery query, CancellationToken cancellationToken);

    Task<Result<TaskDto>> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<TaskDto>> CreateTaskAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<Result<TaskDto>> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<Result<TaskDto>> ChangeTaskStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken);

    Task<Result<TaskDto>> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteTaskAsync(Guid id, CancellationToken cancellationToken);
}