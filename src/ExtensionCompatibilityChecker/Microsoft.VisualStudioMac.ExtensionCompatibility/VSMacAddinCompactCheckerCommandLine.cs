//
// VSMacAddinCompactCheckerCommandLine.cs
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

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace Microsoft.VisualStudioMac.ExtensionCompatibility;

class VSMacAddinCompactCheckerCommandLine
{
    static VSMacAddinCompactCheckerCommandLine()
    {
        FilePath assemblyFileName = typeof(VSMacAddinCompactCheckerCommandLine).Assembly.Location;
        CompatCheckerFileName = assemblyFileName.ParentDirectory.Combine("vsmac-addin-compat.dll");

        assemblyFileName = typeof(Runtime).Assembly.Location;

        VisualStudioMacAppBundleDirectory = assemblyFileName
            .ParentDirectory
            .ParentDirectory
            .ParentDirectory;

        DotNetFileName = VisualStudioMacAppBundleDirectory.Combine("Contents", "dotnet", "dotnet");
    }

    public static string DotNetFileName { get; }

    static FilePath CompatCheckerFileName { get; }
    static FilePath VisualStudioMacAppBundleDirectory { get; }

    public string? AddinDirectory { get; set; }

    public string BuildCommandLine()
    {
        var processArgumentBuilder = new ProcessArgumentBuilder();

        processArgumentBuilder.AddQuoted(CompatCheckerFileName);

        processArgumentBuilder.Add("--vsmac-app");
        processArgumentBuilder.AddQuoted(VisualStudioMacAppBundleDirectory);

        processArgumentBuilder.Add("--addin-dir");
        processArgumentBuilder.AddQuoted(AddinDirectory);

        return processArgumentBuilder.ToString();
    }
}

