using System;

namespace ProjectHub.Application.Tasks.DTOs;

public sealed class AssignTaskRequest
{
    /// <summary>The employee to assign. Send null to unassign the task.</summary>
    public Guid? AssigneeUserId { get; init; }
}