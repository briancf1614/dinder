using Dinder.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Auth.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
}
