// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class FindLocalPackagesResourceUnzippedProvider : ResourceProvider
    {
        // Cache unzipped resources across the repository
        private readonly ConcurrentDictionary<PackageSource, FindLocalPackagesResourceUnzipped> _cache =
            new ConcurrentDictionary<PackageSource, FindLocalPackagesResourceUnzipped>();

        public FindLocalPackagesResourceUnzippedProvider()
            : base(typeof(FindLocalPackagesResource), nameof(FindLocalPackagesResourceUnzippedProvider), NuGetResourceProviderPositions.Last)
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            if (await sourceRepository.GetFeedType(token) == FeedType.FileSystemUnzipped)
            {
                resource = _cache.GetOrAdd(sourceRepository.PackageSource,
                    (packageSource) => new FindLocalPackagesResourceUnzipped(packageSource.Source));

                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindLocalPackagesProxyResource>(
                    sourceRepository,
                    innerResource: resource,
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
