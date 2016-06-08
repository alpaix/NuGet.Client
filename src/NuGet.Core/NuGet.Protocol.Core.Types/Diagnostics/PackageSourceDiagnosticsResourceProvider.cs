// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnosticsResourceProvider : ResourceProvider
    {
        public PackageSourceDiagnosticsResourceProvider()
            : base(typeof(PackageSourceDiagnosticsResource))
        {
        }

        public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            var diagnostics = new PackageSourceDiagnostics(sourceRepository.PackageSource);
            INuGetResource resource = new PackageSourceDiagnosticsResource
            {
                PackageSourceDiagnostics = diagnostics
            };

            return Task.FromResult(Tuple.Create(true, resource));
        }
    }
}