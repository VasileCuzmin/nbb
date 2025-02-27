﻿// Copyright (c) TotalSoft.
// This source code is licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NBB.MultiTenancy.Abstractions.Repositories
{
    public class BasicTenantRepository : ITenantRepository
    {
        public BasicTenantRepository()
        {
        }

        public Task<Tenant> Get(Guid id, CancellationToken token = default)
        {
            var tenant = new Tenant(id, id.ToString());
            return Task.FromResult(tenant);
        }

        public Task<Tenant> GetByHost(string host, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
