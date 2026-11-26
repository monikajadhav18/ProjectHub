using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Application.Common.Constants;
using ProjectHub.Application.Common.Models;
using ProjectHub.Application.Tasks;
using ProjectHub.Application.Tasks.DTOs;

namespace ProjectHub.API.Controllers;

[Authorize]
[Route("api")]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectTasks(
        Guid projectId,
        [FromQuery] GetTasksQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetProjectTasksAsync(projectId, query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPost("projects/{projectId:guid}/tasks")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTask(
        Guid projectId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateTaskAsync(projectId, request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetTaskById), new { id = result.Value!.Id }, result.Value)
            : ToProblem(result.Error!);
    }

    [HttpGet("tasks/my")]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks([FromQuery] GetTasksQuery query, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetMyTasksAsync(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTaskByIdAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPut("tasks/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateTaskAsync(id, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    // Any logged-in user may call this, but the service only lets employees change tasks assigned to them.
    [HttpPatch("tasks/{id:guid}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTaskStatus(
        Guid id,
        [FromBody] ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.ChangeTaskStatusAsync(id, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPatch("tasks/{id:guid}/assignee")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTask(
        Guid id,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.AssignTaskAsync(id, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpDelete("tasks/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteTaskAsync(id, cancellationToken);

        return result.IsSuccess ? NoContent() : ToProblem(result.Error!);
    }
}