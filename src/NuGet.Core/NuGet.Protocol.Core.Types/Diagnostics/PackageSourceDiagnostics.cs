// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.Configuration;

namespace NuGet.Protocol
{
    public class PackageSourceDiagnostics : IPackageSourceDiagnostics
    {
        private readonly IList<DiagnosticEvent> _diagnosticEvents = new List<DiagnosticEvent>();

        public PackageSource PackageSource { get; }

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
            _diagnosticEvents.Add(@event);
        }
    }
}