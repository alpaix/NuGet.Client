// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Windows.Media;
using NuGet.PackageManagement;
using NuGet.VisualStudio;

namespace NuGetConsole.Host
{
    /// <summary>
    /// This host is used when PowerShell 2.0 runtime is not installed in the system. It's basically a no-op host.
    /// </summary>
    internal class UnsupportedHost : IHost
    {
        public bool IsCommandEnabled => false;

        public event EventHandler ExecuteEnd;

        public Task InitializeAsync(IConsole console)
        {
            // display the error message at the beginning
            console.Write(PowerShell.Resources.Host_PSNotInstalled, Colors.Red, null);
            return Task.CompletedTask;
        }

        public string Prompt => string.Empty;

        public bool Execute(IConsole console, string command, object[] inputs)
        {
            ExecuteEnd.Raise(this, EventArgs.Empty);
            return false;
        }

        public Task ExecuteInitScriptsAsync()
        {
            return Task.CompletedTask;
        }

        public void Abort()
        {
        }

        public string ActivePackageSource
        {
            get => string.Empty;
            set { }
        }

        public string[] GetPackageSources()
        {
            return new string[0];
        }

        public string DefaultProject => string.Empty;

        public void SetDefaultProjectIndex(int index)
        {
        }

        public string[] GetAvailableProjects()
        {
            return new string[0];
        }

        public void SetDefaultRunspace()
        {
        }

        public bool IsAsync => false;
    }
}
