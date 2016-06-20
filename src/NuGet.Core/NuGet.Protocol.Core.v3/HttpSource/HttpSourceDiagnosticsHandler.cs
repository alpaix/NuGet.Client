// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class HttpSourceDiagnosticsHandler : DelegatingHandler
    {
        private readonly PackageSource _packageSource;
        private readonly IPackageSourceDiagnostics _diagnostics;

        public HttpSourceDiagnosticsHandler(PackageSource packageSource, IPackageSourceDiagnostics diagnostics)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException(nameof(packageSource));
            }

            _packageSource = packageSource;

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            _diagnostics = diagnostics;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var configuration = request.GetOrCreateConfiguration();

            var tag = Guid.NewGuid().ToString();
            _diagnostics.RecordEvent(DiagnosticEvents.Started(_packageSource.Name, request.RequestUri.ToString(), tag));

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var timeoutMessage = string.Format(
                CultureInfo.CurrentCulture,
                Strings.Http_Timeout,
                request.Method,
                request.RequestUri.ToString(),
                (int)configuration.RequestTimeout.TotalMilliseconds);

            HttpResponseMessage response;
            try
            {
                response = await TimeoutUtility.StartWithTimeout(
                    timeoutToken => base.SendAsync(request, timeoutToken),
                    configuration.RequestTimeout,
                    timeoutMessage,
                    cancellationToken);
            }
            catch (TaskCanceledException)
            {
                stopWatch.Stop();
                _diagnostics.RecordEvent(DiagnosticEvents.Cancelled(_packageSource.Name, request.RequestUri.ToString(), tag, stopWatch.Elapsed));
                throw;
            }
            catch
            {
                stopWatch.Stop();
                _diagnostics.RecordEvent(DiagnosticEvents.Failed(_packageSource.Name, request.RequestUri.ToString(), tag, stopWatch.Elapsed));
                throw;
            }

            stopWatch.Stop();

            if ((int)response.StatusCode >= 500)
            {
                _diagnostics.RecordEvent(DiagnosticEvents.Failed(_packageSource.Name, request.RequestUri.ToString(), tag, stopWatch.Elapsed));
            }
            else
            {
                _diagnostics.RecordEvent(DiagnosticEvents.Completed(_packageSource.Name, request.RequestUri.ToString(), tag, stopWatch.Elapsed));
            }

            return response;
        }
    }
}