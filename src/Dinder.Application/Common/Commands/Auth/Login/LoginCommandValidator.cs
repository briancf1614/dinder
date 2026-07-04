using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Auth.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email es requerido")
                .EmailAddress().WithMessage("Email no es válido");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password es requerida")
                .MinimumLength(6).WithMessage("Password debe tener al menos 6 caracteres");
        }
    }
}
