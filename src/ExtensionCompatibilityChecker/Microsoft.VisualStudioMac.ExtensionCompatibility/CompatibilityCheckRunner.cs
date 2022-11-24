//
// CompatibilityCheckRunner.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2022 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Microsoft.VisualStudio.Threading;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.PooledObjects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace Microsoft.VisualStudioMac.ExtensionCompatibility;

class CompatibilityCheckRunner
{
    public async Task RunCheckAsync()
    {
        await TaskScheduler.Default;

        List<Addin> userAddins = GetUserAddins();

        if (!userAddins.Any())
        {
            return;
        }

        using OutputProgressMonitor progressMonitor = CreateProgressMonitor();
        using OperationConsole console = progressMonitor.Console;

        var wrappedConsole = new CompatibilityCheckOperationConsole(console);

        var incompatibleAddins = new List<Addin>();
        bool reportFailure = false;

        foreach (Addin addin in userAddins)
        {
            try
            {
                int exitCode = await RunCheckAsync(addin, progressMonitor, wrappedConsole);
                if (exitCode == 1)
                {
                    incompatibleAddins.Add(addin);
                }
                else if (exitCode == 0)
                {
                    // Success
                }
                else
                {
                    // error
                    reportFailure = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unable to run addin compat check", ex);
            }
        }

        string? errorMessage = null;
        if (incompatibleAddins.Count > 0)
        {
            errorMessage = GetIncompatibleAddinInfo(incompatibleAddins);
        }
        else if (reportFailure)
        {
            errorMessage = GettextCatalog.GetString("Failed to run compatibility checks");
        }

        if (errorMessage is not null)
        {
            progressMonitor.Log.WriteLine(errorMessage);

            await Runtime.MainTaskScheduler;

            Pad pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor(progressMonitor);
            if (pad is not null)
            {
                pad.BringToFront();
            }
        }
    }

    List<Addin> GetUserAddins()
    {
        List<Addin> userAddins = AddinManager.Registry.GetModules(AddinSearchFlags.IncludeAddins | AddinSearchFlags.LatestVersionsOnly)
            .Where(addin => addin.IsUserAddin)
            .ToList();

        userAddins.Sort(CompareAddin);

        return userAddins;
    }

    static int CompareAddin(Addin x, Addin y)
    {
        return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
    }

    static OutputProgressMonitor CreateProgressMonitor()
    {
        return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor(
            "CompatibilityCheckRunnerConsole",
            GettextCatalog.GetString("Extension Compatibility Console"),
            Stock.Console,
            false,
            true);
    }

    async Task<int> RunCheckAsync(
        Addin addin,
        OutputProgressMonitor progressMonitor,
        OperationConsole console)
    {
        string? directory = Path.GetDirectoryName(addin.AddinFile);

        string message = GettextCatalog.GetString("========== Checking extension {0} {1} ==========", addin.Name, addin.Version);
        progressMonitor.Log.WriteLine(message);

        var commandLine = new VSMacAddinCompactCheckerCommandLine();
        commandLine.AddinDirectory = directory;

        string arguments = commandLine.BuildCommandLine();

        ProcessAsyncOperation operation = Runtime.ProcessService.StartConsoleProcess(
            VSMacAddinCompactCheckerCommandLine.DotNetFileName,
            arguments,
            null,
            console);

        await operation.Task;

        return operation.ExitCode;
    }

    string GetIncompatibleAddinInfo(List<Addin> incompatibleAddins)
    {
        using var builder = PooledStringBuilder.GetInstance();

        string message = GettextCatalog.GetPluralString(
            "{0} extension is not compatible:",
            "{0} extensions are not compatible:",
            incompatibleAddins.Count,
            incompatibleAddins.Count);

        builder.AppendLine(message);

        foreach (Addin addin in incompatibleAddins)
        {
            builder.AppendLine($"    {addin.Name} {addin.Version}");
        }

        return builder.ToStringAndFree();
    }
}

