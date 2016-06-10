// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class FindLocalPackagesResourceV3Provider : ResourceProvider
    {
        public FindLocalPackagesResourceV3Provider()
            : base(typeof(FindLocalPackagesResource), nameof(FindLocalPackagesResourceV3Provider), nameof(FindLocalPackagesResourceV2Provider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            if (await sourceRepository.GetFeedType(token) == FeedType.FileSystemV3)
            {
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindLocalPackagesProxyResource>(
                    sourceRepository,
                    innerResource: new FindLocalPackagesResourceV3(sourceRepository.PackageSource.Source),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
