using System;

namespace DiffLens.VisualStudio.Models
{
    /// <summary>
    /// Configuration for code review
    /// </summary>
    public class ReviewConfig
    {
        public string SystemPrompt { get; set; } = "You are a senior software engineer conducting a code review. Please analyze the provided git diff and provide constructive feedback focusing on code quality, security, performance, and best practices.";
        public string ReviewPerspective { get; set; } = "Focus on code quality, security vulnerabilities, performance issues, and adherence to best practices. Provide specific suggestions for improvement.";
        public int ContextLines { get; set; } = 50;
        public bool ExcludeDeletes { get; set; } = true;
        public LLMProvider LLMProvider { get; set; } = LLMProvider.Bedrock;
        public string AwsAccessKey { get; set; } = "";
        public string AwsSecretKey { get; set; } = "";
        public string AwsRegion { get; set; } = "us-east-1";
        public string ModelName { get; set; } = "anthropic.claude-3-5-sonnet-20241022-v2:0";
        public string FileExtensions { get; set; } = "";
        public InterfaceLanguage InterfaceLanguage { get; set; } = InterfaceLanguage.English;

        /// <summary>
        /// Validates the configuration and returns validation errors
        /// </summary>
        public string[] Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(SystemPrompt))
                errors.Add("System Prompt is required");

            if (string.IsNullOrEmpty(ReviewPerspective))
                errors.Add("Review Perspective is required");

            if (LLMProvider == LLMProvider.Bedrock)
            {
                if (string.IsNullOrEmpty(AwsAccessKey))
                    errors.Add("AWS Access Key is required for Bedrock");

                if (string.IsNullOrEmpty(AwsSecretKey))
                    errors.Add("AWS Secret Key is required for Bedrock");
            }

            return errors.ToArray();
        }
    }

    /// <summary>
    /// LLM Provider options
    /// </summary>
    public enum LLMProvider
    {
        Bedrock,
        VSCodeLM,
        Copilot
    }

    /// <summary>
    /// Interface language options
    /// </summary>
    public enum InterfaceLanguage
    {
        English,
        Japanese
    }
}
