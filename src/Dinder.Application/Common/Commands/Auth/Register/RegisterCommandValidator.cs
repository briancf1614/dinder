using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Auth.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email es obligatorio")
                .EmailAddress().WithMessage("Email no tiene formato valido");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password es obligatorio")
                .MinimumLength(6).WithMessage("Password debe tener al menos 6 caracteres");
        }
    }
}
