﻿//
// CompatibilityCheckOperationConsole.cs
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
using MonoDevelop.Core.Execution;

namespace Microsoft.VisualStudioMac.ExtensionCompatibility;

class CompatibilityCheckOperationConsole : OperationConsole
{
    readonly OperationConsole wrappedOperationConsole;

    public CompatibilityCheckOperationConsole(OperationConsole wrappedOperationConsole)
        : base(wrappedOperationConsole.CancellationToken)
    {
        this.wrappedOperationConsole = wrappedOperationConsole;
    }

    public override TextReader In => wrappedOperationConsole.In;

    public override TextWriter Out => wrappedOperationConsole.Out;

    public override TextWriter Error => wrappedOperationConsole.Error;

    public override TextWriter Log => wrappedOperationConsole.Log;

    public override void Dispose()
    {
        // Do not dispose the operation console. The console's progress monitor is
        // re-used for multiple process service calls.
    }
}

