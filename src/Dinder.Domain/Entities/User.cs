using Dinder.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? RefreshToken { get; set; }       // el token de refresh, nullable
        public DateTime? RefreshTokenExpiry { get; set; } // cuándo expira, nullable
        public string Role { get; set; } = "user";       // rol, default "user"

        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public DateOnly? BirthDate { get; set; }
        public Gender? Gender { get; set; }
    }
}
