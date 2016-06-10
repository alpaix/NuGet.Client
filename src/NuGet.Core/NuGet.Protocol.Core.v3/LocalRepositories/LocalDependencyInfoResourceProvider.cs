// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class LocalDependencyInfoResourceProvider : ResourceProvider
    {
        public LocalDependencyInfoResourceProvider()
            : base(typeof(DependencyInfoResource), nameof(LocalDependencyInfoResourceProvider), NuGetResourceProviderPositions.Last)
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            var localResource = await sourceRepository.GetResourceAsync<FindLocalPackagesResource>(token);

            if (localResource != null)
            {
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<DependencyInfoProxyResource>(
                    sourceRepository,
                    innerResource: new LocalDependencyInfoResource(localResource, sourceRepository),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
