using System;
using ProjectHub.Domain.Common;
using ProjectHub.Domain.Enums;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Entities;

public class TaskItem : BaseEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    // Needed by EF Core. Private on purpose.
    private TaskItem()
    {
    }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public Guid? AssignedToUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public static TaskItem Create(
        Guid projectId,
        string title,
        string? description,
        TaskPriority priority,
        DateOnly? dueDate,
        Guid createdByUserId)
    {
        var task = new TaskItem
        {
            ProjectId = Guard.RequiredId(projectId, "Project"),
            CreatedByUserId = Guard.RequiredId(createdByUserId, "Creator"),
            Status = TaskItemStatus.ToDo
        };

        task.SetDetails(title, description, priority, dueDate);

        return task;
    }

    public void Update(string title, string? description, TaskPriority priority, DateOnly? dueDate)
    {
        SetDetails(title, description, priority, dueDate);
        MarkAsUpdated();
    }

    public void AssignTo(Guid userId)
    {
        AssignedToUserId = Guard.RequiredId(userId, "Assignee");
        MarkAsUpdated();
    }

    public void Unassign()
    {
        AssignedToUserId = null;
        MarkAsUpdated();
    }

    public void ChangeStatus(TaskItemStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new DomainException("Invalid task status.");
        }

        if (newStatus == Status)
        {
            return;
        }

        Status = newStatus;
        CompletedAtUtc = newStatus == TaskItemStatus.Done ? DateTime.UtcNow : null;
        MarkAsUpdated();
    }

    /// <summary>
    /// A task is overdue when it has a due date in the past and is not finished.
    /// "today" is passed in (instead of calling DateTime.Now here) so the rule is easy to unit test.
    /// </summary>
    public bool IsOverdue(DateOnly today)
        => DueDate.HasValue && Status != TaskItemStatus.Done && DueDate.Value < today;

    private void SetDetails(string title, string? description, TaskPriority priority, DateOnly? dueDate)
    {
        var validTitle = Guard.RequiredText(title, "Task title", TitleMaxLength);
        var validDescription = Guard.OptionalText(description, "Description", DescriptionMaxLength);

        if (!Enum.IsDefined(priority))
        {
            throw new DomainException("Invalid task priority.");
        }

        Title = validTitle;
        Description = validDescription;
        Priority = priority;
        DueDate = dueDate;
    }
}