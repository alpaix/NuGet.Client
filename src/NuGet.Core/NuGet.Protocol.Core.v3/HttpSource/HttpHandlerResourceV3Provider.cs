﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class HttpHandlerResourceV3Provider : ResourceProvider
    {
        public HttpHandlerResourceV3Provider()
            : base(typeof(HttpHandlerResource),
                  nameof(HttpHandlerResourceV3Provider),
                  NuGetResourceProviderPositions.Last)
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            Debug.Assert(sourceRepository.PackageSource.IsHttp, "HTTP handler requested for a non-http source.");

            INuGetResource resource = null;

            if (sourceRepository.PackageSource.IsHttp)
            {
                var diagnosticsResource = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>(token);
                resource = CreateResource(sourceRepository.PackageSource, diagnosticsResource?.PackageSourceDiagnostics);
            }

            return Tuple.Create(resource != null, resource);
        }

        private static HttpHandlerResourceV3 CreateResource(PackageSource packageSource, IPackageSourceDiagnostics diagnostics)
        {
            var sourceUri = packageSource.SourceUri;
            var proxy = ProxyCache.Instance.GetProxy(sourceUri);

            // replace the handler with the proxy aware handler
            var clientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            };

            // HTTP handler pipeline can be injected here, around the client handler
            HttpMessageHandler messageHandler = new HttpSourceDiagnosticsHandler(packageSource, diagnostics)
            {
                InnerHandler = clientHandler
            };

            if (proxy != null)
            {
                messageHandler = new ProxyAuthenticationHandler(clientHandler, HttpHandlerResourceV3.CredentialService, ProxyCache.Instance)
                {
                    InnerHandler = messageHandler
                };
            }

#if !IS_CORECLR
            {
                messageHandler = new StsAuthenticationHandler(packageSource, TokenStore.Instance)
                {
                    InnerHandler = messageHandler
                };
            }
#endif
            {
                messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService)
                {
                    InnerHandler = messageHandler
                };
            }

            var resource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            return resource;
        }
    }
}