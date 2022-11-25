//
// AddinCompatChecker.cs
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

using BinaryCompatChecker;

namespace Microsoft.VisualStudioMac.AddinCompatibility;

class AddinCompatChecker : IDisposable
{
    TemporaryReportFile? reportFile;

    public string? VSMacDirectory { get; set; }
    public string? AddinDirectory { get; set; }
    public string? ReportFileName { get; set; }
    public string[]? BaseLine { get; set; }

    public bool Check()
    {
        ArgumentNullException.ThrowIfNull(VSMacDirectory);

        string? configFile = null;

        IEnumerable<string> allFiles = Checker.GetFiles(VSMacDirectory, configFile, out _);
        IEnumerable<string> startFiles = allFiles;

        if (!string.IsNullOrEmpty(AddinDirectory))
        {
            IEnumerable<string> allAddinFiles = Checker.GetFiles(
                AddinDirectory,
                configFile,
                out List<string>? addinStartFiles);

            allFiles = allFiles.Concat(allAddinFiles);
            startFiles = allAddinFiles;
        }

        ConfigureReportFileName();

        Checker.ReportIntPtrConstructors = true;
        Checker.ReportVersionMismatch = false;
        Checker.ReportEmbeddedInteropTypes = false;
        Checker.ReportVersionMismatch = false;

        var checker = new Checker();

        bool success = checker.Check(
            VSMacDirectory,
            allFiles,
            startFiles,
            ReportFileName);

        if (!success)
        {
            Console.WriteLine("Unexpected Checker.Check failure");
        }

        if (BaseLine is not null)
        {
            string[] newBaseLine = GetReportLines();
            return BaseLineChecker.Check(BaseLine, newBaseLine);
        }

        return success;
    }

    void ConfigureReportFileName()
    {
        if (string.IsNullOrEmpty(ReportFileName))
        {
            reportFile = new TemporaryReportFile();
            ReportFileName = reportFile.FileName;
        }
        else
        {
            string? directory = Path.GetDirectoryName(ReportFileName);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    public void Dispose()
    {
        reportFile?.Dispose();
        reportFile = null;
    }

    public string[] GetReportLines()
    {
        if (ReportFileName is not null)
        {
            if (File.Exists(ReportFileName))
            {
                return File.ReadAllLines(ReportFileName);
            }
            return Array.Empty<string>();
        }

        throw new UserException("Report file not set");
    }
}

