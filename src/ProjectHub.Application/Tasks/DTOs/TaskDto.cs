using System;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Tasks.DTOs;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    DateTime? CompletedAtUtc,
    Guid? AssignedToUserId,
    string? AssignedToName,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsOverdue);