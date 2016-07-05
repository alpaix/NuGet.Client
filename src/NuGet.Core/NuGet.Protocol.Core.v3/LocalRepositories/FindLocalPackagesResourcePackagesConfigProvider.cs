// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    /// <summary>
    /// Reads packages.config packages folders. The expected format is root/id.version/id.version.nupkg
    /// This resource cannot handle packages folders in the format root/id (exclude version)
    /// </summary>
    public class FindLocalPackagesResourcePackagesConfigProvider : ResourceProvider
    {
        public FindLocalPackagesResourcePackagesConfigProvider()
            : base(typeof(FindLocalPackagesResource), nameof(FindLocalPackagesResourcePackagesConfigProvider), NuGetResourceProviderPositions.Last)
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;
            var feedType = await sourceRepository.GetFeedType(token);

            if (feedType == FeedType.FileSystemPackagesConfig)
            {
                resource = new FindLocalPackagesResourcePackagesConfig(sourceRepository.PackageSource.Source);

                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindLocalPackagesProxyResource>(
                    sourceRepository,
                    innerResource: resource,
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
