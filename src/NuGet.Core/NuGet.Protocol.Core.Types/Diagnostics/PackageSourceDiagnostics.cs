// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnostics : IPackageSourceDiagnostics
    {
        private readonly IList<DiagnosticEvent> _diagnosticEvents = new List<DiagnosticEvent>();

        public PackageSource PackageSource { get; }

        public IEnumerable<DiagnosticEvent> Events => _diagnosticEvents;

        public PackageSourceDiagnostics(PackageSource packageSource)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException(nameof(packageSource));
            }

            PackageSource = packageSource;
        }

        public void RecordEvent(DiagnosticEvent @event)
        {
            _diagnosticEvents.Add(@event);
        }

        public async Task<T> TraceAsync<T>(string resource, string operation, Func<CancellationToken, Task<T>> taskFactory, CancellationToken cancellationToken)
        {
            var tag = Guid.NewGuid().ToString();
            var started = DiagnosticEvents.Started(resource, operation, tag);
            RecordEvent(started);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            T result;
            try
            {
                result = await taskFactory(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                stopWatch.Stop();
                var cancelled = DiagnosticEvents.Cancelled(resource, operation, tag, stopWatch.Elapsed);
                RecordEvent(cancelled);
                throw;
            }
            catch
            {
                stopWatch.Stop();
                var failed = DiagnosticEvents.Failed(resource, operation, tag, stopWatch.Elapsed);
                RecordEvent(failed);
                throw;
            }

            stopWatch.Stop();
            var completed = DiagnosticEvents.Completed(resource, operation, tag, stopWatch.Elapsed);
            RecordEvent(completed);

            return result;
        }

        public T Trace<T>(string resource, string operation, Func<T> valueFactory)
        {
            var tag = Guid.NewGuid().ToString();
            var started = DiagnosticEvents.Started(resource, operation, tag);
            RecordEvent(started);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            T result;
            try
            {
                result = valueFactory();
            }
            catch (TaskCanceledException)
            {
                stopWatch.Stop();
                var cancelled = DiagnosticEvents.Cancelled(resource, operation, tag, stopWatch.Elapsed);
                RecordEvent(cancelled);
                throw;
            }
            catch
            {
                stopWatch.Stop();
                var failed = DiagnosticEvents.Failed(resource, operation, tag, stopWatch.Elapsed);
                RecordEvent(failed);
                throw;
            }

            stopWatch.Stop();
            var completed = DiagnosticEvents.Completed(resource, operation, tag, stopWatch.Elapsed);
            RecordEvent(completed);

            return result;
        }
    }
}