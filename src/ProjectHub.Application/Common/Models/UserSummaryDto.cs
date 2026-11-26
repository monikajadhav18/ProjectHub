using System;

namespace ProjectHub.Application.Common.Models;

public sealed record UserSummaryDto(Guid Id, string FullName, string Email);