using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public static async Task InitializeAsync()
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
                switch (config.LLMProvider.ToLower())
                {
                    case "aws bedrock":
                        return await ReviewWithBedrockAsync(diff, config);
                    case "copilot":
                        return await ReviewWithCopilotAsync(diff, config);
                    default:
                        return new ReviewResult("Error", $"Unsupported LLM provider: {config.LLMProvider}");
                }
            }
            catch (Exception ex)
            {
                return new ReviewResult("Error", $"Code review failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Reviews code using AWS Bedrock
        /// </summary>
        private async Task<ReviewResult> ReviewWithBedrockAsync(string diff, ReviewConfig config)
        {
            try
            {
                using var client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.GetBySystemName(config.AwsRegion));

                var diffService = DiffService.Instance;
                var formattedDiff = diffService?.FormatDiffAsMarkdown(diff) ?? diff;

                // Create a comprehensive prompt for the AI
                var prompt = $@"{config.SystemPrompt}

Review Perspective: {config.ReviewPerspective}

Please review the following git diff:

{formattedDiff}

Please provide a detailed code review with specific suggestions for improvement.";

                var request = new InvokeModelRequest
                {
                    ModelId = config.ModelName,
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        anthropic_version = "bedrock-2023-05-31",
                        max_tokens = 4000,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        }
                    })))
                };

                var response = await client.InvokeModelAsync(request);
                
                using var reader = new StreamReader(response.Body);
                var responseBody = await reader.ReadToEndAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                
                var reviewText = result?.content?[0]?.text?.ToString() ?? "No review generated";
                
                return new ReviewResult(config.ModelName, reviewText);
            }
            catch (Exception ex)
            {
                return new ReviewResult("Error", $"AWS Bedrock review failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Reviews code using Visual Studio Copilot (disabled in .NET Core version)
        /// </summary>
        private async Task<ReviewResult> ReviewWithCopilotAsync(string diff, ReviewConfig config)
        {
            await Task.CompletedTask;
            return new ReviewResult("Warning", 
                "Visual Studio Copilot is not available in .NET Core version. " +
                "Please use AWS Bedrock provider for automated code review.");
        }

        /// <summary>
        /// Shows review results in a new document window
        /// </summary>
        public async Task ShowReviewResultsAsync(ReviewResult reviewResult)
        {
            await Task.CompletedTask;

            try
            {
                var content = $@"# Code Review Results

**Model Used:** {reviewResult.ModelName}  
**Generated at:** {reviewResult.Timestamp:yyyy-MM-dd HH:mm:ss}

---

{reviewResult.Review}";

                // In .NET Core version, output to console instead of creating VS document
                Console.WriteLine("=== Code Review Results ===");
                Console.WriteLine(content);
                Console.WriteLine("============================");
            }
            catch (Exception ex)
            {
                var message = $"Failed to display review results: {ex.Message}";
                Console.WriteLine($"Error: {message}");
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
                switch (config.LLMProvider.ToLower())
                {
                    case "aws bedrock":
                        return await TestBedrockConnectionAsync(config);
                    case "copilot":
                        return false; // Not available in .NET Core version
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
        /// Tests the connection to AWS Bedrock
        /// </summary>
        private async Task<bool> TestBedrockConnectionAsync(ReviewConfig config)
        {
            try
            {
                using var client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.GetBySystemName(config.AwsRegion));

                var request = new InvokeModelRequest
                {
                    ModelId = config.ModelName,
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        anthropic_version = "bedrock-2023-05-31",
                        max_tokens = 100,
                        messages = new[]
                        {
                            new { role = "user", content = "Hello" }
                        }
                    })))
                };

                var response = await client.InvokeModelAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets supported AWS regions for Bedrock
        /// </summary>
        public string[] GetSupportedAwsRegions()
        {
            return new[]
            {
                "us-east-1",
                "us-west-2",
                "eu-west-1",
                "ap-southeast-1",
                "ap-northeast-1"
            };
        }

        /// <summary>
        /// Gets supported model names for the specified provider
        /// </summary>
        public string[] GetSupportedModels(string provider)
        {
            switch (provider.ToLower())
            {
                case "aws bedrock":
                    return new[]
                    {
                        "anthropic.claude-3-sonnet-20240229-v1:0",
                        "anthropic.claude-3-haiku-20240307-v1:0",
                        "anthropic.claude-v2:1",
                        "anthropic.claude-v2",
                        "anthropic.claude-instant-v1"
                    };
                case "copilot":
                    return new[] { "GitHub Copilot" };
                default:
                    return new string[0];
            }
        }
    }
}
