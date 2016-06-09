// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class DependencyInfoProxyResource : DependencyInfoResource, IProxyResource
    {
        private static readonly string ResourceName = nameof(DependencyInfoResource);
        private readonly DependencyInfoResource _inner;
        private readonly IPackageSourceDiagnostics _diagnostics;

        public DependencyInfoProxyResource(INuGetResource inner, IPackageSourceDiagnostics diagnostics)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = (DependencyInfoResource)inner;

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            _diagnostics = diagnostics;
        }

        public override Task<SourcePackageDependencyInfo> ResolvePackage(PackageIdentity package, NuGetFramework projectFramework, ILogger log, CancellationToken token)
        {
            return _diagnostics.TraceActionAsync(
                ResourceName,
                nameof(DependencyInfoResource.ResolvePackage),
                t => _inner.ResolvePackage(package, projectFramework, log, t),
                token);
        }

        public override Task<IEnumerable<SourcePackageDependencyInfo>> ResolvePackages(string packageId, NuGetFramework projectFramework, ILogger log, CancellationToken token)
        {
            return _diagnostics.TraceActionAsync(
                ResourceName,
                nameof(DependencyInfoResource.ResolvePackage),
                t => _inner.ResolvePackages(packageId, projectFramework, log, t),
                token);
        }
    }
}