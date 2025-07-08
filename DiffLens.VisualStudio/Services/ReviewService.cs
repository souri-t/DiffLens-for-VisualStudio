using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DiffLens.VisualStudio.Models;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for handling AI code review using various LLM providers
    /// </summary>
    public class ReviewService
    {
        public static ReviewService? Instance { get; private set; }

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await Task.Run(() =>
            {
                Instance = new ReviewService();
            });
        }

        /// <summary>
        /// Performs code review using the configured LLM provider
        /// </summary>
        public async Task<ReviewResult> ReviewCodeAsync(string diff, ReviewConfig config)
        {
            try
            {
                switch (config.LLMProvider)
                {
                    case LLMProvider.Bedrock:
                        return await ReviewWithBedrockAsync(diff, config);
                    case LLMProvider.Copilot:
                        return await ReviewWithCopilotAsync(diff, config);
                    case LLMProvider.VSCodeLM:
                        // Note: VS Code LM API is not directly available in Visual Studio
                        // This would require integration with external services or APIs
                        throw new NotSupportedException("VS Code LM API is not available in Visual Studio. Please use AWS Bedrock or Copilot.");
                    default:
                        throw new ArgumentException($"Unknown LLM provider: {config.LLMProvider}");
                }
            }
            catch (Exception ex)
            {
                return new ReviewResult("Error", $"Failed to perform code review: {ex.Message}");
            }
        }

        /// <summary>
        /// Reviews code using AWS Bedrock
        /// </summary>
        private async Task<ReviewResult> ReviewWithBedrockAsync(string diff, ReviewConfig config)
        {
            try
            {
                // Create Bedrock client
                var client = new AmazonBedrockRuntimeClient(
                    config.AwsAccessKey,
                    config.AwsSecretKey,
                    Amazon.RegionEndpoint.GetBySystemName(config.AwsRegion)
                );

                var diffService = DiffService.Instance;
                var formattedDiff = diffService?.FormatDiffAsMarkdown(diff) ?? diff;

                var prompt = $@"{config.SystemPrompt}

Review Perspective: {config.ReviewPerspective}

Please review the following git diff (formatted in markdown for better readability):

{formattedDiff}

Please provide a detailed code review with specific suggestions for improvement.";

                // Prepare the request body for Claude
                var requestBody = new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 4000,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);

                var request = new InvokeModelRequest
                {
                    ModelId = config.ModelName,
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
                };

                var response = await client.InvokeModelAsync(request);

                // Parse the response
                var responseBody = Encoding.UTF8.GetString(response.Body.ToArray());
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseBody);

                var reviewText = responseJson?.content?[0]?.text?.ToString() ?? "No review content received.";

                return new ReviewResult(config.ModelName, reviewText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Bedrock API error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reviews code using Visual Studio Copilot
        /// </summary>
        private async Task<ReviewResult> ReviewWithCopilotAsync(string diff, ReviewConfig config)
        {
            try
            {
                var copilotService = CopilotService.Instance;
                if (copilotService == null)
                {
                    return new ReviewResult("Error", "Copilot service is not initialized");
                }

                // Check if Copilot is available
                if (!await copilotService.IsCopilotAvailableAsync())
                {
                    return new ReviewResult("Warning", 
                        "Visual Studio Copilot is not available. Please ensure GitHub Copilot extension is installed and enabled.\n\n" +
                        "Alternative: Switch to AWS Bedrock provider for automated code review.");
                }

                var diffService = DiffService.Instance;
                var formattedDiff = diffService?.FormatDiffAsMarkdown(diff) ?? diff;

                // Create a comprehensive prompt for Copilot
                var prompt = $@"{config.SystemPrompt}

Review Perspective: {config.ReviewPerspective}

Please review the following git diff:

{formattedDiff}

Please provide a detailed code review with specific suggestions for improvement.";

                var reviewText = await copilotService.RequestCodeReviewAsync(formattedDiff, config.ReviewPerspective);
                
                return new ReviewResult("Visual Studio Copilot", reviewText);
            }
            catch (Exception ex)
            {
                return new ReviewResult("Error", $"Failed to perform Copilot review: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows review results in a new document window
        /// </summary>
        public async Task ShowReviewResultsAsync(ReviewResult reviewResult)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var content = $@"# Code Review Results

**Model Used:** {reviewResult.ModelName}  
**Generated at:** {reviewResult.Timestamp:yyyy-MM-dd HH:mm:ss}

---

{reviewResult.Review}";

                // Create a new document with the review results
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    var doc = dte.Documents.Add("Text");
                    var textDoc = doc.Object() as EnvDTE.TextDocument;
                    if (textDoc != null)
                    {
                        var editPoint = textDoc.StartPoint.CreateEditPoint();
                        editPoint.Insert(content);
                        
                        // Set the document title
                        // Note: Document.Name is read-only in newer VS versions
                        // The document will have a default name
                    }
                }
            }
            catch (Exception ex)
            {
                var message = $"Failed to display review results: {ex.Message}";
                Microsoft.VisualStudio.Shell.VsShellUtilities.ShowMessageBox(
                    DiffLensPackage.Instance,
                    message,
                    "DiffLens Error",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Validates the review configuration
        /// </summary>
        public string[] ValidateConfiguration(ReviewConfig config)
        {
            return config.Validate();
        }

        /// <summary>
        /// Tests the connection to the configured LLM provider
        /// </summary>
        public async Task<bool> TestConnectionAsync(ReviewConfig config)
        {
            try
            {
                switch (config.LLMProvider)
                {
                    case LLMProvider.Bedrock:
                        return await TestBedrockConnectionAsync(config);
                    case LLMProvider.VSCodeLM:
                        return false; // Not supported in Visual Studio
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests AWS Bedrock connection
        /// </summary>
        private async Task<bool> TestBedrockConnectionAsync(ReviewConfig config)
        {
            try
            {
                var client = new AmazonBedrockRuntimeClient(
                    config.AwsAccessKey,
                    config.AwsSecretKey,
                    Amazon.RegionEndpoint.GetBySystemName(config.AwsRegion)
                );

                // Send a simple test request
                var testRequestBody = new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 10,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "Hello"
                        }
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(testRequestBody);

                var request = new InvokeModelRequest
                {
                    ModelId = config.ModelName,
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
                };

                var response = await client.InvokeModelAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }
    }
}
