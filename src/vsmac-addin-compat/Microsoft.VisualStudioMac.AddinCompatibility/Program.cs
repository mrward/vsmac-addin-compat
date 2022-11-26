//
// Program.cs
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

class Program
{
    static CommandLineOptions? options;
    static int FailedCount;
    static string[]? baseLine;
    static TemporaryReportFile? vsmacBinaryCompatConfigFile;

    static int Main(string[] args)
    {
        try
        {
            options = CommandLineOptions.Parse(args);
            if (options.HasError)
            {
                options.ShowError();
                return -1;
            }

            if (options.Help)
            {
                options.ShowHelp();
                return 0;
            }

            return Run();
        }
        catch (UserException ex)
        {
            Console.WriteLine("ERROR: {0}", ex.Message);
            return -1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: {0}", ex);
            return -1;
        }
    }

    static int Run()
    {
        vsmacBinaryCompatConfigFile = CreateVSMacBinaryCompatConfigFile();

        using TemporaryReportFile configFile = vsmacBinaryCompatConfigFile;

        if (options!.GenerateVSMacBaseLine)
        {
            GenerateVSMacBaseLine(options.VSMacBaseLineFileName);
        }
        else if (options.VSMacBaseLineFileName is not null)
        {
            baseLine = File.ReadAllLines(options.VSMacBaseLineFileName);
        }
        else
        {
            GenerateVSMacBaseLine();
        }

        foreach (string addinDirectory in options.AddinDirectories)
        {
            CheckAddinDirectoryCompat(addinDirectory);
        }

        foreach (string addinMPackFileName in options.AddinMPackFileNames)
        {
            CheckAddinMPackFileCompat(addinMPackFileName);
        }

        foreach (string addinMPackDirectory in options.AddinMPackDirectories)
        {
            CheckAddinMPackDirectoryCompat(addinMPackDirectory);
        }

        if (FailedCount > 0)
        {
            return 1;
        }
        return 0;
    }

    static TemporaryReportFile CreateVSMacBinaryCompatConfigFile()
    {
        var configFile = new TemporaryReportFile();

        vsmacBinaryCompatConfigFile = configFile;

        string contents = GetVSMacBinaryCompatConfig();
        File.WriteAllText(configFile.FileName, contents);

        return configFile;
    }

    static void GenerateVSMacBaseLine()
    {
        using var vsmacBaseLineFileName = new TemporaryReportFile();

        GenerateVSMacBaseLine(vsmacBaseLineFileName.FileName);
    }

    static void GenerateVSMacBaseLine(string? baseLineFileName)
    {
        Console.WriteLine("Generating baseline for Visual Studio for Mac");

        using var checker = new AddinCompatChecker();
        checker.VSMacDirectory = options!.VSMacAppBundle;
        checker.ReportFileName = options.VSMacBaseLineFileName;
        checker.VSMacCompatConfigFile = vsmacBinaryCompatConfigFile!.FileName;

        checker.Check();

        baseLine = checker.GetReportLines();

        Console.WriteLine("Baseline generated");
        Console.WriteLine();
    }

    static string GetVSMacBinaryCompatConfig()
    {
        Stream? stream = typeof(Program).Assembly.GetManifestResourceStream("VSMacBinaryCompatConfig.txt");
        if (stream is null)
        {
            return string.Empty;
        }

        using (stream)
        {
            using var streamReader = new StreamReader(stream);

            return streamReader.ReadToEnd();
        }
    }

    static void CheckAddinDirectoryCompat(string addinDirectory)
    {
        string addinIdentifier = GetAddinIdentifier(addinDirectory);
        CheckAddinDirectoryCompatCore(addinDirectory, addinIdentifier);
    }

    static string GetAddinIdentifier(string addinDirectoryOrFileName)
    {
        string addinIdentifier = addinDirectoryOrFileName;

        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), addinDirectoryOrFileName);
        if (!relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            addinIdentifier = relativePath;
        }

        return addinIdentifier;
    }

    static void CheckAddinDirectoryCompatCore(string addinDirectory, string addinIdentifier)
    {
        Console.WriteLine("Checking Addin compat: '{0}'", addinIdentifier);

        using var checker = new AddinCompatChecker();
        checker.AddinDirectory = addinDirectory;
        checker.VSMacDirectory = options!.VSMacAppBundle;
        checker.BaseLine = baseLine;
        checker.VSMacCompatConfigFile = vsmacBinaryCompatConfigFile!.FileName;

        bool result = checker.Check();

        if (result)
        {
            Console.WriteLine("Passed: Addin compat: '{0}'", addinIdentifier);
        }
        else
        {
            Console.WriteLine("Failed: Addin compat: '{0}'", addinIdentifier);
            FailedCount++;
        }

        Console.WriteLine();
    }

    static void CheckAddinMPackFileCompat(string addinMPackFileName)
    {
        using var mpackExtractor = new AddinMPackExtractor(addinMPackFileName);
        mpackExtractor.Extract();

        string addinIdentifier = GetAddinIdentifier(addinMPackFileName);

        CheckAddinDirectoryCompatCore(
            mpackExtractor.AddinDirectory!,
            addinIdentifier: addinMPackFileName);
    }

    static void CheckAddinMPackDirectoryCompat(string addinMPackDirectory)
    {
        int mpackFilesChecked = 0;

        foreach (string addinMPackFileName in Directory.GetFiles(addinMPackDirectory, "*.mpack", SearchOption.AllDirectories))
        {
            ++mpackFilesChecked;
            CheckAddinMPackFileCompat(addinMPackFileName);
        }

        if (mpackFilesChecked == 0)
        {
            Console.WriteLine("WARN: No .mpack files found in specified directories");
        }
    }
}

