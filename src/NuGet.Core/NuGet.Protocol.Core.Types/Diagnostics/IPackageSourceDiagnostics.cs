// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public interface IPackageSourceDiagnostics
    {
        PackageSource PackageSource { get; }

        IEnumerable<DiagnosticEvent> Events { get; }

        IReadOnlyList<DiagnosticMessage> DiagnosticMessages { get; }

        // source, resource, activity, operation?, started, complete-status, completed
        void RecordEvent(DiagnosticEvent @event);

        Task<T> TraceAsync<T>(
            string resource,
            string operation,
            Func<CancellationToken, Task<T>> taskFactory,
            CancellationToken cancellationToken);

        T Trace<T>(
            string resource,
            string operation,
            Func<T> valueFactory);
    }
}