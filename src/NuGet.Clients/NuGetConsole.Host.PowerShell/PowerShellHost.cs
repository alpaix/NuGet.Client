﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.PackageManagement.VisualStudio;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.VisualStudio;
using Task = System.Threading.Tasks.Task;

namespace NuGetConsole.Host.PowerShell
{
    internal abstract class PowerShellHost : IHost, IPathExpansion, IDisposable
    {
        /// <summary>
        /// This PowerShell host name. Used for PowerShell "$host".
        /// </summary>
        private const string PowerConsoleHostName = "Package Manager Host";

        private const string ActivePackageSourceKey = "activePackageSource";
        private const string SyncModeKey = "IsSyncMode";
        private const string DTEKey = "DTE";
        private const string CancellationTokenKey = "CancellationTokenKey";

        private static readonly string AggregateSourceName = Resources.AggregateSourceName;

        private readonly AsyncSemaphore _initScriptsLock = new AsyncSemaphore(1);
        private readonly IRestoreEvents _restoreEvents;
        private readonly IRunspaceManager _runspaceManager;
        private readonly ISourceRepositoryProvider _sourceRepositoryProvider;
        private readonly IVsSolutionManager _solutionManager;
        private readonly ISettings _settings;
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly AsyncLazy<EnvDTE.DTE> _dte;

        private string _activePackageSource;
        private string[] _packageSources;

        private IConsole _activeConsole;
        private NuGetPSHost _nugetHost;
        // indicates whether this host has been initialized.
        // null = not initilized, true = initialized successfully, false = initialized unsuccessfully
        private bool? _initialized;
        // store the current (non-truncated) project names displayed in the project name combobox
        private string[] _projectSafeNames;

        // store the current command typed so far
        private ComplexCommand _complexCommand;

        // store the current CancellationTokenSource which will be used to cancel the operation
        // in case of abort
        private CancellationTokenSource _tokenSource;

        // store the current CancellationToken. This will be set on the private data
        private CancellationToken _token;

        // store the current solution directory which will be to check the solution change while executing init scripts.
        private string _currentSolutionDirectory;

        /// <summary>
        /// An initial restore event used to compare against future (real) restore events. This value endures on
        /// <see cref="_latestRestore"/> and <see cref="_currentRestore"/> as long as no restore occurs. Note that the
        /// hash mentioned here cannot collide with a real hash.
        /// </summary>
        private static readonly SolutionRestoredEventArgs InitialRestore = new SolutionRestoredEventArgs(
            isSuccess: true,
            solutionSpecHash: "initial");

        /// <summary>
        /// This field tracks information about the latest restore.
        /// </summary>
        private SolutionRestoredEventArgs _latestRestore = InitialRestore;

        /// <summary>
        /// This field tracks information about the most recent restore that had scripts executed for it.
        /// </summary>
        private SolutionRestoredEventArgs _currentRestore = InitialRestore;

        public abstract event EventHandler ExecuteEnd;

        protected PowerShellHost(IRestoreEvents restoreEvents, IRunspaceManager runspaceManager)
        {
            _restoreEvents = restoreEvents;
            _runspaceManager = runspaceManager;

            // TODO: Take these as ctor arguments
            _sourceRepositoryProvider = ServiceLocator.GetInstance<ISourceRepositoryProvider>();
            _solutionManager = ServiceLocator.GetInstance<IVsSolutionManager>();
            _settings = ServiceLocator.GetInstance<ISettings>();
            _deleteOnRestartManager = ServiceLocator.GetInstance<IDeleteOnRestartManager>();
            _scriptExecutor = ServiceLocator.GetInstance<IScriptExecutor>();

            InitializeSources();

            _sourceRepositoryProvider.PackageSourceProvider.PackageSourcesChanged += PackageSourceProvider_PackageSourcesChanged;
            _restoreEvents.SolutionRestoreCompleted += RestoreEvents_SolutionRestoreCompleted;
        }

        [ImportingConstructor]
        protected PowerShellHost(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IRestoreEvents restoreEvents,
            IRunspaceManager runspaceManager,
            ISourceRepositoryProvider sourceRepositoryProvider,
            IVsSolutionManager vsSolutionManager,
            ISettings settings,
            IDeleteOnRestartManager deleteOnRestartManager,
            IScriptExecutor scriptExecutor)
        {
            _restoreEvents = restoreEvents;
            _runspaceManager = runspaceManager;

            _sourceRepositoryProvider = sourceRepositoryProvider;
            _solutionManager = vsSolutionManager;
            _settings = settings;
            _deleteOnRestartManager = deleteOnRestartManager;
            _scriptExecutor = scriptExecutor;

            _dte = new AsyncLazy<EnvDTE.DTE>(
                async () =>
                {
                    await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return serviceProvider.GetDTE();
                },
                NuGetUIThreadHelper.JoinableTaskFactory);

            InitializeSources();

            _sourceRepositoryProvider.PackageSourceProvider.PackageSourcesChanged += PackageSourceProvider_PackageSourcesChanged;
            _restoreEvents.SolutionRestoreCompleted += RestoreEvents_SolutionRestoreCompleted;
        }

        private void InitializeSources()
        {
            _packageSources = GetEnabledPackageSources(_sourceRepositoryProvider);
            UpdateActiveSource(_sourceRepositoryProvider.PackageSourceProvider.ActivePackageSourceName);
        }

        private static string[] GetEnabledPackageSources(ISourceRepositoryProvider sourceRepositoryProvider)
        {
            var enabledSources = sourceRepositoryProvider
                           .GetRepositories()
                           .Where(r => r.PackageSource.IsEnabled)
                           .ToArray();

            var packageSources = new List<string>();

            if (enabledSources.Length > 1)
            {
                packageSources.Add(AggregateSourceName);
            }

            packageSources.AddRange(
                enabledSources.Select(r => r.PackageSource.Name));
            return packageSources.ToArray();
        }

        #region Properties

        protected Pipeline ExecutingPipeline { get; set; }

        /// <summary>
        /// The host is associated with a particular console on a per-command basis.
        /// This gets set every time a command is executed on this host.
        /// </summary>
        protected IConsole ActiveConsole
        {
            get { return _activeConsole; }
            set
            {
                _activeConsole = value;
                if (_nugetHost != null)
                {
                    _nugetHost.ActiveConsole = value;
                }
            }
        }

        public bool IsCommandEnabled { get; private set; } = true;

        protected RunspaceDispatcher Runspace { get; private set; }

        private ComplexCommand ComplexCommand
        {
            get
            {
                if (_complexCommand == null)
                {
                    _complexCommand = new ComplexCommand((allLines, lastLine) =>
                        {
                            Collection<PSParseError> errors;
                            PSParser.Tokenize(allLines, out errors);

                            // If there is a parse error token whose END is past input END, consider
                            // it a multi-line command.
                            if (errors.Count > 0)
                            {
                                if (errors.Any(e => (e.Token.Start + e.Token.Length) >= allLines.Length))
                                {
                                    return false;
                                }
                            }

                            return true;
                        });
                }
                return _complexCommand;
            }
        }

        public string Prompt
        {
            get { return ComplexCommand.IsComplete ? EvaluatePrompt() : ">> "; }
        }

        public string ActivePackageSource
        {
            get { return _activePackageSource; }
            set { UpdateActiveSource(value); }
        }

        public string DefaultProject
        {
            get
            {
                Assumes.Present(_solutionManager);

                if (_solutionManager.DefaultNuGetProject == null)
                {
                    return null;
                }

                return GetDisplayName(_solutionManager.DefaultNuGetProject);
            }
        }

        public abstract bool IsAsync { get; }

        #endregion

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private string EvaluatePrompt()
        {
            var prompt = "PM>";

            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                try
                {
                    // Execute the prompt function from a worker thread, so that the UI thread is not blocked waiting
                    // on it. Note that a default prompt function as defined in Profile.ps1 will simply return
                    // a string "PM>". This will always work. However, a custom "prompt" function might call
                    // Write-Host and NuGet will explicity switch to the main thread using JTF.
                    // If the main thread was blocked then, it will consistently result in a hang.
                    var output = await Task.Run(() =>
                                        Runspace.Invoke("prompt", null, outputResults: false).FirstOrDefault());
                    if (output != null)
                    {
                        var result = output.BaseObject.ToString();
                        if (!string.IsNullOrEmpty(result))
                        {
                            prompt = result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHelper.WriteErrorToActivityLog(ex);
                }
                return prompt;
            });
        }

        /// <summary>
        /// Doing all necessary initialization works before the console accepts user inputs
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public async Task InitializeAsync(IConsole console)
        {
            ActiveConsole = console;

            if (_initialized.HasValue)
            {
                if (_initialized.Value
                    && console.ShowDisclaimerHeader)
                {
                    DisplayDisclaimerAndHelpText();
                }
            }
            else
            {
                try
                {
                    var result = _runspaceManager.GetRunspace(console, PowerConsoleHostName);
                    Runspace = result.Item1;
                    _nugetHost = result.Item2;

                    ExecuteEnd += OnExecuteCommandEnd;

                    _initialized = true;

                    if (console.ShowDisclaimerHeader)
                    {
                        DisplayDisclaimerAndHelpText();
                    }

                    await UpdateWorkingDirectoryAsync();
                    await ExecuteInitScriptsAsync();

                    // check if PMC console is actually opened, then only hook to solution load/close events.
                    if (console is IWpfConsole)
                    {
                        // Hook up solution events
                        _solutionManager.SolutionOpened += (o, e) =>
                            {
                                _scriptExecutor.Reset();

                                // Solution opened event is raised on the UI thread
                                // Go off the UI thread before calling likely expensive call of ExecuteInitScriptsAsync
                                // Also, it uses semaphores, do not call it from the UI thread
                                Task.Run(async () =>
                                {
                                    await UpdateWorkingDirectoryAsync();
                                    await ExecuteInitScriptsAsync();
                                });
                            };
                        _solutionManager.SolutionClosed += (o, e) => NuGetUIThreadHelper.JoinableTaskFactory.Run(() => UpdateWorkingDirectoryAsync());
                    }
                    _solutionManager.NuGetProjectAdded += (o, e) => UpdateWorkingDirectoryAndAvailableProjects();
                    _solutionManager.NuGetProjectRenamed += (o, e) => UpdateWorkingDirectoryAndAvailableProjects();
                    _solutionManager.NuGetProjectUpdated += (o, e) => UpdateWorkingDirectoryAndAvailableProjects();
                    _solutionManager.NuGetProjectRemoved += (o, e) =>
                        {
                            UpdateWorkingDirectoryAndAvailableProjects();
                            // When the previous default project has been removed, _solutionManager.DefaultNuGetProjectName becomes null
                            if (_solutionManager.DefaultNuGetProjectName == null)
                            {
                                // Change default project to the first one in the collection
                                SetDefaultProjectIndex(0);
                            }
                        };
                    // Set available private data on Host
                    SetPrivateDataOnHost(isSync: false);
                }
                catch (Exception ex)
                {
                    // catch all exception as we don't want it to crash VS
                    _initialized = false;
                    IsCommandEnabled = false;
                    ReportError(ex);

                    ExceptionHelper.WriteErrorToActivityLog(ex);
                }
            }
        }

        private void UpdateWorkingDirectoryAndAvailableProjects()
        {
            NuGetUIThreadHelper.JoinableTaskFactory.Run(() => UpdateWorkingDirectoryAsync());
            GetAvailableProjects();
        }

        private async Task UpdateWorkingDirectoryAsync()
        {
            await TaskScheduler.Default;

            if (Runspace.RunspaceAvailability == RunspaceAvailability.Available)
            {
                // if there is no solution open, we set the active directory to be user profile folder
                var targetDir = _solutionManager.IsSolutionOpen ?
                    _solutionManager.SolutionDirectory :
                    Environment.GetEnvironmentVariable("USERPROFILE");

                Runspace.ChangePSDirectory(targetDir);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want execution of init scripts to crash our console.")]
        public async Task ExecuteInitScriptsAsync()
        {
            // Fix for Bug 1426 Disallow ExecuteInitScripts from being executed concurrently by multiple threads.
            using (await _initScriptsLock.EnterAsync())
            {
                if (!_solutionManager.IsSolutionOpen)
                {
                    return;
                }

                Debug.Assert(_settings != null);
                if (_settings == null)
                {
                    return;
                }

                var latestRestore = _latestRestore;
                var latestSolutionDirectory = _solutionManager.SolutionDirectory;
                if (ShouldNoOpDueToRestore(latestRestore) &&
                    ShouldNoOpDueToSolutionDirectory(latestSolutionDirectory))
                {
                    _currentRestore = latestRestore;
                    _currentSolutionDirectory = latestSolutionDirectory;

                    return;
                }

                // invoke init.ps1 files in the order of package dependency.
                // if A -> B, we invoke B's init.ps1 before A's.

                var projects = _solutionManager.GetNuGetProjects().ToList();

                var packagesByFramework = new Dictionary<NuGetFramework, HashSet<PackageIdentity>>();
                var sortedGlobalPackages = new List<PackageIdentity>();

                // Sort projects by type
                foreach (var project in projects)
                {
                    // Skip project K projects.
                    if (project is ProjectKNuGetProjectBase)
                    {
                        continue;
                    }

                    var buildIntegratedProject = project as BuildIntegratedNuGetProject;

                    if (buildIntegratedProject != null)
                    {
                        var packages = await BuildIntegratedProjectUtility
                            .GetOrderedProjectPackageDependencies(buildIntegratedProject);

                        sortedGlobalPackages.AddRange(packages);
                    }
                    else
                    {
                        // Read packages.config
                        var installedRefs = await project.GetInstalledPackagesAsync(CancellationToken.None);

                        if (installedRefs?.Any() == true)
                        {
                            // Index packages.config references by target framework since this affects dependencies
                            NuGetFramework targetFramework;
                            if (!project.TryGetMetadata(NuGetProjectMetadataKeys.TargetFramework, out targetFramework))
                            {
                                targetFramework = NuGetFramework.AnyFramework;
                            }

                            HashSet<PackageIdentity> fwPackages;
                            if (!packagesByFramework.TryGetValue(targetFramework, out fwPackages))
                            {
                                fwPackages = new HashSet<PackageIdentity>();
                                packagesByFramework.Add(targetFramework, fwPackages);
                            }

                            fwPackages.UnionWith(installedRefs.Select(reference => reference.PackageIdentity));
                        }
                    }
                }

                // Each id/version should only be executed once
                var finishedPackages = new HashSet<PackageIdentity>();

                // Packages.config projects
                if (packagesByFramework.Count > 0)
                {
                    await ExecuteInitPs1ForPackagesConfigAsync(
                        packagesByFramework,
                        finishedPackages);
                }

                // build integrated projects
                if (sortedGlobalPackages.Count > 0)
                {
                    await ExecuteInitPs1ForBuildIntegratedAsync(
                        sortedGlobalPackages,
                        finishedPackages);
                }

                // We are done executing scripts, so record the restore and solution directory that we executed for.
                // This aids the no-op logic above.
                _currentRestore = latestRestore;
                _currentSolutionDirectory = latestSolutionDirectory;
            }
        }

        private async Task ExecuteInitPs1ForPackagesConfigAsync(
            Dictionary<NuGetFramework, HashSet<PackageIdentity>> packagesConfigInstalled,
            HashSet<PackageIdentity> finishedPackages)
        {
            var packageManager = new NuGetPackageManager(
                _sourceRepositoryProvider,
                _settings,
                _solutionManager,
                _deleteOnRestartManager);

            // Get the path to the Packages folder.
            var packagesFolderPath = packageManager.PackagesFolderSourceRepository.PackageSource.Source;
            var packagePathResolver = new PackagePathResolver(packagesFolderPath);

            var packagesToSort = new HashSet<ResolverPackage>();
            var resolvedPackages = new HashSet<PackageIdentity>();

            var dependencyInfoResource = await packageManager
                .PackagesFolderSourceRepository
                .GetResourceAsync<DependencyInfoResource>();

            // Order by the highest framework first to make this deterministic
            // Process each framework/id/version once to avoid duplicate work
            // Packages may have different dependendcy orders depending on the framework, but there is
            // no way to fully solve this across an entire solution so we make a best effort here.
            foreach (var framework in packagesConfigInstalled.Keys.OrderByDescending(fw => fw, new NuGetFrameworkSorter()))
            {
                foreach (var package in packagesConfigInstalled[framework])
                {
                    if (resolvedPackages.Add(package))
                    {
                        var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                            package,
                            framework,
                            NuGet.Common.NullLogger.Instance,
                            CancellationToken.None);

                        // This will be null for unrestored packages
                        if (dependencyInfo != null)
                        {
                            packagesToSort.Add(new ResolverPackage(dependencyInfo, listed: true, absent: false));
                        }
                    }
                }
            }

            // Order packages by dependency order
            var sortedPackages = ResolverUtility.TopologicalSort(packagesToSort);
            foreach (var package in sortedPackages)
            {
                if (finishedPackages.Add(package))
                {
                    // Find the package path in the packages folder.
                    var installPath = packagePathResolver.GetInstalledPath(package);

                    if (string.IsNullOrEmpty(installPath))
                    {
                        continue;
                    }

                    await ExecuteInitPs1Async(installPath, package);
                }
            }
        }

        private async Task ExecuteInitPs1ForBuildIntegratedAsync(
            List<PackageIdentity> sortedGlobalPackages,
            HashSet<PackageIdentity> finishedPackages)
        {
            var nugetPaths = NuGetPathContext.Create(_settings);
            var fallbackResolver = new FallbackPackagePathResolver(nugetPaths);

            foreach (var package in sortedGlobalPackages)
            {
                if (finishedPackages.Add(package))
                {
                    // Find the package in the global packages folder or any of the fallback folders.
                    var installPath = fallbackResolver.GetPackageDirectory(package.Id, package.Version);
                    if (installPath == null)
                    {
                        continue;
                    }

                    await ExecuteInitPs1Async(installPath, package);
                }
            }
        }

        private async Task ExecuteInitPs1Async(string installPath, PackageIdentity identity)
        {
            try
            {
                var toolsPath = Path.Combine(installPath, "tools");
                if (Directory.Exists(toolsPath))
                {
                    AddPathToEnvironment(toolsPath);

                    var scriptPath = Path.Combine(toolsPath, PowerShellScripts.Init);
                    if (File.Exists(scriptPath) &&
                        _scriptExecutor.TryMarkVisited(identity, PackageInitPS1State.FoundAndExecuted))
                    {
                        // always execute init script from a background thread
                        await TaskScheduler.Default;

                        var request = new ScriptExecutionRequest(scriptPath, installPath, identity, project: null);

                        Runspace.Invoke(
                            request.BuildCommand(),
                            request.BuildInput(),
                            outputResults: true);

                        return;
                    }
                }

                _scriptExecutor.TryMarkVisited(identity, PackageInitPS1State.NotFound);
            }
            catch (Exception ex)
            {
                // If execution of an init.ps1 scripts fails, do not let it crash our console.
                ReportError(ex);

                ExceptionHelper.WriteErrorToActivityLog(ex);
            }
        }

        private static void AddPathToEnvironment(string path)
        {
            var currentPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Process);

            var currentPaths = new HashSet<string>(
                currentPath.Split(Path.PathSeparator).Select(p => p.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (currentPaths.Add(path))
            {
                var newPath = currentPath + Path.PathSeparator + path;
                Environment.SetEnvironmentVariable("path", newPath, EnvironmentVariableTarget.Process);
            }
        }

        protected abstract bool ExecuteHost(string fullCommand, string command, params object[] inputs);

        public bool Execute(IConsole console, string command, params object[] inputs)
        {
            Assumes.Present(console);
            Assumes.Present(command);

            // since install.ps1/uninstall.ps1 could depend on init scripts, so we need to make sure
            // to run it once for each solution
            NuGetUIThreadHelper.JoinableTaskFactory.Run(() => ExecuteInitScriptsAsync());

            NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.PackageManagerConsoleCommandExecutionBegin);
            ActiveConsole = console;

            string fullCommand;
            if (ComplexCommand.AddLine(command, out fullCommand)
                && !string.IsNullOrEmpty(fullCommand))
            {
                // create a new token source with each command since CTS aren't usable once cancelled.
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
                return ExecuteHost(fullCommand, command, inputs);
            }

            return false; // constructing multi-line command
        }

        private void OnExecuteCommandEnd(object sender, EventArgs e)
        {
            NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.PackageManagerConsoleCommandExecutionEnd);

            // dispose token source related to this current command
            _tokenSource?.Dispose();
            _token = CancellationToken.None;
        }

        public void Abort()
        {
            ExecutingPipeline?.StopAsync();
            ComplexCommand.Clear();
            try
            {
                _tokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ObjectDisposedException is expected here, since at clear console command, tokenSource
                // would have already been disposed.
            }
        }

        protected void SetPrivateDataOnHost(bool isSync)
        {
            SetPropertyValueOnHost(SyncModeKey, isSync);
            // "All" aggregate source in a context of PS command means no particular source is preferred,
            // in that case all enabled sources will be picked for a command execution.
            SetPropertyValueOnHost(ActivePackageSourceKey, ActivePackageSource != AggregateSourceName ? ActivePackageSource : string.Empty);
            var dte = NuGetUIThreadHelper.JoinableTaskFactory.Run(_dte.GetValueAsync);
            SetPropertyValueOnHost(DTEKey, dte);
            SetPropertyValueOnHost(CancellationTokenKey, _token);
        }

        private void SetPropertyValueOnHost(string propertyName, object value)
        {
            if (_nugetHost != null)
            {
                var property = _nugetHost.PrivateData.Properties[propertyName];
                if (property == null)
                {
                    property = new PSNoteProperty(propertyName, value);
                    _nugetHost.PrivateData.Properties.Add(property);
                }
                else
                {
                    property.Value = value;
                }
            }
        }

        public void SetDefaultRunspace()
        {
            Runspace.MakeDefault();
        }

        private void DisplayDisclaimerAndHelpText()
        {
            WriteLine(Resources.Console_DisclaimerText);
            WriteLine();

            WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.PowerShellHostTitle, _nugetHost.Version));
            WriteLine();

            WriteLine(Resources.Console_HelpText);
            WriteLine();
        }

        protected void ReportError(ErrorRecord record)
        {
            WriteErrorLine(Runspace.ExtractErrorFromErrorRecord(record));
        }

        protected void ReportError(Exception exception)
        {
            exception = NuGet.Common.ExceptionUtilities.Unwrap(exception);
            WriteErrorLine(exception.Message);
        }

        private void WriteErrorLine(string message)
        {
            ActiveConsole?.Write(message + Environment.NewLine, Colors.DarkRed, null);
        }

        private void WriteLine(string message = "")
        {
            ActiveConsole?.WriteLine(message);
        }

        public string[] GetPackageSources() => _packageSources;

        private void PackageSourceProvider_PackageSourcesChanged(object sender, EventArgs e)
        {
            _packageSources = GetEnabledPackageSources(_sourceRepositoryProvider);
            UpdateActiveSource(ActivePackageSource);
        }

        private void RestoreEvents_SolutionRestoreCompleted(SolutionRestoredEventArgs args)
        {
            _latestRestore = args;
        }

        private bool ShouldNoOpDueToRestore(SolutionRestoredEventArgs latestRestore)
        {
            return latestRestore != null &&
                   _currentRestore != null &&
                   latestRestore.SolutionSpecHash == _currentRestore.SolutionSpecHash &&
                   latestRestore.IsSuccess == _currentRestore.IsSuccess;
        }

        private bool ShouldNoOpDueToSolutionDirectory(string latestSolutionDirectory)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(
                _currentSolutionDirectory,
                latestSolutionDirectory);
        }

        private void UpdateActiveSource(string activePackageSource)
        {
            if (_packageSources.Length == 0)
            {
                _activePackageSource = string.Empty;
            }
            else if (activePackageSource == null)
            {
                // use the first enabled source as the active source
                _activePackageSource = _packageSources.First();
            }
            else
            {
                var s = _packageSources.FirstOrDefault(
                    p => StringComparer.CurrentCultureIgnoreCase.Equals(p, activePackageSource));

                // if the old active source still exists. Keep it as the active source.
                // if the old active source does not exist any more. In this case,
                // use the first eneabled source as the active source.
                _activePackageSource = s ?? _packageSources.First();
            }
        }

        public void SetDefaultProjectIndex(int selectedIndex)
        {
            Debug.Assert(_solutionManager != null);

            if (_projectSafeNames != null
                && selectedIndex >= 0
                && selectedIndex < _projectSafeNames.Length)
            {
                _solutionManager.DefaultNuGetProjectName = _projectSafeNames[selectedIndex];
            }
            else
            {
                _solutionManager.DefaultNuGetProjectName = null;
            }
        }

        public string[] GetAvailableProjects()
        {
            Debug.Assert(_solutionManager != null);

            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var allProjects = _solutionManager.GetNuGetProjects();
                    _projectSafeNames = allProjects.Select(_solutionManager.GetNuGetProjectSafeName).ToArray();
                    var displayNames = allProjects.Select(GetDisplayName).ToArray();
                    Array.Sort(displayNames, _projectSafeNames, StringComparer.CurrentCultureIgnoreCase);
                    return _projectSafeNames;
                });
        }

        private string GetDisplayName(NuGetProject nuGetProject)
        {
            var vsProjectAdapter = _solutionManager.GetVsProjectAdapter(nuGetProject);

            var name = vsProjectAdapter.CustomUniqueName;
            if (IsWebSite(vsProjectAdapter))
            {
                name = PathHelper.SmartTruncate(name, 40);
            }
            return name;
        }

        private static bool IsWebSite(IVsProjectAdapter project)
        {
            return project.ProjectTypeGuids.Contains(VsProjectTypes.WebSiteProjectTypeGuid);
        }

        #region ITabExpansion

        public async Task<string[]> GetExpansionsAsync(string line, string lastWord, CancellationToken token)
        {
            var isSync = !IsAsync;

            // Set the _token object to the CancellationToken passed in, so that the Private Data can be set with this token
            // Powershell cmdlets will pick up the CancellationToken from the private data of the Host, and use it in their calls to NuGetPackageManager
            _token = token;
            string[] expansions;
            try
            {
                SetPrivateDataOnHost(isSync);
                expansions = await Task.Run(() =>
                    {
                        var query = from s in Runspace.Invoke(
                            @"$__pc_args=@();$input|%{$__pc_args+=$_};if(Test-Path Function:\TabExpansion2){(TabExpansion2 $__pc_args[0] $__pc_args[0].length).CompletionMatches|%{$_.CompletionText}}else{TabExpansion $__pc_args[0] $__pc_args[1]};Remove-Variable __pc_args -Scope 0;",
                            new[] { line, lastWord },
                            outputResults: false)
                                    select (s == null ? null : s.ToString());
                        return query.ToArray();
                    }, _token);
            }
            finally
            {
                // Set the _token object to the CancellationToken passed in, so that the Private Data can be set correctly
                _token = CancellationToken.None;
            }

            return expansions;
        }

        #endregion

        #region IPathExpansion

        public async Task<SimpleExpansion> GetPathExpansionsAsync(string line, CancellationToken token)
        {
            // Set the _token object to the CancellationToken passed in, so that the Private Data can be set with this token
            // Powershell cmdlets will pick up the CancellationToken from the private data of the Host, and use it in their calls to NuGetPackageManager
            _token = token;
            SetPropertyValueOnHost(CancellationTokenKey, _token);
            var simpleExpansion = await Task.Run(() =>
                {
                    var expansion = Runspace.Invoke(
                        "$input|%{$__pc_args=$_}; _TabExpansionPath $__pc_args; Remove-Variable __pc_args -Scope 0",
                        new object[] { line },
                        outputResults: false).FirstOrDefault();
                    if (expansion != null)
                    {
                        var replaceStart = (int)expansion.Properties["ReplaceStart"].Value;
                        IList<string> paths = ((IEnumerable<object>)expansion.Properties["Paths"].Value).Select(o => o.ToString()).ToList();
                        return new SimpleExpansion(replaceStart, line.Length - replaceStart, paths);
                    }

                    return null;
                }, token);

            _token = CancellationToken.None;
            SetPropertyValueOnHost(CancellationTokenKey, _token);
            return simpleExpansion;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            ExecuteEnd -= OnExecuteCommandEnd;
            _restoreEvents.SolutionRestoreCompleted -= RestoreEvents_SolutionRestoreCompleted;
            _initScriptsLock.Dispose();
            Runspace?.Dispose();
        }

        #endregion
    }
}
