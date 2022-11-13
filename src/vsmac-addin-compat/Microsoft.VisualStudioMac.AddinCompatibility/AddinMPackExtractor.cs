//
// AddinMPackExtractor.cs
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

using System.IO.Compression;

namespace Microsoft.VisualStudioMac.AddinCompatibility;

class AddinMPackExtractor : IDisposable
{
    readonly string addinMPackFileName;

    public AddinMPackExtractor(string addinMPackFileName)
    {
        this.addinMPackFileName = addinMPackFileName;
    }

    public string? AddinDirectory { get; private set; }

    public void Extract()
    {
        AddinDirectory = CreateTempDirectory();
        Unzip();
    }

    static string CreateTempDirectory()
    {
        string baseTempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string tempDirectory = baseTempDirectory;
        int index = 0;
        while (Directory.Exists(tempDirectory))
        {
            index++;
            tempDirectory = baseTempDirectory + index;
        }

        Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }

    void Unzip()
    {
        using var zip = ZipFile.OpenRead(addinMPackFileName);

        zip.ExtractToDirectory(AddinDirectory!);
    }

    public void Dispose()
    {
        if (AddinDirectory is null)
        {
            return;
        }

        // Ensure we are not trying to delete the root directory.
        string? parentDirectory = Path.GetDirectoryName(AddinDirectory);
        if (string.IsNullOrEmpty(parentDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(AddinDirectory, recursive: true);
        }
        catch
        {
            // Ignore errors
        }
    }
}

