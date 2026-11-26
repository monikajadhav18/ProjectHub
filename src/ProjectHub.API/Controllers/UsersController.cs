using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Application.Common.Constants;
using ProjectHub.Application.Common.Interfaces;
using ProjectHub.Application.Common.Models;

namespace ProjectHub.API.Controllers;

[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class UsersController : ApiControllerBase
{
    private readonly IUserDirectory _userDirectory;

    // A pure read-only lookup with no business rules, so there is no separate service here.
    public UsersController(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    [HttpGet("employees")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken)
    {
        var employees = await _userDirectory.GetEmployeesAsync(cancellationToken);

        return Ok(employees);
    }
}