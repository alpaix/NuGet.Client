// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class DiagnosticMessage
    {
        public PackageSource PackageSource { get; }
        public SourceStatus SourceStatus { get; }
        public string Details { get; }

        public DiagnosticMessage(PackageSource packageSource, SourceStatus sourceStatus, string details)
        {
            PackageSource = packageSource;
            SourceStatus = sourceStatus;
            Details = details;
        }
    }
}
