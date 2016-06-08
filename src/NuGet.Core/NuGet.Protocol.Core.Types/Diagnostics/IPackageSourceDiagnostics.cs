// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public interface IPackageSourceDiagnostics
    {
        PackageSource PackageSource { get; }

        IEnumerable<DiagnosticEvent> Events { get; }

        // source, resource, activity, operation?, started, complete-status, completed
        void RecordEvent(DiagnosticEvent @event);
    }
}