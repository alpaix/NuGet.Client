// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI
{
    public class PackageSourceDiagnosticsWorker : IObservable<DiagnosticMessage>
    {
        private readonly List<IObserver<DiagnosticMessage>> _observers = new List<IObserver<DiagnosticMessage>>();
        private readonly IEnumerable<SourceRepository> _activeSources;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private JoinableTask _task;

        public PackageSourceDiagnosticsWorker(IEnumerable<SourceRepository> activeSources)
        {
            _activeSources = activeSources;
        }

        public static PackageSourceDiagnosticsWorker Create(INuGetUI uiService)
        {
            if (uiService == null)
            {
                throw new ArgumentNullException(nameof(uiService));
            }

            var observer = new PackageSourceDiagnosticsObserver(uiService.ActionEventSink, uiService.ProgressWindow);

            var worker = new PackageSourceDiagnosticsWorker(uiService.ActiveSources);
            worker.Subscribe(observer);

            return worker;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var diagnosticsResources = await GetDiagnosticsResources(cancellationToken);

            foreach (var resource in diagnosticsResources)
            {
                resource.ResetDiagnosticsData();
            }

            _task = NuGetUIThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5.5), _cts.Token);
                    await RetrieveDiagnosticMessagesAsync(
                        diagnosticsResources.Select(r => r.PackageSourceDiagnostics),
                        _cts.Token);
                }
            });
        }

        private async Task<List<PackageSourceDiagnosticsResource>> GetDiagnosticsResources(CancellationToken cancellationToken)
        {
            var diagnosticsSources = new List<PackageSourceDiagnosticsResource>();

            foreach (var sourceRepository in _activeSources)
            {
                var dr = await sourceRepository.GetResourceAsync<PackageSourceDiagnosticsResource>(
                    cancellationToken);
                diagnosticsSources.Add(dr);
            }

            return diagnosticsSources;
        }

        private async Task RetrieveDiagnosticMessagesAsync(IEnumerable<IPackageSourceDiagnostics> diagnosticsSources, CancellationToken token)
        {
            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);

            foreach (var diagnosticMessage in diagnosticsSources.SelectMany(d => d.DiagnosticMessages))
            {
                foreach (var observer in _observers)
                {
                    observer.OnNext(diagnosticMessage);
                }
            }

            // Go off the UI thread to perform non-UI operations
            await TaskScheduler.Default;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            await _task?.JoinAsync(cancellationToken);
        }

        public IDisposable Subscribe(IObserver<DiagnosticMessage> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber(() => _observers.Remove(observer));
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsubscribeAction;

            public Unsubscriber(Action unsubscribeAction)
            {
                if (unsubscribeAction == null)
                {
                    throw new ArgumentNullException(nameof(unsubscribeAction));
                }

                _unsubscribeAction = unsubscribeAction;
            }

            public void Dispose()
            {
                _unsubscribeAction();
            }
        }
    }
}
