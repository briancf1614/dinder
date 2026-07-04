using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Application.Common.Commands.Auth.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;

        public RegisterCommandHandler(IApplicationDbContext dbContext, ITokenService tokenService)
        {
            this._dbContext = dbContext;
            this._tokenService = tokenService;
        }

        public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // 1. ¿Ya existe el email?
            var exists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
            if (exists)
                throw new ValidationException("Email ya registrado");
            // 2. Hashear la password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            // 3. Crear y guardar el usuario
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            // 4. Generar tokens
            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            return new AuthResponse(token, refreshToken);
        }
    }
}
