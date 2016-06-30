// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.ProjectManagement;
using NuGet.Protocol;

namespace NuGet.PackageManagement.UI
{
    public class PackageSourceDiagnosticsObserver : IObserver<DiagnosticMessage>
    {
        private readonly IActionEventSink _actionEventSink;
        private readonly INuGetProjectContext _logger;

        private readonly List<string> _warnings = new List<string>();

        public PackageSourceDiagnosticsObserver(
            IActionEventSink actionEventSink,
            INuGetProjectContext logger)
        {
            _actionEventSink = actionEventSink;
            _logger = logger;
        }
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticMessage value)
        {
            _logger.Log(MessageLevel.Warning, $"[PSD] {value.Details}");

            var warning = $"{value.PackageSource.Name} performance warning. {Convert(value.SourceStatus)}";
            if (!_warnings.Contains(warning))
            {
                _warnings.Add(warning);
                _actionEventSink.OnWarning(warning);
            }
        }

        private static string Convert(SourceStatus sourceStatus)
        {
            switch (sourceStatus)
            {
                case SourceStatus.SlowSource:
                    return "Source is slow.";
                case SourceStatus.UnavailableSource:
                    return "Source is not available.";
                case SourceStatus.UnreliableSource:
                    return "Source is not reliable.";
                case SourceStatus.UnresponsiveSource:
                    return "Source is not responsive.";
            }

            throw new ArgumentException("Unexpected source status", nameof(sourceStatus));
        }
    }
}
