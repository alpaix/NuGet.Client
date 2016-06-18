// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Protocol
{
    public enum EventType
    {
        Cancelled,
        Completed,
        Failed,
        Started
    }

    public class DiagnosticEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public DateTime EventTime { get; }
        public EventType EventType { get; }
        public string CorrelationId { get; }
        public string Resource { get; }
        public string Operation { get; }
        public string Tag { get; }
        public TimeSpan Latency { get; }

        public bool Is(EventType eventType) => EventType == eventType;

        public DiagnosticEvent(
            EventType eventType,
            DateTime eventTime,
            string correlationId,
            string resource,
            string operation,
            string tag,
            TimeSpan latency
        )
        {
            EventType = eventType;
            EventTime = eventTime;
            CorrelationId = correlationId;
            Resource = resource;
            Operation = operation;
            Tag = tag;
            Latency = latency;
        }
    }
}