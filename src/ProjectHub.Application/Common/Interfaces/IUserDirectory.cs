using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectHub.Application.Common.Models;

namespace ProjectHub.Application.Common.Interfaces;

/// <summary>
/// Read-only questions the Application layer needs to ask about users.
/// Implemented in Infrastructure, where the Identity tables live.
/// </summary>
public interface IUserDirectory
{
    Task<bool> IsEmployeeAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, UserSummaryDto>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserSummaryDto>> GetEmployeesAsync(CancellationToken cancellationToken);
}