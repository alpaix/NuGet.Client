// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Protocol
{
    public enum EventType
    {
        Started,
        Failed,
        Cancelled,
        Completed
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

        public DiagnosticEvent(
            EventType eventType,
            DateTime eventTime,
            string correlationId,
            string resource,
            string operation,
            string tag
        )
        {
            EventType = eventType;
            EventTime = eventTime;
            CorrelationId = correlationId;
            Resource = resource;
            Operation = operation;
            Tag = tag;
        }
    }
}