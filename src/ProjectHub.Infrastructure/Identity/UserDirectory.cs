using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Application.Common.Constants;
using ProjectHub.Application.Common.Interfaces;
using ProjectHub.Application.Common.Models;
using ProjectHub.Infrastructure.Persistence;

namespace ProjectHub.Infrastructure.Identity;

public sealed class UserDirectory : IUserDirectory
{
    private readonly ApplicationDbContext _db;

    public UserDirectory(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsEmployeeAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (from userRole in _db.UserRoles
                      join role in _db.Roles on userRole.RoleId equals role.Id
                      where userRole.UserId == userId && role.Name == Roles.Employee
                      select userRole.UserId)
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummaryDto>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.ToArray();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetEmployeesAsync(CancellationToken cancellationToken)
    {
        return await (from user in _db.Users.AsNoTracking()
                      join userRole in _db.UserRoles on user.Id equals userRole.UserId
                      join role in _db.Roles on userRole.RoleId equals role.Id
                      where role.Name == Roles.Employee
                      orderby user.FullName
                      select new UserSummaryDto(user.Id, user.FullName, user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }
}