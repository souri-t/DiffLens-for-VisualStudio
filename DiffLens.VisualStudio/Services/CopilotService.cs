using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Text;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for interacting with Visual Studio Copilot
    /// </summary>
    public class CopilotService
    {
        public static CopilotService Instance { get; private set; }

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await Task.Run(() =>
            {
                Instance = new CopilotService();
            });
        }

        /// <summary>
        /// Request code review from Visual Studio Copilot
        /// </summary>
        /// <param name="code">Code to review</param>
        /// <param name="prompt">Review prompt</param>
        /// <returns>Review result</returns>
        public async Task<string> RequestCodeReviewAsync(string code, string prompt)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // Get the command service to execute Copilot commands
                var commandService = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
                
                if (commandService == null)
                {
                    return "Visual Studio Copilot service is not available.";
                }

                // Create a formatted prompt for Copilot
                var copilotPrompt = CreateCopilotPrompt(code, prompt);
                
                // Try to use Copilot Chat if available
                var result = await TryCopilotChatAsync(copilotPrompt);
                
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }

                // Fallback message if Copilot is not available
                return GenerateFallbackReview(code, prompt);
            }
            catch (Exception ex)
            {
                return $"Error requesting Copilot review: {ex.Message}";
            }
        }

        /// <summary>
        /// Create a formatted prompt for Copilot
        /// </summary>
        private string CreateCopilotPrompt(string code, string prompt)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Please review the following code:");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine($"Review perspective: {prompt}");
            sb.AppendLine();
            sb.AppendLine("Please provide a detailed code review focusing on:");
            sb.AppendLine("- Code quality and best practices");
            sb.AppendLine("- Potential bugs or issues");
            sb.AppendLine("- Performance considerations");
            sb.AppendLine("- Security concerns");
            sb.AppendLine("- Maintainability and readability");
            sb.AppendLine("- Suggestions for improvement");
            
            return sb.ToString();
        }

        /// <summary>
        /// Try to use Copilot Chat for code review
        /// </summary>
        private async Task<string> TryCopilotChatAsync(string prompt)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Try to get Copilot Chat service
                // Note: This is a simplified implementation
                // The actual Copilot integration would require more specific VS APIs
                
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.Commands != null)
                {
                    // Try to execute Copilot Chat command if available
                    try
                    {
                        // Check if Copilot Chat command exists
                        var copilotCommand = dte.Commands.Item("GitHub.Copilot.OpenChat");
                        if (copilotCommand != null && copilotCommand.IsAvailable)
                        {
                            // Open Copilot Chat and set the prompt
                            dte.Commands.Raise(copilotCommand.Guid, copilotCommand.ID, prompt, null);
                            return "Copilot Chat opened with your review request. Please check the Copilot Chat window for the response.";
                        }
                    }
                    catch
                    {
                        // Copilot command not available
                    }

                    // Try alternative Copilot commands
                    try
                    {
                        var commands = dte.Commands;
                        for (int i = 1; i <= commands.Count; i++)
                        {
                            var cmd = commands.Item(i);
                            if (cmd.Name.Contains("Copilot") || cmd.Name.Contains("GitHub"))
                            {
                                // Found a potential Copilot command
                                return $"Visual Studio Copilot detected. Please use Copilot Chat manually with this prompt:\n\n{prompt}";
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors while searching for commands
                    }
                }

                return null; // Copilot not available
            }
            catch
            {
                return null; // Error occurred
            }
        }

        /// <summary>
        /// Generate a fallback review when Copilot is not available
        /// </summary>
        private string GenerateFallbackReview(string code, string prompt)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Code Review (Fallback Analysis)");
            sb.AppendLine();
            sb.AppendLine("Visual Studio Copilot is not available. Here's a basic analysis:");
            sb.AppendLine();
            
            // Basic code analysis
            var lines = code.Split('\n');
            sb.AppendLine($"**Code Statistics:**");
            sb.AppendLine($"- Lines of code: {lines.Length}");
            sb.AppendLine($"- Characters: {code.Length}");
            sb.AppendLine();

            // Simple heuristics
            sb.AppendLine("**Quick Analysis:**");
            
            if (code.Contains("TODO") || code.Contains("FIXME"))
            {
                sb.AppendLine("- ⚠️ Found TODO/FIXME comments - consider addressing these");
            }
            
            if (code.Contains("catch") && !code.Contains("throw"))
            {
                sb.AppendLine("- ⚠️ Exception handling detected - ensure proper error handling");
            }
            
            if (code.Contains("async") && !code.Contains("await"))
            {
                sb.AppendLine("- ⚠️ Async method without await - verify this is intentional");
            }

            if (code.Length > 10000)
            {
                sb.AppendLine("- ⚠️ Large code block - consider breaking into smaller methods");
            }

            sb.AppendLine();
            sb.AppendLine("**Recommendations:**");
            sb.AppendLine("- Install GitHub Copilot extension for detailed AI-powered code reviews");
            sb.AppendLine("- Use Visual Studio's built-in code analysis tools");
            sb.AppendLine("- Consider peer review for complex changes");
            sb.AppendLine();
            sb.AppendLine($"**Review Perspective Applied:** {prompt}");

            return sb.ToString();
        }

        /// <summary>
        /// Check if Copilot is available in Visual Studio
        /// </summary>
        public async Task<bool> IsCopilotAvailableAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.Commands != null)
                {
                    try
                    {
                        // Check for GitHub Copilot extension
                        var copilotCommand = dte.Commands.Item("GitHub.Copilot.OpenChat");
                        return copilotCommand != null && copilotCommand.IsAvailable;
                    }
                    catch
                    {
                        return false;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get available AI providers
        /// </summary>
        public async Task<string[]> GetAvailableProvidersAsync()
        {
            var providers = new System.Collections.Generic.List<string>();
            
            // Always include Bedrock
            providers.Add("AWS Bedrock");
            
            // Check if Copilot is available
            if (await IsCopilotAvailableAsync())
            {
                providers.Add("Visual Studio Copilot");
            }
            
            return providers.ToArray();
        }
    }
}
