// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class FindLocalPackagesProxyResource : FindLocalPackagesResource, IProxyResource
    {
        private static readonly string ResourceName = nameof(FindLocalPackagesResource);

        private readonly FindLocalPackagesResource _inner;
        private readonly IPackageSourceDiagnostics _diagnostics;

        public FindLocalPackagesProxyResource(INuGetResource inner, IPackageSourceDiagnostics diagnostics)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = (FindLocalPackagesResource)inner;

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            _diagnostics = diagnostics;
        }

        public override IEnumerable<LocalPackageInfo> FindPackagesById(string id, ILogger logger, CancellationToken token)
        {
            return _diagnostics.Trace(
                ResourceName,
                nameof(FindLocalPackagesResource.FindPackagesById),
                () => _inner.FindPackagesById(id, logger, token));
        }

        public override LocalPackageInfo GetPackage(PackageIdentity identity, ILogger logger, CancellationToken token)
        {
            return _diagnostics.Trace(
                ResourceName,
                nameof(FindLocalPackagesResource.GetPackage),
                () => _inner.GetPackage(identity, logger, token));
        }

        public override LocalPackageInfo GetPackage(Uri path, ILogger logger, CancellationToken token)
        {
            return _diagnostics.Trace(
                ResourceName,
                nameof(FindLocalPackagesResource.GetPackage),
                () => _inner.GetPackage(path, logger, token));
        }

        public override IEnumerable<LocalPackageInfo> GetPackages(ILogger logger, CancellationToken token)
        {
            return _diagnostics.Trace(
                ResourceName,
                nameof(FindLocalPackagesResource.GetPackages),
                () => _inner.GetPackages(logger, token));
        }
    }
}