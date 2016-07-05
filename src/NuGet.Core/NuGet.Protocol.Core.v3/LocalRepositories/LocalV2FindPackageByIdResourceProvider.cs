// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    /// <summary>
    /// A v2-style package repository that has nupkgs at the root.
    /// </summary>
    public class LocalV2FindPackageByIdResourceProvider : ResourceProvider
    {
        public LocalV2FindPackageByIdResourceProvider()
            : base(typeof(FindPackageByIdResource),
                  nameof(LocalV2FindPackageByIdResourceProvider),
                  before: nameof(LocalV3FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            var feedType = await sourceRepository.GetFeedType(token);
            if (feedType == FeedType.FileSystemV2)
            {
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindPackageByIdProxyResource>(
                    sourceRepository,
                    innerResource: new LocalV2FindPackageByIdResource(sourceRepository.PackageSource),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
