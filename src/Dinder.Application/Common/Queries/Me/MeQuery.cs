using Dinder.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Queries.Me
{
    public record MeQuery : IRequest<MeResponse>;
}
