// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class FindLocalPackagesResourceV2Provider : ResourceProvider
    {
        public FindLocalPackagesResourceV2Provider()
            : base(typeof(FindLocalPackagesResource), nameof(FindLocalPackagesResourceV2Provider), nameof(FindLocalPackagesResourceUnzippedProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;
            var feedType = await sourceRepository.GetFeedType(token);

            if (feedType == FeedType.FileSystemV2
                || feedType == FeedType.FileSystemUnknown)
            {
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindLocalPackagesProxyResource>(
                    sourceRepository,
                    innerResource: new FindLocalPackagesResourceV2(sourceRepository.PackageSource.Source),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
