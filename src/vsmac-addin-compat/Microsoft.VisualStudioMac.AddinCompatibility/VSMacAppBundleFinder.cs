//
// VSMacAppBundleFinder.cs
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
using VSMacLocator;

namespace Microsoft.VisualStudioMac.AddinCompatibility
{
    static class VSMacAppBundleFinder
    {
        public static string? GetAppBundlePath(bool usePreview)
        {
            string bundleId = GetBundleId(usePreview);
            string?[]? urls = MacInterop.GetApplicationUrls(bundleId, out _);
            if (urls is null)
            {
                return null;
            }

            List<string> appBundlePaths = urls.Where(url => url is not null)
                .Select(url => url!)
                .ToList();

            if (!appBundlePaths.Any())
            {
                return null;
            }

            if (appBundlePaths.Count == 1)
            {
                return appBundlePaths[0];
            }

            // Prefer app bundle from "/Applications"
            IEnumerable<string> appBundlesInApplications = appBundlePaths
                .Where(appBundlePath => appBundlePath.StartsWith("/Applications/"));

            string? appBundlePath = appBundlesInApplications.FirstOrDefault();

            if (appBundlesInApplications.Count() == 1)
            {
                Console.WriteLine($"Multiple Visual Studio for Mac applications found. Using '{appBundlePath}'");

                return appBundlePath;
            }

            // Find latest version.
            appBundlePath = GetAppBundlePathForLatestVersion(appBundlePaths);
            if (appBundlePath is not null)
            {
                Console.WriteLine($"Multiple Visual Studio for Mac applications found. Using '{appBundlePath}'");
                return appBundlePath;
            }

            appBundlePath = appBundlePaths.FirstOrDefault();

            Console.WriteLine($"Could not determine latest Visual Studio for Mac version. Using '{appBundlePath}'");

            return appBundlePath;
        }

        static string GetBundleId(bool usePreview)
        {
            if (usePreview)
            {
                return "com.microsoft.visual-studio-preview";
            }

            return "com.microsoft.visual-studio";
        }

        static string? GetAppBundlePathForLatestVersion(List<string> appBundlePaths)
        {
            var appBundleInfo = new List<(string Path, Version Version)>();

            var versions = new List<Version>();
            foreach (string appBundlePath in appBundlePaths)
            {
                string? versionText = GetAppBundleVersion(appBundlePath);
                if (!Version.TryParse(versionText, out Version? version))
                {
                    Console.WriteLine($"Could not determine app bundle version '{appBundlePath}'");
                    continue;
                }

                appBundleInfo.Add((appBundlePath, version));
            }

            appBundleInfo.Sort((x, y) => x.Version.CompareTo(y.Version));

            return appBundleInfo
                .Select(info => info.Path)
                .LastOrDefault();
        }

        static string? GetAppBundleVersion(string appBundlePath)
        {
            const string bundleShortVersionKey = "CFBundleShortVersionString";

            string infoPlistPath = Path.Combine(appBundlePath, "Contents", "Info.plist");

            Dictionary<string, string?> values = MacInterop.GetStringValuesFromPlist(
                infoPlistPath,
                bundleShortVersionKey);

            values.TryGetValue(bundleShortVersionKey, out string? version);

            return version;
        }
    }
}

