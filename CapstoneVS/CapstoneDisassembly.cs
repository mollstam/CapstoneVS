namespace CapstoneVS
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using EnvDTE90a;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Debugger.CallStack;
    using Microsoft.VisualStudio.Debugger.Interop;
    using Microsoft.VisualStudio.Debugger.Interop.Internal;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
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
        CapstoneDisassemblyDebugListener _debugListener;
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

            _debugListener = new CapstoneDisassemblyDebugListener();
            _debugListener.OnBreak += OnDebugBreak;
            _debugListener.OnDebugEnd += OnDebugEnd;
        }

        private void OnDebugEnd()
        {
            RefreshDisplay(CapstoneDisassemblyCommand.Instance.OleServiceProvider);
        }

        private void OnDebugBreak()
        {
            RefreshDisplay(CapstoneDisassemblyCommand.Instance.OleServiceProvider);
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();

            RefreshDisplay(CapstoneDisassemblyCommand.Instance.OleServiceProvider);
        }

        internal void RefreshDisplay(IOleServiceProvider oleServiceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var serviceProvider = oleServiceProvider.GetServiceProvider();
            var vsEditorAdaptersFactoryService = serviceProvider.GetExportedValue<IVsEditorAdaptersFactoryService>();
            var editorFactory = serviceProvider.GetExportedValue<IEditorFactory>();

            var vsTextBuffer = vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvider);

            var contentTypeKey = new Guid(0x1beb4195, 0x98f4, 0x4589, 0x80, 0xe0, 0x48, 12, 0xe3, 0x2f, 240, 0x59);
            var vsUserData = (IVsUserData)vsTextBuffer;
            vsUserData.SetData(ref contentTypeKey, "text");

            string content = GenerateContent();
            vsTextBuffer.InitializeContent(content, content.Length);
            vsTextBuffer.SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);

            var textBuffer = vsEditorAdaptersFactoryService.GetDataBuffer(vsTextBuffer);

            var vsTextView = editorFactory.CreateVsTextView(
                vsTextBuffer,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.PrimaryDocument);

            var wpfTextViewHost = vsEditorAdaptersFactoryService.GetWpfTextViewHost(vsTextView);
            _disassemblyControl.TextViewControl = wpfTextViewHost.HostControl;
        }

        internal string GenerateContent()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (HasCurrentStackFrame() == false)
            {
                return "No program in being debugged";
            }
            ulong instructionPointer;
            
            if (GetCurrentInstructionPointer(out instructionPointer) == false)
            {
                return "Error: Unable to get instruction pointer from current stack frame";
            }

            var lines = new List<string>();

            ulong currentAddress = instructionPointer;
            for (int i = 0; i < 5; ++i)
            {
                lines.Add(String.Format("{0:X8}", currentAddress) + "\t\thello!");
                currentAddress += 4;
            }

            return String.Join("\n", lines);
        }

        private bool HasCurrentStackFrame()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (dte == null || dte.Debugger.CurrentStackFrame == null)
            {
                return false;
            }

            return true;
        }

        private bool GetCurrentInstructionPointer(out ulong ptr)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ptr = 0;

            EnvDTE.DTE dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;

            var dkmStackFrame = DkmStackFrame.ExtractFromDTEObject(dte.Debugger.CurrentStackFrame);
            if (dkmStackFrame != null)
            {
                // Concord frame
                var registers = dkmStackFrame.Registers;
                if (registers == null)
                {
                    return false;
                }
                ptr = registers.GetInstructionPointer();
                return true;
            }

            // Ehm...

            StackFrame2 currentFrame2 = dte.Debugger.CurrentStackFrame as StackFrame2;
            if (currentFrame2 != null)
            {

                IVsDebugger debugService = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
                IDebuggerInternal11 debuggerServiceInternal = (IDebuggerInternal11)debugService;
                IDebugThread2 debugThread = debuggerServiceInternal.CurrentThread;

                if (debugThread.EnumFrameInfo(
                        enum_FRAMEINFO_FLAGS.FIF_FRAME,
                        0, out IEnumDebugFrameInfo2 enumDebugFrameInfo2
                    ) == VSConstants.S_OK)
                {
                    enumDebugFrameInfo2.Reset();
                    if (enumDebugFrameInfo2.Skip(currentFrame2.Depth - 1) == VSConstants.S_OK)
                    {
                        FRAMEINFO[] frameInfo = new FRAMEINFO[1];
                        uint fetched = 0;
                        int hr = enumDebugFrameInfo2.Next(1, frameInfo, ref fetched);
                        if (hr == VSConstants.S_OK && fetched == 1)
                        {
                            IDebugStackFrame2 stackFrame = frameInfo[0].m_pFrame;
                            if (stackFrame.GetCodeContext(out IDebugCodeContext2 codeContext) == VSConstants.S_OK)
                            {
                                CONTEXT_INFO[] contextInfo = new CONTEXT_INFO[1];
                                if (codeContext.GetInfo(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS, contextInfo) == VSConstants.S_OK)
                                {
                                    ptr = Convert.ToUInt64(contextInfo[0].bstrAddress, 16);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
