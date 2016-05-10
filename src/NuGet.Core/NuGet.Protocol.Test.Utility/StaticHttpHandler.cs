﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Test.Utility
{
    public static class StaticHttpHandler
    {
        /// <summary>
        /// Creates a handler to override url requests to static content
        /// </summary>
        public static TestHttpHandlerProvider CreateHttpHandler(Dictionary<string, string> responses, string errorContent = "")
        {
            return new TestHttpHandlerProvider(() => new TestMessageHandler(responses, errorContent));
        }

        /// <summary>
        /// Creates a source and injects an http handler to override the normal http calls
        /// </summary>
        public static SourceRepository CreateSource(string sourceUrl, IEnumerable<Lazy<INuGetResourceProvider>> providers, Dictionary<string, string> responses, string errorContent = "")
        {
            var handler = new Lazy<INuGetResourceProvider>(() => CreateHttpHandler(responses, errorContent));

            return new SourceRepository(new PackageSource(sourceUrl), providers.Concat(new Lazy<INuGetResourceProvider>[] { handler }));
        }
    }

    public class TestHttpHandlerProvider : ResourceProvider
    {
        private Func<HttpClientHandler> _messageHandlerFactory;

        public TestHttpHandlerProvider(Func<HttpClientHandler> messageHandlerFactory)
            : base(typeof(HttpHandlerResource), "testhandler", NuGetResourceProviderPositions.First)
        {
            _messageHandlerFactory = messageHandlerFactory;
        }

        public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
        {
            var result = new Tuple<bool, INuGetResource>(true, new TestHttpHandler(_messageHandlerFactory()));
            return Task.FromResult(result);
        }
    }

    public class TestHttpHandler : HttpHandlerResource
    {
        private HttpClientHandler _messageHandler;

        public TestHttpHandler(HttpClientHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public override HttpClientHandler ClientHandler
        {
            get { return _messageHandler; }
        }

        public override HttpMessageHandler MessageHandler
        {
            get
            {
                return _messageHandler;
            }
        }
    }

    public class TestMessageHandler : HttpClientHandler
    {
        private Dictionary<string, string> _responses;
        private string _errorContent;

        public TestMessageHandler(Dictionary<string, string> responses, string errorContent)
        {
            _responses = responses;
            _errorContent = errorContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncPublic(request);
        }

        public virtual Task<HttpResponseMessage> SendAsyncPublic(HttpRequestMessage request)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);

            string source;
            if (_responses.TryGetValue(request.RequestUri.AbsoluteUri, out source))
            {
                // TODO: Make this test infrastructure not a big hack.
                if (source == null)
                {
                    msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    msg.Content = new TestContent(_errorContent);
                }
                else if (source == string.Empty)
                {
                    msg = new HttpResponseMessage(HttpStatusCode.NotFound);
                    msg.Content = new TestContent(_errorContent);
                }
                else if (source == "204")
                {
                    msg = new HttpResponseMessage(HttpStatusCode.NoContent);
                    msg.Content = new TestContent(string.Empty);
                }
                else if (source.StartsWith("301 "))
                {
                    var url = source.Substring(4);
                    msg = new HttpResponseMessage(HttpStatusCode.MovedPermanently)
                    {
                        RequestMessage = new HttpRequestMessage(HttpMethod.Get, url),
                        Content = new TestContent(string.Empty)
                    };
                }
                else
                {
                    msg.Content = new TestContent(source);
                }
            }
            else
            {
                throw new Exception("Unhandled test request: " + request.RequestUri.AbsoluteUri);
            }

            return Task.FromResult(msg);
        }
    }

    public class TestContent : HttpContent
    {
        private MemoryStream _stream;

        public TestContent(string s)
        {
            _stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
            _stream.Seek(0, SeekOrigin.Begin);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return _stream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = (long)_stream.Length;
            return true;
        }
    }
}
