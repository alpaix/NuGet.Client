// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Moq;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.PackageManagement.VisualStudio;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.VisualStudio;
using Test.Utility;
using Xunit;

namespace NuGetConsole.Host.PowerShell.Test
{
    public class PowerShellHostTests
    {
        [Fact]
        public async Task ExecuteInitScriptsAsync_Test()
        {
            using (var vsSolutionManager = new TestVsSolutionManager())
            {
                var settings = Mock.Of<ISettings>();
                var services = new TestHostServices();
                var host = new TestPowerShellHost(vsSolutionManager, settings, services);

                await host.ExecuteInitScriptsAsync();
            }
        }
    }

    internal class TestVsSolutionManager : TestSolutionManager, IVsSolutionManager
    {
        public TestVsSolutionManager()
            :base(true)
        {

        }

        public NuGetProject DefaultNuGetProject => throw new NotImplementedException();

        public string DefaultNuGetProjectName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<IVsProjectAdapter> GetAllVsProjectAdapters()
        {
            throw new NotImplementedException();
        }

        public Task<NuGetProject> GetOrCreateProjectAsync(Project project, INuGetProjectContext projectContext)
        {
            throw new NotImplementedException();
        }

        public IVsProjectAdapter GetVsProjectAdapter(string name)
        {
            throw new NotImplementedException();
        }

        public IVsProjectAdapter GetVsProjectAdapter(NuGetProject project)
        {
            throw new NotImplementedException();
        }

        public bool IsAllProjectsNominated()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSolutionFullyLoadedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<NuGetProject> UpgradeProjectToPackageReferenceAsync(NuGetProject project)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestHostServices
        : IRestoreEvents
        , IRunspaceManager
        , ISourceRepositoryProvider
        , IDeleteOnRestartManager
        , IScriptExecutor
        , ISourceControlManagerProvider
        , ICommonOperations
    {
        public IPackageSourceProvider PackageSourceProvider { get; } = Mock.Of<IPackageSourceProvider>();

        public event SolutionRestoreCompletedEventHandler SolutionRestoreCompleted;
        public event EventHandler<PackagesMarkedForDeletionEventArgs> PackagesMarkedForDeletionFound;

        public void CheckAndRaisePackageDirectoriesMarkedForDeletion()
        {
            throw new NotImplementedException();
        }

        public Task CollapseAllNodes(ISolutionManager solutionManager)
        {
            throw new NotImplementedException();
        }

        public SourceRepository CreateRepository(PackageSource source)
        {
            throw new NotImplementedException();
        }

        public SourceRepository CreateRepository(PackageSource source, FeedType type)
        {
            throw new NotImplementedException();
        }

        public void DeleteMarkedPackageDirectories(INuGetProjectContext projectContext)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExecuteAsync(PackageIdentity packageIdentity, string packageInstallPath, string scriptRelativePath, Project envDTEProject, INuGetProjectContext nuGetProjectContext, bool throwOnFailure)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExecuteInitScriptAsync(PackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> GetPackageDirectoriesMarkedForDeletion()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SourceRepository> GetRepositories()
        {
            return Enumerable.Empty<SourceRepository>();
        }

        public Tuple<RunspaceDispatcher, NuGetPSHost> GetRunspace(IConsole console, string hostName)
        {
            throw new NotImplementedException();
        }

        public SourceControlManager GetSourceControlManager()
        {
            throw new NotImplementedException();
        }

        public void MarkPackageDirectoryForDeletion(PackageIdentity package, string packageDirectory, INuGetProjectContext projectContext)
        {
            throw new NotImplementedException();
        }

        public Task OpenFile(string fullPath)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Task SaveSolutionExplorerNodeStates(ISolutionManager solutionManager)
        {
            throw new NotImplementedException();
        }

        public bool TryMarkVisited(PackageIdentity packageIdentity, PackageInitPS1State initPS1State)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestPowerShellHost
        : PowerShellHost
    {
        public override bool IsAsync => true;

        public override event EventHandler ExecuteEnd;

        public TestPowerShellHost(
            IVsSolutionManager vsSolutionManager,
            ISettings settings,
            TestHostServices services)
            :base(
                 restoreEvents: services,
                 runspaceManager: services,
                 sourceRepositoryProvider: services,
                 vsSolutionManager: vsSolutionManager,
                 settings: settings,
                 deleteOnRestartManager: services,
                 scriptExecutor: services)
        {

        }

        protected override bool ExecuteHost(string fullCommand, string command, params object[] inputs)
        {
            throw new NotImplementedException();
        }
    }
}
