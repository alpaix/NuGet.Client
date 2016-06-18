// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnostics : IPackageSourceDiagnostics
    {
        private static readonly TimeSpan SlowSourceThreshold = TimeSpan.FromSeconds(5.0);
        private static readonly TimeSpan UnresponsiveSourceThreshold = TimeSpan.FromSeconds(5.0);

        private readonly IList<DiagnosticEvent> _diagnosticEvents = new List<DiagnosticEvent>();

        public PackageSource PackageSource { get; }

        public IEnumerable<DiagnosticEvent> Events => _diagnosticEvents;

        public IReadOnlyList<DiagnosticMessage> DiagnosticMessages
        {
            get
            {
                var messages = new List<DiagnosticMessage>();

                if (IsPackageSourceSlow())
                {
                    messages.Add(new DiagnosticMessage(
                        SourceStatus.SlowSource,
                        $"[{PackageSource.Name}] source is slow"));
                }

                if (IsPackageSourceUnresponsive(DateTime.UtcNow))
                {
                    messages.Add(new DiagnosticMessage(
                        SourceStatus.UnresponsiveSource,
                        $"[{PackageSource.Name}] source is unresponsive"));
                }

                return messages;
            }
        }

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
            RecordEvent(DiagnosticEvents.Started(resource, operation, tag));

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
                RecordEvent(DiagnosticEvents.Cancelled(resource, operation, tag, stopWatch.Elapsed));
                throw;
            }
            catch
            {
                stopWatch.Stop();
                RecordEvent(DiagnosticEvents.Failed(resource, operation, tag, stopWatch.Elapsed));
                throw;
            }

            stopWatch.Stop();
            RecordEvent(DiagnosticEvents.Completed(resource, operation, tag, stopWatch.Elapsed));

            return result;
        }

        public T Trace<T>(string resource, string operation, Func<T> valueFactory)
        {
            var tag = Guid.NewGuid().ToString();
            RecordEvent(DiagnosticEvents.Started(resource, operation, tag));

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
                RecordEvent(DiagnosticEvents.Cancelled(resource, operation, tag, stopWatch.Elapsed));
                throw;
            }
            catch
            {
                stopWatch.Stop();
                RecordEvent(DiagnosticEvents.Failed(resource, operation, tag, stopWatch.Elapsed));
                throw;
            }

            stopWatch.Stop();
            RecordEvent(DiagnosticEvents.Completed(resource, operation, tag, stopWatch.Elapsed));

            return result;
        }

        public bool IsPackageSourceSlow()
        {
            var isSlow = Events
                .Select(e => e.Latency)
                .Any(t => t != TimeSpan.Zero && t > SlowSourceThreshold);
            return isSlow;
        }

        public bool IsPackageSourceUnresponsive(DateTime referenceTime)
        {
            var completedOperations = Events
                .Where(e => e.Is(EventType.Cancelled) || e.Is(EventType.Completed) || e.Is(EventType.Failed))
                .Select(e => e.Tag);

            var incompleteOperations = Events
                .Where(e => e.Is(EventType.Started))
                .Select(e => e.Tag)
                .Except(completedOperations)
                .ToArray();

            if (incompleteOperations.Length > 0)
            {
                var isUnresponsive = Events
                    .Where(e => e.Is(EventType.Started))
                    .Join(incompleteOperations, e => e.Tag, tag => tag, (e, tag) => referenceTime - e.EventTime)
                    .Any(t => t > UnresponsiveSourceThreshold);
                return isUnresponsive;
            }

            return false;
        }
    }
}