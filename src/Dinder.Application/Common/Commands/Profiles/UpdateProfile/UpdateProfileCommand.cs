using Dinder.Application.Common.Models;
using Dinder.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Profiles.UpdateProfile
{
    public record UpdateProfileCommand(
        string DisplayName,
        string? Bio,
        DateOnly? BirthDate,
        Gender? Gender
    ) : IRequest<MeResponse>;
}
