using System;
using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Entities;

namespace ProjectHub.Application.Projects.DTOs;

/// <summary>Used for both create (POST) and update (PUT): the editable fields are the same.</summary>
public sealed class SaveProjectRequest
{
    [Required]
    [StringLength(Project.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    [StringLength(Project.DescriptionMaxLength)]
    public string? Description { get; init; }

    // Nullable + [Required] so a missing date is reported instead of silently becoming 0001-01-01.
    [Required]
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}