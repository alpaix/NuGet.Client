﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    /// <summary>
    /// A v3-style package repository that has expanded packages.
    /// </summary>
    public class LocalV3FindPackageByIdResourceProvider : ResourceProvider
    {
        public LocalV3FindPackageByIdResourceProvider()
            : base(typeof(FindPackageByIdResource), nameof(LocalV3FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            var feedType = await sourceRepository.GetFeedType(token);

            // Default to v3 if the type is unknown
            if (feedType == FeedType.FileSystemV3
                || feedType == FeedType.FileSystemUnknown)
            {
                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindPackageByIdProxyResource>(
                    sourceRepository,
                    innerResource: new LocalV3FindPackageByIdResource(sourceRepository.PackageSource),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
