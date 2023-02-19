//
// ExtensionCompatibilityConsolePad.cs
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

using System;
using AppKit;
using MonoDevelop.Components;
using MonoDevelop.Components.Declarative;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components.LogView;

namespace Microsoft.VisualStudioMac.ExtensionCompatibility
{
    class ExtensionCompatibilityConsolePad : PadContent
    {
        static NSView? logView;
        static LogViewProgressMonitor? progressMonitor;
        static LogViewController? logViewController;
        static bool isSaveBaselineButtonEnabled;
        static bool isResetBaselineButtonEnabled;

        static ToolbarButtonItem? saveBaselineButton;
        static ToolbarButtonItem? resetBaselineButton;

        public static void Initialize()
        {
            Runtime.AssertMainThread();

            CreateLogView();
        }

        protected override void Initialize(IPadWindow window)
        {
            var toolbar = new Toolbar();

            saveBaselineButton = new ToolbarButtonItem(toolbar.Properties, nameof(saveBaselineButton));
            saveBaselineButton.Clicked += SaveBaselineButtonClick;
            saveBaselineButton.Label = GettextCatalog.GetString("Save Baseline");
            saveBaselineButton.Enabled = isSaveBaselineButtonEnabled;
            toolbar.AddItem(saveBaselineButton);

            resetBaselineButton = new ToolbarButtonItem(toolbar.Properties, nameof(resetBaselineButton));
            resetBaselineButton.Clicked += ResetBaselineButtonClick;
            resetBaselineButton.Label = GettextCatalog.GetString("Reset Baseline");
            resetBaselineButton.Enabled = isResetBaselineButtonEnabled;
            toolbar.AddItem(resetBaselineButton);

            window.SetToolbar(toolbar, DockPositionType.Top);
        }

        public static bool IsSaveBaselineButtonEnabled
        {
            get { return isSaveBaselineButtonEnabled; }
            set
            {
                isSaveBaselineButtonEnabled = value;
                if (saveBaselineButton is not null)
                {
                    saveBaselineButton.Enabled = value;
                }
            }
        }

        public static bool IsResetBaselineButtonEnabled
        {
            get { return isResetBaselineButtonEnabled; }
            set
            {
                isResetBaselineButtonEnabled = value;
                if (resetBaselineButton is not null)
                {
                    resetBaselineButton.Enabled = value;
                }
            }
        }

        public override Control Control
        {
            get { return logView; }
        }

        public static LogViewController? LogView
        {
            get { return logViewController; }
        }

        void ResetBaselineButtonClick(object? sender, EventArgs e)
        {
            try
            {
                CompatibilityBaselineDiffProvider.RemoveDiffFiles();
            }
            catch (Exception ex)
            {
                MessageService.ShowError(GettextCatalog.GetString("Could not reset baselines"), ex);
            }
        }

        void SaveBaselineButtonClick(object? sender, EventArgs e)
        {
            try
            {
                CompatibilityBaselineDiffProvider.SaveDiffFiles();
            }
            catch (Exception ex)
            {
                MessageService.ShowError(GettextCatalog.GetString("Could not save baselines"), ex);
            }
        }

        static void CreateLogView()
        {
            if (logViewController != null)
            {
                return;
            }

            logViewController = new LogViewController("LogMonitor");
            logView = logViewController.Control.GetNativeWidget<NSView>();

            // Need to create a progress monitor to avoid a null reference exception
            // when LogViewController.WriteText is called.
            progressMonitor = (LogViewProgressMonitor)logViewController.GetProgressMonitor();
        }
    }
}

