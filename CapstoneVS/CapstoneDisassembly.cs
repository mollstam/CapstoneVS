namespace CapstoneVS
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
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

        }
    }
}
