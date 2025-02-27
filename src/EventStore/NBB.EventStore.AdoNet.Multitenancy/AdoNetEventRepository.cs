﻿// Copyright (c) TotalSoft.
// This source code is licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBB.EventStore.Abstractions;
using NBB.EventStore.AdoNet.Internal;
using NBB.MultiTenancy.Abstractions.Context;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NBB.EventStore.AdoNet.Multitenancy
{
    public class AdoNetMultiTenantEventRepository : AdoNetEventRepository
    {
        private readonly ITenantContextAccessor _tenantContextAccessor;

        public AdoNetMultiTenantEventRepository(Scripts scripts, ILogger<AdoNetEventRepository> logger, IOptions<EventStoreOptions> eventstoreOptions, ITenantContextAccessor tenantContextAccessor)
                : base(scripts, logger, eventstoreOptions)
        {
            _tenantContextAccessor = tenantContextAccessor;
        }

        protected override IEnumerable<SqlParameter> GetGlobalFilterParams()
        {
            var tenantId = _tenantContextAccessor.TenantContext.GetTenantId();
            yield return new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId };
        }
    }
}
