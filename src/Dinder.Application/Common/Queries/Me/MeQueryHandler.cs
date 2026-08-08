using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Dinder.Application.Common.Queries.Me
{
    public class MeQueryHandler : IRequestHandler<MeQuery, MeResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MeQueryHandler(IApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            this._dbContext = dbContext;
            this._httpContextAccessor = httpContextAccessor;
        }

        public async Task<MeResponse> Handle(MeQuery request, CancellationToken cancellationToken)
        {
            // 1. Sacar el email del JWT
            var email = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Token inválido: no contiene email");
            // 2. Buscar usuario
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
                throw new UnauthorizedAccessException("Usuario no encontrado");
            // 3. Devolver respuesta
            return new MeResponse(user.Id, user.Email, user.CreatedAt,
                user.DisplayName, user.Bio, user.BirthDate, user.Gender);
        }
    }
}
