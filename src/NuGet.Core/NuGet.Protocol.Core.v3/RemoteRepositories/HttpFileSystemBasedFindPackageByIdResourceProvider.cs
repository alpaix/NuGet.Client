// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class HttpFileSystemBasedFindPackageByIdResourceProvider : ResourceProvider
    {
        private const string HttpFileSystemIndexType = "PackageBaseAddress/3.0.0";

        public HttpFileSystemBasedFindPackageByIdResourceProvider()
            : base(typeof(FindPackageByIdResource),
                nameof(HttpFileSystemBasedFindPackageByIdResourceProvider),
                before: nameof(RemoteV3FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;
            var serviceIndexResource = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>(token);
            var packageBaseAddress = serviceIndexResource?[HttpFileSystemIndexType];

            if (packageBaseAddress != null
                && packageBaseAddress.Count > 0)
            {
                var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);

                resource = await ProxyResourceFactory.CreateDiagnosticsProxyAsync<FindPackageByIdProxyResource>(
                    sourceRepository,
                    innerResource: new HttpFileSystemBasedFindPackageByIdResource(
                        packageBaseAddress,
                        httpSourceResource.HttpSource),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
