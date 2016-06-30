// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnosticsResourceProvider : ResourceProvider
    {
        private readonly ConcurrentDictionary<PackageSource, PackageSourceDiagnosticsResource> _cache = new ConcurrentDictionary<PackageSource, PackageSourceDiagnosticsResource>();

        public PackageSourceDiagnosticsResourceProvider()
            : base(typeof(PackageSourceDiagnosticsResource))
        {
        }

        public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = _cache.GetOrAdd(
                sourceRepository.PackageSource,
                valueFactory: s => new PackageSourceDiagnosticsResource(s));

            return Task.FromResult(Tuple.Create(true, resource));
        }
    }
}