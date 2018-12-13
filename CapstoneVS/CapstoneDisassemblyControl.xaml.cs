namespace CapstoneVS
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for CapstoneDisassemblyControl.
    /// </summary>
    public partial class CapstoneDisassemblyControl : UserControl
    {
        //private CapstoneDisassemblyTextView _textView;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapstoneDisassemblyControl"/> class.
        /// </summary>
        public CapstoneDisassemblyControl()
        {
            this.InitializeComponent();

            //_textView = new CapstoneDisassemblyTextView();
            //this.DisassemblyTextView = _textView;
        }

        internal void ShowDisassembly()
        {
            //this._textView.ShowDisassembly();
        }
    }
}