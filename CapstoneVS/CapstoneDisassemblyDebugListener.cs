using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft;

namespace CapstoneVS
{
    class CapstoneDisassemblyDebugListener : IVsDebuggerEvents, IDebugEventCallback2
    {

        readonly uint _debuggerEventsCookie;

        public CapstoneDisassemblyDebugListener()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IServiceProvider serviceProvider = (IServiceProvider)CapstoneDisassemblyCommand.Instance.package;
            IVsDebugger debugger = serviceProvider.GetService(typeof(SVsShellDebugger)) as IVsDebugger;
            Assumes.Present(debugger);

            if (debugger.AdviseDebuggerEvents(this, out _debuggerEventsCookie) != VSConstants.S_OK)
            {
                Console.WriteLine("Failed to register for debugger events");
            }

            if (debugger.AdviseDebugEventCallback(this) != VSConstants.S_OK)
            {
                Console.WriteLine("Failed to register debug event callback");
            }
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            return VSConstants.S_OK;
        }

        public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
        {
            return VSConstants.S_OK;
        }
    }
}
