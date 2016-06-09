// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    /// <summary>
    /// Retrieves all dependency info for the package resolver.
    /// </summary>
    public class DependencyInfoResourceV3Provider : ResourceProvider
    {
        public DependencyInfoResourceV3Provider()
            : base(typeof(DependencyInfoResource), nameof(DependencyInfoResourceV3Provider), "DependencyInfoResourceV2FeedProvider")
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            if (await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>(token) != null)
            {
                var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
                var regResource = await sourceRepository.GetResourceAsync<RegistrationResourceV3>(token);

                // construct a new resource
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyResourceAsync<DependencyInfoProxyResource>(
                    sourceRepository,
                    innerResource: new DependencyInfoResourceV3(httpSourceResource.HttpSource, regResource, sourceRepository),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
