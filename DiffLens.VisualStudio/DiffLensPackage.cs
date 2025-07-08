using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.TaskStatusCenter;
using DiffLens.VisualStudio.Commands;
using DiffLens.VisualStudio.ToolWindows;
using DiffLens.VisualStudio.Services;
using DiffLens.VisualStudio.Models;
using Task = System.Threading.Tasks.Task;

namespace DiffLens.VisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DiffLensPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(DiffLensWindow))]
    [ProvideOptionPage(typeof(OptionsPage), "DiffLens", "General", 0, 0, true)]
    public sealed class DiffLensPackage : AsyncPackage
    {
        /// <summary>
        /// DiffLensPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a74d0f88-1234-4567-8901-1234567890ab";

        public static DiffLensPackage? Instance { get; private set; }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on the service provider.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A progress object for reporting the initialization progress.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;

            // Initialize commands
            await ReviewCodeCommand.InitializeAsync(this);
            await PreviewDiffCommand.InitializeAsync(this);
            await OpenSettingsCommand.InitializeAsync(this);

            // Initialize services
            await ConfigurationService.InitializeAsync(this);
            await GitService.InitializeAsync(this);
            await ReviewService.InitializeAsync(this);
            await LanguageService.InitializeAsync(this);
            await CopilotService.InitializeAsync(this);
        }

        #endregion

        /// <summary>
        /// Shows the tool window
        /// </summary>
        public async Task ShowToolWindowAsync()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = await this.FindToolWindowAsync(typeof(DiffLensWindow), 0, true, this.DisposalToken);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }

    /// <summary>
    /// Options page for DiffLens configuration
    /// </summary>
    public class OptionsPage : DialogPage
    {
        private ConfigurationService? configService = ConfigurationService.Instance;

        [System.ComponentModel.Category("AI Provider")]
        [System.ComponentModel.DisplayName("LLM Provider")]
        [System.ComponentModel.Description("Choose between AWS Bedrock or VS Code Language Model")]
        public LLMProvider LLMProvider
        {
            get { return configService?.LLMProvider ?? Models.LLMProvider.Bedrock; }
            set { if (configService != null) configService.LLMProvider = value; }
        }

        [System.ComponentModel.Category("AI Provider")]
        [System.ComponentModel.DisplayName("System Prompt")]
        [System.ComponentModel.Description("System prompt to be sent to the AI model")]
        public string SystemPrompt
        {
            get { return configService?.SystemPrompt ?? ""; }
            set { if (configService != null) configService.SystemPrompt = value; }
        }

        [System.ComponentModel.Category("AI Provider")]
        [System.ComponentModel.DisplayName("Review Perspective")]
        [System.ComponentModel.Description("Review perspective/criteria for code analysis")]
        public string ReviewPerspective
        {
            get { return configService?.ReviewPerspective ?? ""; }
            set { if (configService != null) configService.ReviewPerspective = value; }
        }

        [System.ComponentModel.Category("AWS Bedrock")]
        [System.ComponentModel.DisplayName("AWS Access Key")]
        [System.ComponentModel.Description("AWS Access Key ID for Bedrock access")]
        public string AwsAccessKey
        {
            get { return configService?.AwsAccessKey ?? ""; }
            set { if (configService != null) configService.AwsAccessKey = value; }
        }

        [System.ComponentModel.Category("AWS Bedrock")]
        [System.ComponentModel.DisplayName("AWS Secret Key")]
        [System.ComponentModel.Description("AWS Secret Access Key for Bedrock access")]
        public string AwsSecretKey
        {
            get { return configService?.AwsSecretKey ?? ""; }
            set { if (configService != null) configService.AwsSecretKey = value; }
        }

        [System.ComponentModel.Category("AWS Bedrock")]
        [System.ComponentModel.DisplayName("AWS Region")]
        [System.ComponentModel.Description("AWS region for Bedrock service")]
        public string AwsRegion
        {
            get { return configService?.AwsRegion ?? "us-east-1"; }
            set { if (configService != null) configService.AwsRegion = value; }
        }

        [System.ComponentModel.Category("AWS Bedrock")]
        [System.ComponentModel.DisplayName("Model Name")]
        [System.ComponentModel.Description("Bedrock model name to use for code review")]
        public string ModelName
        {
            get { return configService?.ModelName ?? "anthropic.claude-3-5-sonnet-20241022-v2:0"; }
            set { if (configService != null) configService.ModelName = value; }
        }

        [System.ComponentModel.Category("Diff Configuration")]
        [System.ComponentModel.DisplayName("Context Lines")]
        [System.ComponentModel.Description("Number of context lines to show before and after changes in git diff")]
        public int ContextLines
        {
            get { return configService?.ContextLines ?? 50; }
            set { if (configService != null) configService.ContextLines = value; }
        }

        [System.ComponentModel.Category("Diff Configuration")]
        [System.ComponentModel.DisplayName("Exclude Deleted Files")]
        [System.ComponentModel.Description("Exclude deleted files and lines from git diff output")]
        public bool ExcludeDeletes
        {
            get { return configService?.ExcludeDeletes ?? true; }
            set { if (configService != null) configService.ExcludeDeletes = value; }
        }

        [System.ComponentModel.Category("Diff Configuration")]
        [System.ComponentModel.DisplayName("File Extensions")]
        [System.ComponentModel.Description("File extensions to include in diff output (e.g., '*.cs *.xaml *.json')")]
        public string FileExtensions
        {
            get { return configService?.FileExtensions ?? ""; }
            set { if (configService != null) configService.FileExtensions = value; }
        }

        [System.ComponentModel.Category("Interface")]
        [System.ComponentModel.DisplayName("Interface Language")]
        [System.ComponentModel.Description("Interface language for the extension")]
        public InterfaceLanguage InterfaceLanguage
        {
            get { return configService?.InterfaceLanguage ?? Models.InterfaceLanguage.English; }
            set { if (configService != null) configService.InterfaceLanguage = value; }
        }
    }
}
