using System;
using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Entities;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Tasks.DTOs;

/// <summary>PUT replaces all of these fields, so priority is required here.</summary>
public sealed class UpdateTaskRequest
{
    [Required]
    [StringLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    [Required]
    public TaskPriority? Priority { get; init; }

    public DateOnly? DueDate { get; init; }
}