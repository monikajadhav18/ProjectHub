using System;
using System.Collections.Generic;
using ProjectHub.Domain.Common;
using ProjectHub.Domain.Enums;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Entities;

public class Project : BaseEntity
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    // Needed by EF Core to create instances when reading from the database.
    // Private, so no other code can build an invalid Project with "new Project()".
    private Project()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ProjectStatus Status { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    public bool IsClosed => Status is ProjectStatus.Completed or ProjectStatus.Cancelled;

    public static Project Create(
        string name,
        string? description,
        DateOnly startDate,
        DateOnly? endDate,
        Guid createdByUserId)
    {
        var project = new Project
        {
            Status = ProjectStatus.Planning,
            CreatedByUserId = Guard.RequiredId(createdByUserId, "Creator")
        };

        project.SetDetails(name, description, startDate, endDate);

        return project;
    }

    public void Update(string name, string? description, DateOnly startDate, DateOnly? endDate)
    {
        if (IsClosed)
        {
            throw new DomainException("A completed or cancelled project cannot be edited.");
        }

        SetDetails(name, description, startDate, endDate);
        MarkAsUpdated();
    }

    public void ChangeStatus(ProjectStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new DomainException("Invalid project status.");
        }

        if (newStatus == Status)
        {
            return;
        }

        if (IsClosed)
        {
            throw new DomainException("A completed or cancelled project cannot change its status.");
        }

        Status = newStatus;
        MarkAsUpdated();
    }

    private void SetDetails(string name, string? description, DateOnly startDate, DateOnly? endDate)
    {
        // Validate everything first, then assign, so a failed update never leaves a half-changed object.
        var validName = Guard.RequiredText(name, "Project name", NameMaxLength);
        var validDescription = Guard.OptionalText(description, "Description", DescriptionMaxLength);

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainException("End date cannot be earlier than start date.");
        }

        Name = validName;
        Description = validDescription;
        StartDate = startDate;
        EndDate = endDate;
    }
}