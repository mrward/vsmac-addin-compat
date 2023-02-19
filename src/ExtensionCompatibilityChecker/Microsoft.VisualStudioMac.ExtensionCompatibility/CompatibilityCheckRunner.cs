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
        await Runtime.MainTaskScheduler;

        // Ensure pad is created and available.
        ExtensionCompatibilityConsolePad.Initialize();

        await TaskScheduler.Default;

        List<Addin> userAddins = GetUserAddins();

        if (!userAddins.Any())
        {
            return;
        }

        OutputProgressMonitor progressMonitor = GetProgressMonitor();

        progressMonitor.Log.WriteLine(GettextCatalog.GetString("Checking extension compatibility…"));

        var wrappedConsole = new CompatibilityCheckOperationConsole(progressMonitor.Console);

        var incompatibleAddins = new List<Addin>();
        bool reportFailure = false;

        FilePath baseLineFileName = await GenerateVSMacBaseLine(progressMonitor, wrappedConsole);

        if (baseLineFileName.IsNull)
        {
            return;
        }

        FilePath diffOutputDirectory = FileService.CreateTempDirectory();

        foreach (Addin addin in userAddins)
        {
            try
            {
                FilePath diffOutputFileName = diffOutputDirectory.Combine($"{addin.LocalId}-diff.txt");
                FilePath diffIgnoreFileName = CompatibilityBaselineDiffProvider.GetExistingDiffFileName(addin);

                int exitCode = await RunCheckAsync(
                    addin,
                    baseLineFileName,
                    diffIgnoreFileName,
                    diffOutputFileName,
                    progressMonitor,
                    wrappedConsole);

                if (exitCode == 1)
                {
                    incompatibleAddins.Add(addin);
                    CompatibilityBaselineDiffProvider.AddTemporaryDiffFile(addin, diffOutputFileName);
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

        SafeRemoveVSMacBaseLineReport(baseLineFileName);

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
            ExtensionCompatibilityConsolePad.IsResetBaselineButtonEnabled = true;

            Pad pad = IdeApp.Workbench.GetPad<ExtensionCompatibilityConsolePad>();
            if (pad is not null)
            {
                pad.BringToFront();
                ExtensionCompatibilityConsolePad.IsSaveBaselineButtonEnabled = true;
            }
        }
        else
        {
            await Runtime.MainTaskScheduler;
            ExtensionCompatibilityConsolePad.IsResetBaselineButtonEnabled = true;

            progressMonitor.Log.WriteLine(GettextCatalog.GetString("All extensions are compatible"));
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

    static OutputProgressMonitor GetProgressMonitor()
    {
        return ExtensionCompatibilityConsolePad.LogView!.GetProgressMonitor();
    }

    async Task<FilePath> GenerateVSMacBaseLine(
        OutputProgressMonitor progressMonitor,
        OperationConsole console)
    {
        var commandLine = new VSMacAddinCompactCheckerCommandLine();
        commandLine.GenerateVSMacBaseLineFile = true;
        commandLine.VSMacBaseLineFile = Path.Combine(FileService.CreateTempDirectory(), "vsmac-baseline.txt");

        int exitCode = await RunCheckAsync(commandLine, console);
        if (exitCode != 0)
        {
            string message = GettextCatalog.GetString("Unable to generate baseline");
            progressMonitor.Log.WriteLine(message);

            return FilePath.Null;
        }

        return commandLine.VSMacBaseLineFile;
    }

    Task<int> RunCheckAsync(
        Addin addin,
        FilePath baseLineFileName,
        FilePath compatDiffIgnoreFileName,
        FilePath compatDiffOutputFileName,
        OutputProgressMonitor progressMonitor,
        OperationConsole console)
    {
        string message = GettextCatalog.GetString("========== Checking extension {0} {1} ==========", addin.Name, addin.Version);
        progressMonitor.Log.WriteLine(message);

        var commandLine = new VSMacAddinCompactCheckerCommandLine();
        commandLine.AddinDirectory = Path.GetDirectoryName(addin.AddinFile);
        commandLine.VSMacBaseLineFile = baseLineFileName;
        commandLine.CompatDiffIgnoreFile = compatDiffIgnoreFileName;
        commandLine.CompatDiffOutputFile = compatDiffOutputFileName;

        return RunCheckAsync(commandLine, console);
    }

    async Task<int> RunCheckAsync(VSMacAddinCompactCheckerCommandLine commandLine, OperationConsole console)
    {
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

        string fullMessage = builder;
        return fullMessage;
    }

    static void SafeRemoveVSMacBaseLineReport(FilePath baseLineFileName)
    {
        FilePath directory = baseLineFileName.ParentDirectory;
        if (directory.IsNullOrEmpty)
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex)
        {
            LoggingService.LogError("Could not remove baseline report directory", ex);
        }
    }
}

