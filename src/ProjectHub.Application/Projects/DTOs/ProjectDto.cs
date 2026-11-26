using System;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int TaskCount,
    int CompletedTaskCount);