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
    /// Command handler for reviewing code with AI
    /// </summary>
    internal sealed class ReviewCodeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a74d0f88-1234-4567-8901-1234567890ab");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewCodeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ReviewCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static ReviewCodeCommand Instance
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
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ReviewCodeCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ReviewCodeCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Execute the command asynchronously
            Task.Run(async () => await ExecuteAsync());
        }

        /// <summary>
        /// Executes the code review command asynchronously
        /// </summary>
        private async Task ExecuteAsync()
        {
            try
            {
                var languageService = LanguageService.Instance;
                var configService = ConfigurationService.Instance;
                var gitService = GitService.Instance;
                var reviewService = ReviewService.Instance;

                if (configService == null || gitService == null || reviewService == null)
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
                var configErrors = reviewService.ValidateConfiguration(config);
                if (configErrors.Length > 0)
                {
                    var errorMessage = string.Join(", ", configErrors);
                    await ShowErrorAsync($"Configuration errors: {errorMessage}");
                    return;
                }

                // Show progress and perform review
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                statusBar?.SetText(languageService?.GetString("ReviewInProgress") ?? "Code review in progress...");

                try
                {
                    // Get git diff
                    var repositoryRoot = await gitService.GetRepositoryRootAsync(solutionPath);
                    var diff = await gitService.GetDiffAsync(repositoryRoot, null, config.ContextLines, config.ExcludeDeletes, config.FileExtensions);

                    if (string.IsNullOrEmpty(diff))
                    {
                        await ShowInfoAsync(languageService?.GetString("NoChangesFound") ?? "No changes found to review");
                        return;
                    }

                    // Send to AI for review
                    var reviewResult = await reviewService.ReviewCodeAsync(diff, config);

                    // Show results
                    await reviewService.ShowReviewResultsAsync(reviewResult);

                    statusBar?.SetText(languageService?.GetString("ReviewComplete") ?? "Code review completed successfully");
                }
                catch (Exception ex)
                {
                    var errorMessage = string.Format(languageService?.GetString("ReviewFailed") ?? "Code review failed: {0}", ex.Message);
                    await ShowErrorAsync(errorMessage);
                    statusBar?.SetText("");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Unexpected error: {ex.Message}");
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

        /// <summary>
        /// Shows an info message
        /// </summary>
        private async Task ShowInfoAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "DiffLens",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
