using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Auth.Login
{
    public class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;
        public RefreshCommandHandler(IApplicationDbContext dbContext, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }
        public async Task<AuthResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscar usuario por refresh token NO expirado
            var user = await _dbContext.Users.FirstOrDefaultAsync(
                u => u.RefreshToken == request.RefreshToken
                     && u.RefreshTokenExpiry > DateTime.UtcNow,
                cancellationToken);
            if (user is null)
                throw new UnauthorizedAccessException("Refresh token inválido o expirado");
            // 2. Rotar: generar nuevo refresh token
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            // 3. Generar nuevo JWT
            var token = _tokenService.GenerateToken(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new AuthResponse(token, newRefreshToken);
        }
    }
}
