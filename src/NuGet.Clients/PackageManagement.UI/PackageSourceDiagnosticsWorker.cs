// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using NuGet.Configuration;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
    public class PackageSourceDiagnosticsWorker
    {
        private readonly IEnumerable<SourceRepository> _activeSources;
        private readonly IActionEventSink _actionEventSink;
        private readonly INuGetProjectContext _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private JoinableTask _task;

        public PackageSourceDiagnosticsWorker(
            IEnumerable<SourceRepository> activeSources,
            IActionEventSink actionEventSink,
            INuGetProjectContext logger)
        {
            _activeSources = activeSources;
            _actionEventSink = actionEventSink;
            _logger = logger;
        }

        public static PackageSourceDiagnosticsWorker Create(INuGetUI uiService)
        {
            if (uiService == null)
            {
                throw new ArgumentNullException(nameof(uiService));
            }

            return new PackageSourceDiagnosticsWorker(uiService.ActiveSources, uiService.ActionEventSink, uiService.ProgressWindow);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            foreach (var sourceRepository in _activeSources)
            {
                var dr = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>(cancellationToken);
                dr.ResetDiagnosticsData();
            }

            _task = NuGetUIThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5.5), _cts.Token);
                    await RetrieveDiagnosticsWarningsAsync();
                }
            });
        }

        private async Task RetrieveDiagnosticsWarningsAsync()
        {
            foreach (var sourceRepository in _activeSources)
            {
                var dr = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>(_cts.Token);
                var d = dr.PackageSourceDiagnostics;
                var diagnosticMessages = d.DiagnosticMessages.ToArray();
                if (diagnosticMessages.Length > 0)
                {
                    await ProcessDiagnosticsMessagesAsync(sourceRepository.PackageSource, diagnosticMessages);
                }
            }
        }

        private async Task ProcessDiagnosticsMessagesAsync(
            PackageSource packageSource,
            IEnumerable<DiagnosticMessage> diagnosticMessages)
        {
            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (var msg in diagnosticMessages)
            {
                _logger.Log(MessageLevel.Warning, $"[PSD] {msg.Details}");
            }

            var warnings = diagnosticMessages.Select(m => Convert(m.SourceStatus));
            var reportedWarnings = _warnings
                .Where(w => w.PackageSource == packageSource)
                .Select(w => w.WarningCode);
            var newWarnings = warnings
                .Except(reportedWarnings)
                .ToArray();

            foreach(var warning in newWarnings)
            {
                _warnings.Add(new DiagnosticsWarning
                {
                    PackageSource = packageSource,
                    WarningCode = warning
                });

                _actionEventSink.OnWarning($"{packageSource.Name} performance warning. {warning}.");
            }

            // Go off the UI thread to perform non-UI operations
            await TaskScheduler.Default;
        }

        private static string Convert(SourceStatus sourceStatus)
        {
            switch (sourceStatus)
            {
                case SourceStatus.SlowSource:
                    return "Source is slow.";
                case SourceStatus.UnavailableSource:
                    return "Source is not available";
                case SourceStatus.UnreliableSource:
                    return "Source is not reliable";
                case SourceStatus.UnresponsiveSource:
                    return "Source is not responsive";
            }

            throw new ArgumentException("Unexpected source status", nameof(sourceStatus));
        }

        private readonly List<DiagnosticsWarning> _warnings = new List<DiagnosticsWarning>();

        private class DiagnosticsWarning
        {
            public PackageSource PackageSource { get; set; }
            public string WarningCode { get; set; }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            await _task?.JoinAsync(cancellationToken);

            await RetrieveDiagnosticsWarningsAsync();
        }
    }
}
