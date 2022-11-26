//
// TemporaryFile.cs
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

namespace Microsoft.VisualStudioMac.AddinCompatibility;

class TemporaryFile : IDisposable
{
    readonly string directory;

    public TemporaryFile(string name = "report.txt")
    {
        directory = CreateTempDirectory();

        FileName = Path.Combine(directory, name);
    }

    public string FileName { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Ignore
        }
    }

    static string CreateTempDirectory()
    {
        var random = new Random();
        string directory;

        while (true)
        {
            directory = Path.Combine(Path.GetTempPath(), "vsmac-addin-compat-" + random.Next());

            if (!Directory.Exists(directory))
            {
                break;
            }
        }

        Directory.CreateDirectory(directory);

        return directory;
    }
}

