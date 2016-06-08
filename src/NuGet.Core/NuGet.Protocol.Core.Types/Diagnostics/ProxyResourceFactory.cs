// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public static class ProxyResourceFactory
    {
        public static async Task<INuGetResource> CreateDiagnosticsProxyResourceAsync<TProxyResource>(
            SourceRepository sourceRepository, 
            INuGetResource innerResource,
            CancellationToken cancellationToken)
            where TProxyResource : class, IProxyResource
        {
            if (sourceRepository == null)
            {
                throw new ArgumentNullException(nameof(sourceRepository));
            }

            if (innerResource == null)
            {
                throw new ArgumentNullException(nameof(innerResource));
            }

            var diagnosticsResource = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>(cancellationToken);
            if (diagnosticsResource != null)
            {
                var proxyResource = (INuGetResource)Activator.CreateInstance(typeof(TProxyResource), innerResource, diagnosticsResource.PackageSourceDiagnostics);

                return proxyResource;
            }

            return innerResource;
        }
    }
}
