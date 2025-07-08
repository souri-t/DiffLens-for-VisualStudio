using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DiffLens.VisualStudio.Services;
using Task = System.Threading.Tasks.Task;

namespace DiffLens.VisualStudio.Commands
{
    /// <summary>
    /// Command handler for previewing git diff
    /// </summary>
    internal sealed class PreviewDiffCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a74d0f88-1234-4567-8901-1234567890ab");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewDiffCommand"/> class.
        /// </summary>
        private PreviewDiffCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PreviewDiffCommand Instance
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
            Instance = new PreviewDiffCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Task.Run(async () => await ExecuteAsync());
        }

        /// <summary>
        /// Executes the preview diff command asynchronously
        /// </summary>
        private async Task ExecuteAsync()
        {
            try
            {
                var languageService = LanguageService.Instance;
                var configService = ConfigurationService.Instance;
                var gitService = GitService.Instance;
                var diffService = DiffService.Instance;

                if (configService == null || gitService == null || diffService == null)
                {
                    await ShowErrorAsync("Services not initialized");
                    return;
                }

                // Get current solution path
                var solutionPath = await GetSolutionPathAsync();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    await ShowErrorAsync("No solution loaded. Please open a solution containing a Git repository.");
                    return;
                }

                // Check if it's a git repository
                var isRepo = await gitService.IsGitRepositoryAsync(solutionPath);
                if (!isRepo)
                {
                    await ShowErrorAsync("Current solution is not in a Git repository.");
                    return;
                }

                // Get configuration
                var config = configService.GetReviewConfig();

                // Get repository root and generate preview
                var repositoryRoot = await gitService.GetRepositoryRootAsync(solutionPath);
                var previewContent = await diffService.GetDiffPreviewAsync(
                    repositoryRoot, 
                    null, // Use HEAD~1 as default
                    config.ContextLines, 
                    config.ExcludeDeletes, 
                    config.FileExtensions
                );

                // Show preview in a new document
                await ShowPreviewAsync(previewContent);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error generating diff preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the diff preview in a new document
        /// </summary>
        private async Task ShowPreviewAsync(string content)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    var doc = dte.Documents.Add("Text");
                    var textDoc = doc.Object() as EnvDTE.TextDocument;
                    if (textDoc != null)
                    {
                        var editPoint = textDoc.StartPoint.CreateEditPoint();
                        editPoint.Insert(content);
                        
                        // Note: Document.Name is read-only in newer VS versions
                        // The document will have a default name like "Document1.txt"
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to display diff preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current solution path
        /// </summary>
        private async Task<string> GetSolutionPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.Solution?.FullName != null)
            {
                return System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            }

            return null;
        }

        /// <summary>
        /// Shows an error message
        /// </summary>
        private async Task ShowErrorAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "DiffLens Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
