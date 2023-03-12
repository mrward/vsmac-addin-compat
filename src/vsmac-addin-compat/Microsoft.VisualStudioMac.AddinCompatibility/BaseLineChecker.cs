//
// BaseLineChecker.cs
// Based on https://github.com/KirillOsenkov/MetadataTools/blob/main/src/BinaryCompatChecker/Program.cs
//
// Author:
//       Kirill Osenkov <kirill.osenkov@microsoft.com>
//
// Copyright (c) 2017 Kirill Osenkov
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

namespace Microsoft.VisualStudioMac.AddinCompatibility;

class BaseLineChecker
{
    public string? CompatDiffOutputReportFileName { get; set; }
    public string[] IgnoreDiffLines { get; set; } = Array.Empty<string>();

    public bool Check(string[] oldBaseLine, string[] newBaseLine)
    {
        IEnumerable<string> added = newBaseLine.Except(oldBaseLine);

        if (added.Any())
        {
            CreateDiffReport(added);
        }

        IEnumerable<string> removed = Array.Empty<string>();

        if (IgnoreDiffLines.Any())
        {
            removed = IgnoreDiffLines.Except(added);
            added = added.Except(IgnoreDiffLines);
        }

        if (added.Any() || removed.Any())
        {
            ReportBaseLineErrorsToConsole(added, removed);
            return false;
        }

        return true;
    }

    void CreateDiffReport(IEnumerable<string> added)
    {
        if (CompatDiffOutputReportFileName is not null)
        {
            File.WriteAllLines(CompatDiffOutputReportFileName, added);
        }
    }

    static void OutputError(string text)
    {
        Console.Error.WriteLine(text);
    }

    static void ReportBaseLineErrorsToConsole(IEnumerable<string> added, IEnumerable<string> removed)
    {
        OutputError("The current assembly binary compatibility report is different from the baseline.");

        if (removed.Any())
        {
            OutputError("=================================");
            OutputError("These expected lines are missing:");

            foreach (var removedLine in removed)
            {
                OutputError(removedLine);
            }

            OutputError("=================================");
        }

        if (added.Any())
        {
            OutputError("=================================");
            OutputError("These actual lines are new:");

            foreach (var addedLine in added)
            {
                OutputError(addedLine);
            }

            OutputError("=================================");
        }
    }
}

