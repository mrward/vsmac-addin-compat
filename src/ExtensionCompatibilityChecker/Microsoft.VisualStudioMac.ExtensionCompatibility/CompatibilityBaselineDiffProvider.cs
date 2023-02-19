//
// CompatibilityBaselineDiffProvider.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2023 Microsoft
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

using Mono.Addins;
using MonoDevelop.Core;

namespace Microsoft.VisualStudioMac.ExtensionCompatibility
{
    static class CompatibilityBaselineDiffProvider
    {
        static List<(Addin Addin, FilePath FileName)> addinDiffFiles =
            new List<(Addin Addin, FilePath FileName)>();

        public static void AddTemporaryDiffFile(Addin addin, FilePath fileName)
        {
            addinDiffFiles.Add((addin, fileName));
        }

        public static FilePath GetExistingDiffFileName(Addin addin)
        {
            FilePath diffFileName = GetAddinDiffFileName(addin);

            if (File.Exists(diffFileName))
            {
                return diffFileName;
            }

            return FilePath.Null;
        }

        public static void SaveDiffFiles()
        {
            if (!addinDiffFiles.Any())
            {
                return;
            }

            RemoveDiffFiles();

            Directory.CreateDirectory(GetDiffCacheDirectory());

            foreach ((Addin Addin, FilePath FileName) item in addinDiffFiles.ToArray())
            {
                FilePath addinDiffFileName = GetAddinDiffFileName(item.Addin);

                File.Move(item.FileName, addinDiffFileName, overwrite: true);

                addinDiffFiles.Remove(item);
            }
        }

        public static void RemoveDiffFiles()
        {
            FilePath diffCacheDirectory = GetDiffCacheDirectory();
            if (Directory.Exists(diffCacheDirectory))
            {
                Directory.Delete(diffCacheDirectory, true);
            }
        }

        static FilePath GetDiffCacheDirectory()
        {
            return UserProfile.Current.CacheDir.Combine("addin-compat-diffs");
        }

        static string GetAddinDiffFileName(Addin addin)
        {
            FilePath diffCacheDirectory = GetDiffCacheDirectory();
            return diffCacheDirectory.Combine($"{addin.LocalId}-diff.txt");
        }
    }
}

