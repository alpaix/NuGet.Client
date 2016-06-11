// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Protocol
{
    public class DiagnosticMessage
    {
        public SourceStatus SourceStatus { get; }
        public string Details { get; }

        public DiagnosticMessage(SourceStatus sourceStatus, string details)
        {
            SourceStatus = sourceStatus;
            Details = details;
        }
    }
}
