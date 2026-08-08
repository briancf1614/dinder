using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Dinder.Application.Common.Commands.Profiles.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, MeResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public UpdateProfileCommandHandler(IApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }
        public async Task<MeResponse> Handle(UpdateProfileCommand request, CancellationToken ct)
        {
            var email = _http.HttpContext?.User
                .FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null)
                throw new UnauthorizedAccessException("Usuario no encontrado");

            user.DisplayName = request.DisplayName;
            user.Bio = request.Bio;
            user.BirthDate = request.BirthDate;
            user.Gender = request.Gender;
            await _db.SaveChangesAsync(ct);

            return new MeResponse(
                user.Id,
                user.Email,
                user.CreatedAt,
                user.DisplayName,
                user.Bio,
                user.BirthDate,
                user.Gender
            );
        }
    }
}
