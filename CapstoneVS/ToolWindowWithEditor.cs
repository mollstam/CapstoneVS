using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

/**
 * Based on code from https://github.com/yysun/Git-Source-Control-Provider/
 */

namespace CapstoneVS
{
    class ToolWindowWithEditor<T> : ToolWindowPane where T : Control
    {
        private IVsTextView vsTextView;
        private IVsCodeWindow vsCodeWindow;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider cachedOleServiceProvider;

        private ITextBufferFactoryService textBufferFactoryService;
        private IContentTypeRegistryService contentTypeRegistryService;
        private ITextEditorFactoryService textEditorFactoryService;
        private IVsEditorAdaptersFactoryService editorAdaptersFactoryService;

        public ToolWindowWithEditor()
        {
            var componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
            this.textBufferFactoryService = componentModel.GetService<ITextBufferFactoryService>();
            this.contentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();
            this.textEditorFactoryService = componentModel.GetService<ITextEditorFactoryService>();
            this.editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
        }

        public Tuple<Control, IVsTextView> SetDisplayedDisassembly()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // @TODO take a parameter of what memory to disasm...

            string text = "foo bar\nbaz\n";
            IContentType contentType = contentTypeRegistryService.GetContentType("text");
            var textBuffer = textBufferFactoryService.CreateTextBuffer(text, contentType);
            var vsTextBuffer = editorAdaptersFactoryService.CreateVsTextBufferAdapter(OleServiceProvider, textBuffer.ContentType);
            var vsTextLines = (IVsTextLines)vsTextBuffer;

            ClearEditor();

            /*var editorFactory = OleServiceProvider.GetOleServiceProvider();

            this.vsCodeWindow = editorAdaptersFactoryService.CreateVsCodeWindowAdapter(OleServiceProvider);

            IVsCodeWindowEx codeWindowEx = (IVsCodeWindowEx)this.vsCodeWindow;
            INITVIEW[] initView = new INITVIEW[1];
            ErrorHandler.ThrowOnFailure(codeWindowEx.Initialize((uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER, VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter, szNameAuxUserContext: "", szValueAuxUserContext: "", InitViewFlags: 0, pInitView: initView));
            ErrorHandler.ThrowOnFailure(this.vsCodeWindow.SetBuffer(vsTextLines));

            ErrorHandler.ThrowOnFailure(this.vsCodeWindow.GetPrimaryView(out this.vsTextView));
            IWpfTextViewHost textViewHost = editorAdaptersFactoryService.GetWpfTextViewHost(this.vsTextView);
            if (textViewHost == null)
            {
                return null;
            }

            // @TODO set options on text view

            Control part1 = textViewHost.HostControl;
            IVsTextView part2 = this.vsTextView;
            return Tuple.Create<Control, IVsTextView>(part1, part2);*/

            return null;
        }

        public void ClearEditor()
        {
            if (this.vsCodeWindow != null)
            {
                this.vsCodeWindow.Close();
                this.vsCodeWindow = null;
            }

            if (this.vsTextView != null)
            {
                this.vsTextView.CloseView();
                this.vsTextView = null;
            }
        }

        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider OleServiceProvider
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this.cachedOleServiceProvider == null)
                {
                    IObjectWithSite objWithSite = ServiceProvider.GlobalProvider;
                    Guid interfaceIID = typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider).GUID;
                    IntPtr rawSP;
                    objWithSite.GetSite(ref interfaceIID, out rawSP);
                    try
                    {
                        if (rawSP != IntPtr.Zero)
                        {
                            this.cachedOleServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Marshal.GetObjectForIUnknown(rawSP);
                        }
                    }
                    finally
                    {
                        if (rawSP != IntPtr.Zero)
                        {
                            Marshal.Release(rawSP);
                        }
                    }
                }

                return this.cachedOleServiceProvider;
            }
        }
    }
}
