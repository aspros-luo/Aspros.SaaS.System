﻿using MediatR;

namespace Aspros.SaaS.System.Application.Command
{
    public class TenantPackageDelCommand :  IRequest<long>
    {
        public long Id{ set; get; }
        public bool IsDel { set; get; } = false;
    }
}
