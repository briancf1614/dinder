using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;


namespace Dinder.Application.Common.Commands.Auth.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;
        public LoginCommandHandler(IApplicationDbContext dbContext, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }
        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscar usuario por email
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            // 2. Si no existe O password incorrecta → mismo error (no filtrar info)
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Email o password incorrectos");
            // 3. Generar tokens
            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            // 4. Guardar refresh token en el usuario
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new AuthResponse(token, refreshToken);
        }
    }
}
