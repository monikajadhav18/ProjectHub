using System;
using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Projects.DTOs;

public sealed class GetProjectsQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public ProjectStatus? Status { get; init; }

    public DateOnly? StartDateFrom { get; init; }

    public DateOnly? StartDateTo { get; init; }

    public ProjectSortBy SortBy { get; init; } = ProjectSortBy.CreatedAt;

    public bool SortDescending { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}