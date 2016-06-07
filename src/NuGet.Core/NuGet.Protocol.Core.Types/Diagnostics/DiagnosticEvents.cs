// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Common;

namespace NuGet.Protocol
{
    public static class DiagnosticEvents
    {
        public static DiagnosticEvent Started(string source, string resource, string operation)
        {
            return new DiagnosticEvent(
                eventId: Guid.NewGuid().ToString(),
                eventTime: DateTime.UtcNow,
                source: source,
                resource: resource,
                activity: ActivityCorrelationContext.Current.CorrelationId,
                operation: operation,
                eventType: EventType.Started
            );
        }

        public static DiagnosticEvent Failed(string source, string resource, string operation)
        {
            return new DiagnosticEvent(
                eventId: Guid.NewGuid().ToString(),
                eventTime: DateTime.UtcNow,
                source: source,
                resource: resource,
                activity: ActivityCorrelationContext.Current.CorrelationId,
                operation: operation,
                eventType: EventType.Failed
            );
        }

        public static DiagnosticEvent Completed(string source, string resource, string operation)
        {
            return new DiagnosticEvent(
                eventId: Guid.NewGuid().ToString(),
                eventTime: DateTime.UtcNow,
                source: source,
                resource: resource,
                activity: ActivityCorrelationContext.Current.CorrelationId,
                operation: operation,
                eventType: EventType.Completed
            );
        }
    }
}