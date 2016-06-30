// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.PackageManagement.UI;

namespace StandaloneUI
{
    internal class StandaloneActionEventSink : IActionEventSink
    {
        public void OnActionCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnActionStarted()
        {
            throw new NotImplementedException();
        }

        public void OnError(string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void OnWarning(string warningMessage)
        {
            throw new NotImplementedException();
        }
    }
}