namespace CapstoneVS
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("4c7ad2ba-eaad-4abd-bf04-974462f52aef")]
    public class CapstoneDisassembly : ToolWindowPane
    {
        //CapstoneDisassemblyDebugListener _debugListener;
        CapstoneDisassemblyControl _disassemblyControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapstoneDisassembly"/> class.
        /// </summary>
        public CapstoneDisassembly() : base(null)
        {
            this.Caption = "Capstone Disassembly";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            _disassemblyControl = new CapstoneDisassemblyControl();
            this.Content = _disassemblyControl;

            //_debugListener = new CapstoneDisassemblyDebugListener(_disassemblyControl);
        }

        internal void ResetDisplay(IOleServiceProvider oleServiceProvider)
        {
            var serviceProvider = oleServiceProvider.GetServiceProvider();
            var vsEditorAdaptersFactoryService = serviceProvider.GetExportedValue<IVsEditorAdaptersFactoryService>();
            var editorFactory = serviceProvider.GetExportedValue<IEditorFactory>();

            var vsTextBuffer = vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvider);

            var contentTypeKey = new Guid(0x1beb4195, 0x98f4, 0x4589, 0x80, 0xe0, 0x48, 12, 0xe3, 0x2f, 240, 0x59);
            var vsUserData = (IVsUserData)vsTextBuffer;
            vsUserData.SetData(ref contentTypeKey, "text");

            vsTextBuffer.InitializeContent("", 0);
            var textBuffer = vsEditorAdaptersFactoryService.GetDataBuffer(vsTextBuffer);

            var vsTextView = editorFactory.CreateVsTextView(
                vsTextBuffer,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.PrimaryDocument);

            var wpfTextViewHost = vsEditorAdaptersFactoryService.GetWpfTextViewHost(vsTextView);
            _disassemblyControl.TextViewControl = wpfTextViewHost.HostControl;
        }
    }
}
