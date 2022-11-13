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

            Run();

            return 0;
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

    static void Run()
    {
        foreach (string addinDirectory in options!.AddinDirectories)
        {
            CheckAddinDirectoryCompat(addinDirectory);
        }
    }

    static void CheckAddinDirectoryCompat(string addinDirectory)
    {
        Console.WriteLine("Checking Addin compat: '{0}'", addinDirectory);

        using var checker = new AddinCompatChecker();
        checker.AddinDirectory = addinDirectory;
        checker.VisualStudioForMacDirectory = options!.VSMacAppBundle;

        bool result = checker.Check();

        if (result)
        {
            Console.WriteLine("Passed: Addin compat: '{0}'", addinDirectory);
        }
        else
        {
            Console.WriteLine("Failed: Addin compat: '{0}'", addinDirectory);
        }

        Console.WriteLine();
    }
}

