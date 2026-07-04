using Dinder.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Auth.Register
{
    public record RegisterCommand(string Email, string Password) : IRequest<AuthResponse>;
}
