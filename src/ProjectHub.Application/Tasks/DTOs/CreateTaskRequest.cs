using System;
using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Entities;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Tasks.DTOs;

public sealed class CreateTaskRequest
{
    [Required]
    [StringLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    // If the client omits the priority, it defaults to Medium.
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    public DateOnly? DueDate { get; init; }

    // Optional: assign the task to an employee while creating it.
    public Guid? AssigneeUserId { get; init; }
}