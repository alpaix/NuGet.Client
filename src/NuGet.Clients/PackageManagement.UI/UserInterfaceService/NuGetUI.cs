// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace NuGet.PackageManagement.UI
{
    public class NuGetUI : INuGetUI
    {
        public const string LogEntrySource = "NuGet Package Manager";

        private readonly INuGetUIContext _context;
        private readonly NuGetUIProjectContext _projectContext;

        public NuGetUI(
            INuGetUIContext context,
            NuGetUIProjectContext projectContext)
        {
            _context = context;
            _projectContext = projectContext;
        }

        public bool PromptForLicenseAcceptance(IEnumerable<PackageLicenseInfo> packages)
        {
            var result = false;

            UIDispatcher.Invoke(() => { result = PromptForLicenseAcceptanceImpl(packages); });

            return result;
        }

        private static bool PromptForLicenseAcceptanceImpl(
            IEnumerable<PackageLicenseInfo> packages)
        {
            var licenseWindow = new LicenseAcceptanceWindow
            {
                DataContext = packages
            };

            using (NuGetEventTrigger.TriggerEventBeginEnd(
                NuGetEvent.LicenseWindowBegin,
                NuGetEvent.LicenseWindowEnd))
            {
                var dialogResult = licenseWindow.ShowModal();
                return dialogResult ?? false;
            }
        }

        public void LaunchExternalLink(Uri url)
        {
            UIUtility.LaunchExternalLink(url);
        }

        public void LaunchNuGetOptionsDialog(OptionsPage optionsPageToOpen)
        {
            if (_context?.OptionsPageActivator != null)
            {
                UIDispatcher.Invoke(() => { _context.OptionsPageActivator.ActivatePage(optionsPageToOpen, null); });
            }
            else
            {
                MessageBox.Show("Options dialog is not available in the standalone UI");
            }
        }

        public bool PromptForPreviewAcceptance(IEnumerable<PreviewResult> actions)
        {
            var result = false;

            if (actions.Any())
            {
                UIDispatcher.Invoke(() =>
                {
                    var w = new PreviewWindow(_context);
                    w.DataContext = new PreviewWindowModel(actions);

                    if (StandaloneSwitch.IsRunningStandalone
                        && _detailControl != null)
                    {
                        var win = Window.GetWindow(_detailControl);
                        w.Owner = win;
                    }

                    result = w.ShowModal() == true;
                });
            }
            else
            {
                return true;
            }

            return result;
        }

        // TODO: rename it to something like Start
        public void ShowProgressDialog(DependencyObject ownerWindow)
        {
            _projectContext.Start();
            _projectContext.FileConflictAction = FileConflictAction;
        }

        // TODO: rename it to something like End
        public void CloseProgressDialog()
        {
            _projectContext.End();
        }

        // TODO: rename it
        public INuGetProjectContext ProgressWindow => _projectContext;

        public IActionEventSink ActionEventSink => _projectContext.ActionEventSink;

        public ICommonOperations CommonOperations => _projectContext.CommonOperations;

        public IEnumerable<NuGetProject> Projects { set; get; } = Enumerable.Empty<NuGetProject>();

        public bool DisplayPreviewWindow { set; get; } = true;

        public FileConflictAction FileConflictAction { set; get; } = FileConflictAction.PromptUser;

        public DependencyBehavior DependencyBehavior { set; get; } = DependencyBehavior.Lowest;

        public bool RemoveDependencies { set; get; } = false;

        public bool ForceRemove { set; get; } = false;

        public PackageIdentity SelectedPackage { get; set; }

        public void OnActionsExecuted(IEnumerable<ResolvedAction> actions)
        {
            _context.SolutionManager.OnActionsExecuted(actions);
        }

        public IEnumerable<SourceRepository> ActiveSources
        {
            get
            {
                IEnumerable<SourceRepository> sources = null;

                if (PackageManagerControl != null)
                {
                    UIDispatcher.Invoke(() => { sources = PackageManagerControl.ActiveSources; });
                }

                return sources;
            }
        }

        internal PackageManagerControl PackageManagerControl { get; set; }

        private DetailControl _detailControl;

        internal DetailControl DetailControl
        {
            set
            {
                _detailControl = value;
            }
        }

        private Dispatcher UIDispatcher
        {
            get
            {
                if (_detailControl != null)
                {
                    return _detailControl.Dispatcher;
                }

                if (Application.Current != null)
                {
                    return Application.Current.Dispatcher;
                }

                // null for unit tests
                return null;
            }
        }

        public void ShowError(Exception ex)
        {
            if (ex is NuGetResolverConstraintException ||
                ex is PackageAlreadyInstalledException ||
                ex is MinClientVersionException ||
                ex is FrameworkException ||
                ex is NuGetProtocolException ||
                ex is PackagingException ||
                ex is InvalidOperationException)
            {
                // for exceptions that are known to be normal error cases, just
                // display the message.
                ProgressWindow.Log(ProjectManagement.MessageLevel.Info, ex.Message);

                // write to activity log
                var message = string.Format(CultureInfo.CurrentCulture, ex.ToString());
                ActivityLog.LogError(LogEntrySource, message);
            }
            else
            {
                ProgressWindow.Log(ProjectManagement.MessageLevel.Error, ex.ToString());
            }

            ProgressWindow.ReportError(ex.Message);
        }
    }
}