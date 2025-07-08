using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using DiffLens.VisualStudio.Models;
using DiffLens.VisualStudio.Services;

namespace DiffLens.VisualStudio.ToolWindows
{
    /// <summary>
    /// Interaction logic for DiffLensWindowControl.xaml
    /// </summary>
    public partial class DiffLensWindowControl : UserControl
    {
        private bool isUpdatingSettings = false;
        private List<GitCommit> recentCommits = new List<GitCommit>();

        public DiffLensWindowControl()
        {
            this.InitializeComponent();
            this.Loaded += DiffLensWindowControl_Loaded;
        }

        private async void DiffLensWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettingsAsync();
            await RefreshRepositoryInfoAsync();
        }

        #region Settings Management

        private async Task LoadSettingsAsync()
        {
            try
            {
                isUpdatingSettings = true;

                var configService = ConfigurationService.Instance;
                if (configService == null) return;

                // Load LLM Provider
                LLMProviderComboBox.SelectedIndex = configService.LLMProvider == LLMProvider.Bedrock ? 0 : 1;

                // Load basic settings
                ContextLinesTextBox.Text = configService.ContextLines.ToString();
                ExcludeDeletesCheckBox.IsChecked = configService.ExcludeDeletes;
                FileExtensionsTextBox.Text = configService.FileExtensions;

                // Load AWS settings
                AwsAccessKeyTextBox.Text = configService.AwsAccessKey;
                AwsSecretKeyPasswordBox.Password = configService.AwsSecretKey;
                
                // Select AWS region
                foreach (ComboBoxItem item in AwsRegionComboBox.Items)
                {
                    if (item.Tag?.ToString() == configService.AwsRegion)
                    {
                        AwsRegionComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Load model name
                ModelNameComboBox.Text = configService.ModelName;

                // Load prompts
                var languageService = LanguageService.Instance;
                SystemPromptTextBox.Text = !string.IsNullOrEmpty(configService.SystemPrompt) 
                    ? configService.SystemPrompt 
                    : languageService?.GetString("DefaultSystemPrompt") ?? "";
                    
                ReviewPerspectiveTextBox.Text = !string.IsNullOrEmpty(configService.ReviewPerspective) 
                    ? configService.ReviewPerspective 
                    : languageService?.GetString("DefaultReviewPerspective") ?? "";

                UpdateUIBasedOnProvider();
            }
            finally
            {
                isUpdatingSettings = false;
            }
        }

        private void UpdateUIBasedOnProvider()
        {
            var selectedProvider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            BedrockSettingsGroup.Visibility = selectedProvider == "Bedrock" ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Repository Management

        private async Task RefreshRepositoryInfoAsync()
        {
            try
            {
                var gitService = GitService.Instance;
                if (gitService == null) return;

                var solutionPath = await GetSolutionPathAsync();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    RepositoryPathText.Text = "Repository: No solution loaded";
                    CurrentBranchText.Text = "Branch: N/A";
                    CommitsComboBox.Items.Clear();
                    return;
                }

                var isRepo = await gitService.IsGitRepositoryAsync(solutionPath);
                if (!isRepo)
                {
                    RepositoryPathText.Text = "Repository: Not a Git repository";
                    CurrentBranchText.Text = "Branch: N/A";
                    CommitsComboBox.Items.Clear();
                    return;
                }

                var repositoryRoot = await gitService.GetRepositoryRootAsync(solutionPath);
                var currentBranch = await gitService.GetCurrentBranchAsync(repositoryRoot);
                recentCommits = await gitService.GetRecentCommitsAsync(repositoryRoot, 20);

                RepositoryPathText.Text = $"Repository: {System.IO.Path.GetFileName(repositoryRoot)}";
                CurrentBranchText.Text = $"Branch: {currentBranch}";

                // Update commits combobox
                CommitsComboBox.Items.Clear();
                foreach (var commit in recentCommits)
                {
                    CommitsComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = commit.ToString(),
                        Tag = commit.Hash
                    });
                }

                if (CommitsComboBox.Items.Count > 0)
                {
                    CommitsComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                RepositoryPathText.Text = $"Repository: Error - {ex.Message}";
                CurrentBranchText.Text = "Branch: Error";
            }
        }

        private async Task<string> GetSolutionPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.Solution?.FullName != null)
            {
                return System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            }

            return null;
        }

        #endregion

        #region Event Handlers

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshRepositoryInfoAsync();
        }

        private void LLMProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSettings) return;

            var selectedProvider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var provider = selectedProvider == "Bedrock" ? LLMProvider.Bedrock : LLMProvider.VSCodeLM;
            
            ConfigurationService.Instance.LLMProvider = provider;
            UpdateUIBasedOnProvider();
        }

        private void ContextLinesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSettings) return;

            if (int.TryParse(ContextLinesTextBox.Text, out int value) && value >= 0 && value <= 100)
            {
                ConfigurationService.Instance.ContextLines = value;
            }
        }

        private void ExcludeDeletesCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.ExcludeDeletes = ExcludeDeletesCheckBox.IsChecked ?? true;
        }

        private void FileExtensionsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.FileExtensions = FileExtensionsTextBox.Text;
        }

        private void AwsAccessKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.AwsAccessKey = AwsAccessKeyTextBox.Text;
        }

        private void AwsSecretKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.AwsSecretKey = AwsSecretKeyPasswordBox.Password;
        }

        private void AwsRegionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSettings) return;

            var selectedRegion = (AwsRegionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(selectedRegion))
            {
                ConfigurationService.Instance.AwsRegion = selectedRegion;
            }
        }

        private void ModelNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.ModelName = ModelNameComboBox.Text;
        }

        private void SystemPromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.SystemPrompt = SystemPromptTextBox.Text;
        }

        private void ReviewPerspectiveTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSettings) return;
            ConfigurationService.Instance.ReviewPerspective = ReviewPerspectiveTextBox.Text;
        }

        private void CommitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This event is handled when user selects a different commit for comparison
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                TestConnectionButton.Content = "Testing...";

                var reviewService = ReviewService.Instance;
                var configService = ConfigurationService.Instance;

                if (reviewService == null || configService == null)
                {
                    MessageBox.Show("Services not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var config = configService.GetReviewConfig();
                var isConnected = await reviewService.TestConnectionAsync(config);

                if (isConnected)
                {
                    MessageBox.Show("Connection test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Connection test failed. Please check your configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection test error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Test Connection";
            }
        }

        private async void PreviewDiffButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PreviewDiffButton.IsEnabled = false;
                PreviewDiffButton.Content = "Generating...";

                var gitService = GitService.Instance;
                var diffService = DiffService.Instance;
                var configService = ConfigurationService.Instance;

                if (gitService == null || diffService == null || configService == null)
                {
                    MessageBox.Show("Services not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var solutionPath = await GetSolutionPathAsync();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    MessageBox.Show("No solution loaded", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var isRepo = await gitService.IsGitRepositoryAsync(solutionPath);
                if (!isRepo)
                {
                    MessageBox.Show("Current solution is not in a Git repository", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var repositoryRoot = await gitService.GetRepositoryRootAsync(solutionPath);
                var selectedCommit = (CommitsComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var config = configService.GetReviewConfig();

                var previewContent = await diffService.GetDiffPreviewAsync(
                    repositoryRoot, 
                    selectedCommit,
                    config.ContextLines, 
                    config.ExcludeDeletes, 
                    config.FileExtensions
                );

                // Show preview in a new document
                await ShowPreviewAsync(previewContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PreviewDiffButton.IsEnabled = true;
                PreviewDiffButton.Content = "Preview Diff";
            }
        }

        private async void RunReviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunReviewButton.IsEnabled = false;
                RunReviewButton.Content = "Reviewing...";

                var gitService = GitService.Instance;
                var reviewService = ReviewService.Instance;
                var configService = ConfigurationService.Instance;

                if (gitService == null || reviewService == null || configService == null)
                {
                    MessageBox.Show("Services not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var solutionPath = await GetSolutionPathAsync();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    MessageBox.Show("No solution loaded", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var isRepo = await gitService.IsGitRepositoryAsync(solutionPath);
                if (!isRepo)
                {
                    MessageBox.Show("Current solution is not in a Git repository", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var config = configService.GetReviewConfig();
                var configErrors = reviewService.ValidateConfiguration(config);
                if (configErrors.Length > 0)
                {
                    var errorMessage = string.Join(", ", configErrors);
                    MessageBox.Show($"Configuration errors: {errorMessage}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var repositoryRoot = await gitService.GetRepositoryRootAsync(solutionPath);
                var selectedCommit = (CommitsComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                var diff = await gitService.GetDiffAsync(repositoryRoot, selectedCommit, config.ContextLines, config.ExcludeDeletes, config.FileExtensions);

                if (string.IsNullOrEmpty(diff))
                {
                    MessageBox.Show("No changes found to review", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var reviewResult = await reviewService.ReviewCodeAsync(diff, config);
                await reviewService.ShowReviewResultsAsync(reviewResult);

                MessageBox.Show("Code review completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during code review: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RunReviewButton.IsEnabled = true;
                RunReviewButton.Content = "Run Review";
            }
        }

        private async Task ShowPreviewAsync(string content)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    var doc = dte.Documents.Add("Text");
                    var textDoc = doc.Object() as EnvDTE.TextDocument;
                    if (textDoc != null)
                    {
                        var editPoint = textDoc.StartPoint.CreateEditPoint();
                        editPoint.Insert(content);
                        
                        // Note: Document.Name is read-only in newer VS versions
                        // The document will have a default name
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to display diff preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
