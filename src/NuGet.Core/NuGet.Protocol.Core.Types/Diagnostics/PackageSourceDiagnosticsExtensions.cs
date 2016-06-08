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
            string resource,
            string operation,
            Func<CancellationToken, Task<T>> taskFactory,
            CancellationToken cancellationToken)
        {
            var tag = Guid.NewGuid().ToString();
            var started = DiagnosticEvents.Started(resource, operation, tag);
            diagnostics.RecordEvent(started);

            T result;
            try
            {
                result = await taskFactory(cancellationToken);
            }
            catch(TaskCanceledException)
            {
                var cancelled = DiagnosticEvents.Cancelled(resource, operation, tag);
                diagnostics.RecordEvent(cancelled);
                throw;
            }
            catch
            {
                var failed = DiagnosticEvents.Failed(resource, operation, tag);
                diagnostics.RecordEvent(failed);
                throw;
            }

            var completed = DiagnosticEvents.Completed(resource, operation, tag);
            diagnostics.RecordEvent(completed);

            return result;
        }
    }
}