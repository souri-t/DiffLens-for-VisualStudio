using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using DiffLens.VisualStudio.Services;
using Task = System.Threading.Tasks.Task;

namespace DiffLens.VisualStudio.Commands
{
    /// <summary>
    /// Command handler for opening DiffLens settings and tool window
    /// </summary>
    internal sealed class OpenSettingsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// Tool Window Command ID.
        /// </summary>
        public const int ToolWindowCommandId = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a74d0f88-1234-4567-8901-1234567890ab");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSettingsCommand"/> class.
        /// </summary>
        private OpenSettingsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            // Settings command
            var settingsMenuCommandID = new CommandID(CommandSet, CommandId);
            var settingsMenuItem = new OleMenuCommand(this.ExecuteSettings, settingsMenuCommandID);
            commandService.AddCommand(settingsMenuItem);

            // Tool window command
            var toolWindowMenuCommandID = new CommandID(CommandSet, ToolWindowCommandId);
            var toolWindowMenuItem = new OleMenuCommand(this.ExecuteToolWindow, toolWindowMenuCommandID);
            commandService.AddCommand(toolWindowMenuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenSettingsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenSettingsCommand(package, commandService);
        }

        /// <summary>
        /// Executes the settings command
        /// </summary>
        private void ExecuteSettings(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Open the options dialog
                var package = this.package as DiffLensPackage;
                if (package != null)
                {
                    package.ShowOptionPage(typeof(OptionsPage));
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening settings: {ex.Message}",
                    "DiffLens Error",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Executes the tool window command
        /// </summary>
        private void ExecuteToolWindow(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var package = this.package as DiffLensPackage;
                if (package != null)
                {
                    Task.Run(async () => await package.ShowToolWindowAsync());
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening tool window: {ex.Message}",
                    "DiffLens Error",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
