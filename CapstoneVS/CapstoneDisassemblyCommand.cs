using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.ComponentModelHost;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace CapstoneVS
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CapstoneDisassemblyCommand
    {
        private Package _package;

        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapstoneDisassemblyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CapstoneDisassemblyCommand(Package package, OleMenuCommandService commandService)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }
            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            _package = package;

            CommandID menuCommandID = new CommandID(GuidList.guidCapstoneDisassemblyPackageCmdSet, (int)PkgCmdIDList.CapstoneDisassemblyCommandId);
            MenuCommand menuToolWin = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuToolWin);

            var componentModel = package.GetService<SComponentModel, IComponentModel>();
            var export = componentModel.DefaultExportProvider;
            _serviceProvider = export.GetExportedValue<SVsServiceProvider>();
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CapstoneDisassemblyCommand Instance
        {
            get;
            private set;
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        public IOleServiceProvider OleServiceProvider
        {
            get
            {
                return this._serviceProvider.GetOleServiceProvider();
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommandService commandService = package.GetService<IMenuCommandService, OleMenuCommandService>();
            Instance = new CapstoneDisassemblyCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            CapstoneDisassembly window = (CapstoneDisassembly)this._package.FindToolWindow(typeof(CapstoneDisassembly), 0, true);
            if (window == null || window.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            window.RefreshDisplay(OleServiceProvider);

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
