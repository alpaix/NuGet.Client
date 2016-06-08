// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnosticsResource : INuGetResource
    {
        public IPackageSourceDiagnostics PackageSourceDiagnostics { get; set; }
    }
}