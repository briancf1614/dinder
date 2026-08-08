using Dinder.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Models
{
    public record MeResponse(
        Guid Id,
        string Email,
        DateTime CreatedAt,
        string? DisplayName,
        string? Bio,
        DateOnly? BirthDate,
        Gender? Gender
    );
}
