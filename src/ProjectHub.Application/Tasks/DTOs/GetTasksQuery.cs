using System;
using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Tasks.DTOs;

public sealed class GetTasksQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public TaskItemStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }

    // Useful for Admins. For employees the results are already limited to their own tasks.
    public Guid? AssignedToUserId { get; init; }

    public bool OverdueOnly { get; init; }

    public DateOnly? DueDateFrom { get; init; }

    public DateOnly? DueDateTo { get; init; }

    public TaskSortBy SortBy { get; init; } = TaskSortBy.CreatedAt;

    public bool SortDescending { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}