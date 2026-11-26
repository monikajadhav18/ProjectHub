using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Projects.DTOs;

public sealed class ChangeProjectStatusRequest
{
    [Required]
    public ProjectStatus? Status { get; init; }
}