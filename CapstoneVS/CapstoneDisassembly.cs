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
    using Gee.External.Capstone;


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

        // TODO config
        public static CapstoneDisassembler<Gee.External.Capstone.Arm64.Arm64Instruction, Gee.External.Capstone.Arm64.Arm64Register, Gee.External.Capstone.Arm64.Arm64InstructionGroup, Gee.External.Capstone.Arm64.Arm64InstructionDetail> _disassembler;

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

            // TODO config
            _disassembler = CapstoneDisassembler.CreateArm64Disassembler(Gee.External.Capstone.DisassembleMode.Arm32);
            _disassembler.EnableDetails = true;
            _disassembler.Syntax = Gee.External.Capstone.DisassembleSyntaxOptionValue.Intel;
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
                return "No program is being debugged";
            }
            ulong instructionPointer;
            
            if (GetCurrentInstructionPointer(out instructionPointer) == false)
            {
                return "Error: Unable to get instruction pointer from current stack frame";
            }

            IDebugStackFrame2 frame;
            if (GetStackFrame(out frame) == false)
            {
                return "Unable to read current stack frame";
            }

            var lines = new List<string>();

            ulong currentAddress = instructionPointer;
            const int numInstructions = 15;
            for (int i = 0; i < numInstructions; ++i)
            {
                string bytesString = "";
                string instructionString = "";
                if (GetInstructionBytesAtAddress(currentAddress, out byte[] bytes, frame) == false)
                {
                    bytesString = "- ERROR -";
                    instructionString = "- ERROR -";
                }
                else
                {
                    bytesString = BitConverter.ToString(bytes).Replace("-", "");
                    GetDisassembledInstruction(bytes, out instructionString, currentAddress);
                }
                // TODO length 4 of bytes hex string should be config
                lines.Add(String.Format("0x{0:X8}:   {1:X4}\t\t{2}", currentAddress, bytesString, instructionString));
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

            IDebugStackFrame2 stackFrame;
            if (GetStackFrame(out stackFrame))
            {
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

            return false;
        }

        private bool GetStackFrame(out IDebugStackFrame2 stackFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            stackFrame = null;

            EnvDTE.DTE dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (dte.Debugger.CurrentStackFrame is StackFrame2 currentFrame2)
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
                            stackFrame = frameInfo[0].m_pFrame;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool GetInstructionBytesAtAddress(ulong address, out byte[] bytes, IDebugStackFrame2 frame)
        {
            // TODO length 4 of memory read should be config
            const uint length = 4;
            bytes = new byte[length];
            string expr = String.Format("0x{0:X8}", address);

            return ReadDebugeeMemory(expr, ref bytes, length, frame);
        }

        private bool GetDisassembledInstruction(byte[] bytes, out string instructionString, ulong startingAddress)
        {
            instructionString = "";

            // TODO config
            const int length = 4;
            try
            {
                var instructions = _disassembler.Disassemble(bytes, length, (long)startingAddress);
                if (instructions.Length != 1)
                {
                    // expected 1 instruction to be disassemble (quite TEMP)
                    instructionString = "- Error -";
                    return false;
                }

                instructionString = String.Format("{0,-10} {1}", instructions[0].Mnemonic, instructions[0].Operand);
                return true;
            }
            catch
            {
                instructionString = "- Error -";
                return false;
            }
        }

        private bool ReadDebugeeMemory(string expr, ref byte[] buffer, uint length, IDebugStackFrame2 frame)
        {
            if (frame.GetExpressionContext(out IDebugExpressionContext2 expressionContext) != VSConstants.S_OK)
            {
                // failed to get expression context
                return false;
            }

            if (expressionContext.ParseText(
                    expr,
                    enum_PARSEFLAGS.PARSE_EXPRESSION,
                    10,
                    out IDebugExpression2 expression,
                    out string parseError,
                    out uint parseErrorCharIndex) != VSConstants.S_OK)
            {
                // failed to parse expression
                return false;
            }

            if (expression.EvaluateSync(
                    enum_EVALFLAGS.EVAL_NOSIDEEFFECTS,
                    unchecked((uint)Timeout.Infinite),
                    null,
                    out IDebugProperty2 debugProperty) != VSConstants.S_OK)
            {
                // failed to execute parsed expression
                return false;
            }

            if (debugProperty.GetMemoryContext(out IDebugMemoryContext2 memoryContext) != VSConstants.S_OK)
            {
                // failed to get memory context
                return false;
            }

            if (debugProperty.GetMemoryBytes(out IDebugMemoryBytes2 memoryBytes) != VSConstants.S_OK)
            {
                // failed to get memory bytes
                return false;
            }

            uint unreadable = 0;
            if (memoryBytes.ReadAt(memoryContext, length, buffer, out uint writtenBytes, ref unreadable) != VSConstants.S_OK)
            {
                // read failed
                return false;
            }

            return true;
        }
    }
}
