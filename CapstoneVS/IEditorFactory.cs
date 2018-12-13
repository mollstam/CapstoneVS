using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapstoneVS
{
    internal interface IEditorFactory
    {
        //IVsEditorFactory VsEditorFactory { get; }

        //ITextBufferFactoryService TextBufferFactoryService { get; }

        //IVsTextBuffer CreateVsTextBuffer(ITextBuffer textBuffer, string name);

        IVsTextView CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles);

        /*bool OpenInNewWindow(
            ITextBuffer textBuffer,
            string name,
            Guid? editorTypeId = null,
            Guid? logicalViewId = null,
            Guid? languageServiceId = null);*/
    }
}
