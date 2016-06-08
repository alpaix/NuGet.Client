// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGet.Protocol
{
    /// <summary>
    /// A <see cref="ResourceProvider" /> for <see cref="FindPackageByIdResource" /> over v2 NuGet feeds.
    /// </summary>
    public class RemoteV2FindPackageByIdResourceProvider : ResourceProvider
    {
        public RemoteV2FindPackageByIdResourceProvider()
            : base(
                typeof(FindPackageByIdResource),
                name: nameof(RemoteV2FindPackageByIdResourceProvider),
                before: nameof(LocalV2FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            FindPackageByIdResource resource = null;

            if (sourceRepository.PackageSource.IsHttp
                &&
                !sourceRepository.PackageSource.Source.EndsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);

                resource = new RemoteV2FindPackageByIdResource(
                    sourceRepository.PackageSource,
                    httpSourceResource.HttpSource);

            }

            if (resource != null)
            {
                var diagnosticResource = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>();
                if (diagnosticResource != null)
                {}
                resource = new FindPackageByIdProxyResource(sourceRepository.PackageSource, innerResource, diagnostics: null);
            }

            return Tuple.Create(resource != null, (INuGetResource)resource);
        }
    }
}
