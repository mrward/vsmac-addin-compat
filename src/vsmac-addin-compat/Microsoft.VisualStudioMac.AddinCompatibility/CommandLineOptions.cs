//
// CommandLineOptions.cs
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

using System.Reflection;
using Mono.Options;

namespace Microsoft.VisualStudioMac.AddinCompatibility;

class CommandLineOptions
{
    OptionSet GetOptionSet()
    {
        return new OptionSet {
            { "h|?|help", "Show help", h => Help = true },
            { "vsmac-app=", "Visual Studio for Mac app bundle directory", app => VSMacAppBundle = app },
            { "addin-dir=", "Directory containing addin files", directory => AddinDirectories.Add(directory) },
            { "addin=", "Addin .mpack filename", fileName => AddinFileNames.Add(fileName) },
        };
    }

    public bool Help { get; private set; }
    public string? VSMacAppBundle { get; private set; }
    public List<string> AddinDirectories { get; private set; } = new List<string>();
    public List<string> AddinFileNames { get; private set; } = new List<string>();
    public List<string> RemainingArgs { get; private set; } = new List<string>();
    public Exception? Error { get; private set; }

    public bool HasError
    {
        get { return Error != null; }
    }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        OptionSet optionsSet = options.GetOptionSet();

        try
        {
            options.RemainingArgs = optionsSet.Parse(args);
            options.Validate();
        }
        catch (OptionException ex)
        {
            options.Error = ex;
        }

        return options;
    }

    public void ShowError()
    {
        if (Error == null)
        {
            return;
        }

        Console.WriteLine("ERROR: {0}", Error);
        Console.WriteLine("Pass --help for usage information.");
    }

    public void ShowHelp()
    {
        string? versionString = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .ToString();

        if (!string.IsNullOrEmpty(versionString))
        {
            Console.WriteLine($"vsmac-addin-compat {versionString}");
            Console.WriteLine();
        }

        Console.WriteLine("Usage:");

        OptionSet optionsSet = GetOptionSet();
        optionsSet.WriteOptionDescriptions(Console.Out);
    }

    void Validate()
    {
        ValidateAddinDirectories();
        ValidateVSMacDirectory();
        ValidateAddinFileNames();
    }

    void ValidateAddinDirectories()
    {
        for (int i = 0; i < AddinDirectories.Count; ++i)
        {
            string? addinDirectory = AddinDirectories[i];
            if (addinDirectory is null)
            {
                continue;
            }

            addinDirectory = GetFullPath(addinDirectory);
            AddinDirectories[i] = addinDirectory!;

            if (!IsValidDirectory(addinDirectory))
            {
                throw new UserException($"Addin directory does not exist: '{addinDirectory}'");
            }
        }
    }

    void ValidateVSMacDirectory()
    {
        VSMacAppBundle = GetFullPath(VSMacAppBundle);

        if (!IsValidDirectory(VSMacAppBundle))
        {
            throw new UserException($"Visual Studio for Mac directory does not exist: '{VSMacAppBundle}'");
        }
    }

    static bool IsValidDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return true;
        }

        return Directory.Exists(directory);
    }

    static string? GetFullPath(string? path)
    {
        if (path is null)
        {
            return path;
        }

        string currentDirectory = Directory.GetCurrentDirectory();
        path = Path.Combine(currentDirectory, path);

        return Path.GetFullPath(path);
    }

    void ValidateAddinFileNames()
    {
        for (int i = 0; i < AddinFileNames.Count; ++i)
        {
            string? fileName = AddinFileNames[i];
            if (fileName is null)
            {
                continue;
            }

            fileName = GetFullPath(fileName);
            AddinFileNames[i] = fileName!;

            if (!File.Exists(fileName))
            {
                throw new UserException($"Addin file does not exist '{fileName}'");
            }
        }
    }
}

