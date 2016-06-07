// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Protocol
{
    public enum EventType
    {
        Started,
        Failed,
        Completed
    }

    public class DiagnosticEvent
    {
        public string EventId { get; }
        public DateTime EventTime { get; }
        public EventType EventType { get; }
        public string Source { get; }
        public string Resource { get; }
        public string Activity { get; }
        public string Operation { get; }

        public DiagnosticEvent(
            string eventId,
            DateTime eventTime,
            EventType eventType,
            string source,
            string resource,
            string activity,
            string operation
        )
        {
            EventId = eventId;
            EventTime = eventTime;
            EventType = eventType;
            Source = source;
            Resource = resource;
            Activity = activity;
            Operation = operation;
        }
    }
}