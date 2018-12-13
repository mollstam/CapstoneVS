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

        public Control TextViewControl
        {
            get { return (Control)_hostControl.Content; }
            set { _hostControl.Content = value; }
        }

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
            throw new NotImplementedException();
            //this._textView.ShowDisassembly();
        }
    }
}