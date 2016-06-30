// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnostics : IPackageSourceDiagnostics
    {
        private static readonly TimeSpan SlowSourceThreshold = TimeSpan.FromSeconds(5.0);
        private static readonly TimeSpan UnresponsiveSourceThreshold = TimeSpan.FromSeconds(5.0);

        private ConcurrentQueue<DiagnosticEvent> _diagnosticEvents = new ConcurrentQueue<DiagnosticEvent>();

        public PackageSource PackageSource { get; }

        public IEnumerable<DiagnosticEvent> Events => _diagnosticEvents;

        public IEnumerable<DiagnosticMessage> DiagnosticMessages
        {
            get
            {
                var metrics = UpdateMetrics(DateTime.UtcNow);

                var messages = new List<DiagnosticMessage>();

                if (metrics.TotalRequestsCount == 0)
                {
                    yield break;
                }

                if (metrics.SlowRequestsCount > 0)
                {
                    yield return new DiagnosticMessage(
                        PackageSource,
                        SourceStatus.SlowSource,
                        $"[{PackageSource.Name}] {FormatRate(metrics.SlowRequestsCount, metrics.TotalRequestsCount)} of source requests took more than {DatetimeUtility.ToReadableTimeFormat(SlowSourceThreshold)}.");
                }

                if (metrics.CancelledRequestsCount > 0)
                {
                    yield return new DiagnosticMessage(
                        PackageSource,
                        SourceStatus.UnreliableSource,
                        $"[{PackageSource.Name}] {FormatRate(metrics.CancelledRequestsCount, metrics.TotalRequestsCount)} of source requests were cancelled.");
                }

                if (metrics.FailedRequestsCount > 0)
                {
                    yield return new DiagnosticMessage(
                        PackageSource,
                        SourceStatus.UnreliableSource,
                        $"[{PackageSource.Name}] {FormatRate(metrics.FailedRequestsCount, metrics.TotalRequestsCount)} of source requests failed.");
                }

                if (metrics.IncompleteRequestsCount > 0)
                {
                    yield return new DiagnosticMessage(
                        PackageSource,
                        SourceStatus.SlowSource,
                        $"[{PackageSource.Name}] {FormatRate(metrics.IncompleteRequestsCount, metrics.TotalRequestsCount)} of source requests are incomplete.");
                }

                if (metrics.TimedOutRequestsCount > 0)
                {
                    yield return new DiagnosticMessage(
                        PackageSource,
                        SourceStatus.UnresponsiveSource,
                        $"[{PackageSource.Name}] {FormatRate(metrics.TimedOutRequestsCount, metrics.TotalRequestsCount)} of source requests timed out");
                }

                yield break;
            }
        }

        private static string FormatRate(int numerator, int denominator)
        {
            if (numerator == 0)
            {
                return $"None of {denominator}";
            }

            if (numerator == denominator)
            {
                return $"All of {denominator}";
            }

            //var rate = (double)numerator / denominator;
            //return rate.ToString("0.0%");
            return $"{numerator}/{denominator}";
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
            _diagnosticEvents.Enqueue(@event);
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

        public class DiagnosticMetrics
        {
            public int CancelledRequestsCount { get; set; }
            public int FailedRequestsCount { get; set; }
            public int IncompleteRequestsCount { get; set; }
            public int SuccessfulRequestsCount { get; set; }
            public int TimedOutRequestsCount { get; set; }
            public int TotalRequestsCount { get; set; }
            public int SlowRequestsCount { get; set; }
        }

        private DiagnosticMetrics _metrics;

        public DiagnosticMetrics UpdateMetrics(DateTime referenceTime)
        {
            var requests = Events
                .ToArray()
                .GroupBy(
                    e => e.Tag,
                    (k, es) =>
                    {
                        var completeEvent = es.FirstOrDefault(e => !e.Is(EventType.Started));
                        if (completeEvent != null)
                        {
                            return new { result = completeEvent?.EventType, duration = completeEvent.Latency };
                        }

                        var startedAt = es.First(e => e.Is(EventType.Started)).EventTime;
                        return new { result = completeEvent?.EventType, duration = referenceTime - startedAt };
                    });

            _metrics = requests
                .Aggregate(new DiagnosticMetrics(), (m, r) =>
                    {
                        m.TotalRequestsCount++;

                        if (!r.result.HasValue)
                        {
                            m.IncompleteRequestsCount++;
                            if (r.duration > SlowSourceThreshold)
                            {
                                m.SlowRequestsCount++;
                            }
                        }
                        else if (r.result == EventType.Cancelled)
                        {
                            m.CancelledRequestsCount++;
                        }
                        else if (r.result == EventType.Failed)
                        {
                            m.FailedRequestsCount++;
                        }
                        else if (r.result == EventType.Completed)
                        {
                            m.SuccessfulRequestsCount++;
                            if (r.duration > SlowSourceThreshold)
                            {
                                m.SlowRequestsCount++;
                            }
                        }
                        else if (r.result == EventType.TimedOut)
                        {
                            m.TimedOutRequestsCount++;
                        }

                        return m;
                    });

            return _metrics;
        }

        public void Reset()
        {
            _diagnosticEvents = new ConcurrentQueue<DiagnosticEvent>();
        }
    }
}