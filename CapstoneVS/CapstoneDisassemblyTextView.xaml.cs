
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ComponentModelHost;

namespace CapstoneVS
{
    /// <summary>
    /// Interaction logic for CapstoneDisassemblyTextView.xaml
    /// </summary>
    public partial class CapstoneDisassemblyTextView : UserControl
    {
        private ToolWindowWithEditor<CapstoneDisassemblyTextView> toolWindow;
        private IVsTextView textView;

        public CapstoneDisassemblyTextView()
        {
            InitializeComponent();

            toolWindow = new ToolWindowWithEditor<CapstoneDisassemblyTextView>();
        }

        private void ClearEditor()
        {
            toolWindow.ClearEditor();

            this.DisassemblyEditor.Content = null;
        }

        public void ShowDisassembly()
        {
            var tuple = this.toolWindow.SetDisplayedDisassembly();
            if (tuple != null)
            {
                this.DisassemblyEditor = (System.Windows.Controls.ContentControl)tuple.Item1;
                this.textView = tuple.Item2;
            }
        }
    }
}
