// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class DependencyInfoResourceV2FeedProvider : ResourceProvider
    {
        public DependencyInfoResourceV2FeedProvider()
            : base(typeof(DependencyInfoResource),
              nameof(DependencyInfoResourceV2FeedProvider),
              "DependencyInfoResourceV2Provider")
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;

            if (await sourceRepository.GetFeedType(token) == FeedType.HttpV2)
            {
                var serviceDocument = await sourceRepository.GetResourceAsync<ODataServiceDocumentResourceV2>(token);

                var httpSource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
                var parser = new V2FeedParser(httpSource.HttpSource, serviceDocument.BaseAddress, sourceRepository.PackageSource);

                resource = await ProxyResourceFactory.CreateDiagnosticsProxyResourceAsync<DependencyInfoProxyResource>(
                    sourceRepository,
                    innerResource: new DependencyInfoResourceV2Feed(parser, sourceRepository),
                    cancellationToken: token);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
