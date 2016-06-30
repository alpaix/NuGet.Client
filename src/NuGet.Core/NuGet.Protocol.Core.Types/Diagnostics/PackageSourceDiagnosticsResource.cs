// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnosticsResource : INuGetResource
    {
        private readonly PackageSource _packageSource;
        private Lazy<IPackageSourceDiagnostics> _packageSourceDiagnostics;

        public IPackageSourceDiagnostics PackageSourceDiagnostics => _packageSourceDiagnostics.Value;

        public PackageSourceDiagnosticsResource(PackageSource packageSource)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException(nameof(packageSource));
            }

            _packageSource = packageSource;
            _packageSourceDiagnostics = new Lazy<IPackageSourceDiagnostics>(() => new PackageSourceDiagnostics(_packageSource));
        }

        public void ResetDiagnosticsData()
        {
            if (_packageSourceDiagnostics.IsValueCreated)
            {
                _packageSourceDiagnostics = new Lazy<IPackageSourceDiagnostics>(() => new PackageSourceDiagnostics(_packageSource));
            }
        }
    }
}