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
        public class FindPackageByIdProxyResource : FindPackageByIdResource
        {
            private static readonly string ResourceName = nameof(FindPackageByIdResource);

            private readonly Configuration.PackageSource _packageSource;
            private readonly FindPackageByIdResource _inner;
            private readonly IPackageSourceDiagnostics _diagnostics;

            public FindPackageByIdProxyResource(Configuration.PackageSource packageSource, FindPackageByIdResource inner, IPackageSourceDiagnostics diagnostics)
            {
                if (packageSource == null)
                {
                    throw new ArgumentNullException(nameof(packageSource));
                }

                _packageSource = packageSource;

                if (inner == null)
                {
                    throw new ArgumentNullException(nameof(inner));
                }

                _inner = inner;

                if (diagnostics == null)
                {
                    throw new ArgumentNullException(nameof(diagnostics));
                }

                _diagnostics = diagnostics;
            }

            public override Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(string id, CancellationToken token)
            {
                return _diagnostics.TraceActionAsync(
                    _packageSource.Name, 
                    ResourceName, 
                    nameof(FindPackageByIdResource.GetAllVersionsAsync),
                    t => _inner.GetAllVersionsAsync(id, t),
                    token);
            }

            public override Task<FindPackageByIdDependencyInfo> GetDependencyInfoAsync(string id, NuGetVersion version, CancellationToken token)
            {
                return _diagnostics.TraceActionAsync(
                    _packageSource.Name, 
                    ResourceName, 
                    nameof(FindPackageByIdResource.GetDependencyInfoAsync),
                    t => _inner.GetDependencyInfoAsync(id, version, t),
                    token);
            }

            public override Task<Stream> GetNupkgStreamAsync(string id, NuGetVersion version, CancellationToken token)
            {
                return _diagnostics.TraceActionAsync(
                    _packageSource.Name, 
                    ResourceName, 
                    nameof(FindPackageByIdResource.GetNupkgStreamAsync),
                    t => _inner.GetNupkgStreamAsync(id, version, t),
                    token);
            }

            public override Task<PackageIdentity> GetOriginalIdentityAsync(string id, NuGetVersion version, CancellationToken token)
            {
                return _diagnostics.TraceActionAsync(
                    _packageSource.Name, 
                    ResourceName, 
                    nameof(FindPackageByIdResource.GetOriginalIdentityAsync),
                    t => _inner.GetOriginalIdentityAsync(id, version, t),
                    token);
            }
        }

        public RemoteV2FindPackageByIdResourceProvider()
            : base(
                typeof(FindPackageByIdResource),
                name: nameof(RemoteV2FindPackageByIdResourceProvider),
                before: nameof(LocalV2FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            if (sourceRepository.PackageSource.IsHttp
                &&
                !sourceRepository.PackageSource.Source.EndsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);

                var innerResource = new RemoteV2FindPackageByIdResource(
                    sourceRepository.PackageSource,
                    httpSourceResource.HttpSource);

                resource = new FindPackageByIdProxyResource(sourceRepository.PackageSource, innerResource, diagnostics: null);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
