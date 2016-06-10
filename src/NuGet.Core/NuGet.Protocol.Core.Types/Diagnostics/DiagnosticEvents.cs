// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Common;

namespace NuGet.Protocol
{
    public static class DiagnosticEvents
    {
        public static DiagnosticEvent Started(string resource, string operation, string tag)
        {
            return new DiagnosticEvent(
                eventType: EventType.Started,
                eventTime: DateTime.UtcNow,
                correlationId: ActivityCorrelationContext.Current.CorrelationId,
                resource: resource,
                operation: operation,
                tag: tag,
                latency: TimeSpan.Zero
            );
        }

        public static DiagnosticEvent Failed(string resource, string operation, string tag, TimeSpan latency)
        {
            return new DiagnosticEvent(
                eventType: EventType.Failed,
                eventTime: DateTime.UtcNow,
                correlationId: ActivityCorrelationContext.Current.CorrelationId,
                resource: resource,
                operation: operation,
                tag: tag,
                latency: latency
            );
        }

        public static DiagnosticEvent Cancelled(string resource, string operation, string tag, TimeSpan latency)
        {
            return new DiagnosticEvent(
                eventType: EventType.Cancelled,
                eventTime: DateTime.UtcNow,
                correlationId: ActivityCorrelationContext.Current.CorrelationId,
                resource: resource,
                operation: operation,
                tag: tag,
                latency: latency
            );
        }

        public static DiagnosticEvent Completed(string resource, string operation, string tag, TimeSpan latency)
        {
            return new DiagnosticEvent(
                eventType: EventType.Completed,
                eventTime: DateTime.UtcNow,
                correlationId: ActivityCorrelationContext.Current.CorrelationId,
                resource: resource,
                operation: operation,
                tag: tag,
                latency: latency
            );
        }
    }
}