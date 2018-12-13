using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;

namespace CapstoneVS
{
    [Export(typeof(IEditorFactory))]
    internal sealed class EditorFactory : IEditorFactory
    {
        private readonly SVsServiceProvider _vsServiceProvider;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly IOleServiceProvider _oleServiceProvider;

        [ImportingConstructor]
        internal EditorFactory(
            SVsServiceProvider vsServiceProvider,
            IVsEditorAdaptersFactoryService vsEditorAdaptersFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _vsServiceProvider = vsServiceProvider;
            _vsEditorAdaptersFactoryService = vsEditorAdaptersFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _oleServiceProvider = _vsServiceProvider.GetService<IOleServiceProvider, IOleServiceProvider>();
        }

        private IVsTextView CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles)
        {
            var textViewRoleSet = _textEditorFactoryService.CreateTextViewRoleSet(textViewRoles);
            var vsTextView = _vsEditorAdaptersFactoryService.CreateVsTextViewAdapter(_oleServiceProvider, textViewRoleSet);
            var hr = vsTextView.Initialize((IVsTextLines)vsTextBuffer, IntPtr.Zero, 0, null);
            ErrorHandler.ThrowOnFailure(hr);
            return vsTextView;
        }

        #region IEditorFactory

        IVsTextView IEditorFactory.CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles)
        {
            return CreateVsTextView(vsTextBuffer, textViewRoles);
        }

        #endregion
    }
}
