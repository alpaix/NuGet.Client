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
    public class FindPackageByIdProxyResource : FindPackageByIdResource, IProxyResource
    {
        private static readonly string ResourceName = nameof(FindPackageByIdResource);

        private readonly FindPackageByIdResource _inner;
        private readonly IPackageSourceDiagnostics _diagnostics;

        public FindPackageByIdProxyResource(INuGetResource inner, IPackageSourceDiagnostics diagnostics)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = (FindPackageByIdResource)inner;

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            _diagnostics = diagnostics;
        }

        public override Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(string id, CancellationToken token)
        {
            return _diagnostics.TraceAsync(
                ResourceName,
                nameof(FindPackageByIdResource.GetAllVersionsAsync),
                t => _inner.GetAllVersionsAsync(id, t),
                token);
        }

        public override Task<FindPackageByIdDependencyInfo> GetDependencyInfoAsync(string id, NuGetVersion version, CancellationToken token)
        {
            return _diagnostics.TraceAsync(
                ResourceName,
                nameof(FindPackageByIdResource.GetDependencyInfoAsync),
                t => _inner.GetDependencyInfoAsync(id, version, t),
                token);
        }

        public override Task<Stream> GetNupkgStreamAsync(string id, NuGetVersion version, CancellationToken token)
        {
            return _diagnostics.TraceAsync(
                ResourceName,
                nameof(FindPackageByIdResource.GetNupkgStreamAsync),
                t => _inner.GetNupkgStreamAsync(id, version, t),
                token);
        }

        public override Task<PackageIdentity> GetOriginalIdentityAsync(string id, NuGetVersion version, CancellationToken token)
        {
            return _diagnostics.TraceAsync(
                ResourceName,
                nameof(FindPackageByIdResource.GetOriginalIdentityAsync),
                t => _inner.GetOriginalIdentityAsync(id, version, t),
                token);
        }
    }
}