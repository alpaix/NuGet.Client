// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnosticsResource : INuGetResource
    {
        public PackageSourceDiagnostics PackageSourceDiagnostics { get; }

        public PackageSourceDiagnosticsResource(PackageSource packageSource)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException(nameof(packageSource));
            }

            PackageSourceDiagnostics = new PackageSourceDiagnostics(packageSource);
        }

        public void ResetDiagnosticsData()
        {
            PackageSourceDiagnostics.Reset();
        }
    }
}