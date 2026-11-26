using System;
using Microsoft.AspNetCore.Identity;

namespace ProjectHub.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
}