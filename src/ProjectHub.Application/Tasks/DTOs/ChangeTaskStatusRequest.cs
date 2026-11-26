using System.ComponentModel.DataAnnotations;
using ProjectHub.Domain.Enums;

namespace ProjectHub.Application.Tasks.DTOs;

public sealed class ChangeTaskStatusRequest
{
    [Required]
    public TaskItemStatus? Status { get; init; }
}