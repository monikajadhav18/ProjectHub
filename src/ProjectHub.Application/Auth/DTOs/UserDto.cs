using System;
using System.Collections.Generic;

namespace ProjectHub.Application.Auth.DTOs;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles);