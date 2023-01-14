﻿
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace resource.package
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CONSTANT.GUID)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class PreviewZIP : AsyncPackage
    {
        internal static class CONSTANT
        {
            public const string APPLICATION = "Visual Studio";
            public const string COPYRIGHT = "Copyright (c) 2020-2022 by Viacheslav Lozinskyi. All rights reserved.";
            public const string DESCRIPTION = "Quick preview of ZIP files";
            public const string GUID = "3D481846-362D-429E-AFF7-0BF74A594CF9";
            public const string NAME = "Preview-ZIP";
            public const string VERSION = "1.0.13";
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            {
                extension.AnyPreview.Connect();
                extension.AnyPreview.Register(".ZIP", new preview.VSPreview());
            }
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }
            try
            {
                if (string.IsNullOrEmpty(atom.Trace.GetFailState(CONSTANT.APPLICATION)) == false)
                {
                    var a_Context = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                    if (a_Context != null)
                    {
                        var a_Context1 = (OutputWindowPane)null;
                        for (var i = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Count; i >= 1; i--)
                        {
                            if (a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Item(i).Name == "MetaOutput")
                            {
                                a_Context1 = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Item(i);
                                break;
                            }
                        }
                        if (a_Context1 == null)
                        {
                            a_Context1 = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Add("MetaOutput");
                        }
                        if (a_Context1 != null)
                        {
                            a_Context1.OutputString("\r\n" + CONSTANT.NAME + " extension doesn't work without MetaOutput.\r\n    Please install it (https://www.metaoutput.net/download)\r\n");
                            a_Context1.Activate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        protected override int QueryClose(out bool canClose)
        {
            {
                extension.AnyPreview.Disconnect();
                canClose = true;
            }
            return VSConstants.S_OK;
        }
    }
}
