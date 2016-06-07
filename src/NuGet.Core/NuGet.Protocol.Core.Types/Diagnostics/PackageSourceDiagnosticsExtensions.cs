// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Protocol
{
    public static class PackageSourceDiagnosticsExtensions
    {
        public static async Task<T> TraceActionAsync<T>(
            this IPackageSourceDiagnostics diagnostics,
            string source,
            string resource,
            string operation,
            Func<CancellationToken, Task<T>> taskFactory,
            CancellationToken token)
        {
            var started = DiagnosticEvents.Started(source, resource, operation);
            diagnostics.RecordEvent(started);

            T result;
            try
            {
                result = await taskFactory(token);
            }
            catch
            {
                var failed = DiagnosticEvents.Failed(source, resource, operation);
                diagnostics.RecordEvent(failed);
                throw;
            }

            var completed = DiagnosticEvents.Completed(source, resource, operation);
            diagnostics.RecordEvent(completed);

            return result;
        }
    }
}