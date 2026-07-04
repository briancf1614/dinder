using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Models
{
    public record MeResponse(Guid Id, string Email, DateTime CreatedAt);
}
