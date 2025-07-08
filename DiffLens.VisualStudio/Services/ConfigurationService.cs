using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using DiffLens.VisualStudio.Models;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for managing DiffLens configuration
    /// </summary>
    public class ConfigurationService
    {
        public static ConfigurationService? Instance { get; private set; }

        private const string CollectionName = "DiffLens";
        private WritableSettingsStore? settingsStore;

        #region Properties

        public LLMProvider LLMProvider
        {
            get => GetEnumSetting(nameof(LLMProvider), Models.LLMProvider.Copilot);
            set => SetSetting(nameof(LLMProvider), value.ToString());
        }

        public string SystemPrompt
        {
            get => GetStringSetting(nameof(SystemPrompt), "You are a senior software engineer conducting a code review. Please analyze the provided git diff and provide constructive feedback focusing on code quality, security, performance, and best practices.");
            set => SetSetting(nameof(SystemPrompt), value);
        }

        public string ReviewPerspective
        {
            get => GetStringSetting(nameof(ReviewPerspective), "Focus on code quality, security vulnerabilities, performance issues, and adherence to best practices. Provide specific suggestions for improvement.");
            set => SetSetting(nameof(ReviewPerspective), value);
        }

        public string AwsAccessKey
        {
            get => GetStringSetting(nameof(AwsAccessKey), "");
            set => SetSetting(nameof(AwsAccessKey), value);
        }

        public string AwsSecretKey
        {
            get => GetStringSetting(nameof(AwsSecretKey), "");
            set => SetSetting(nameof(AwsSecretKey), value);
        }

        public string AwsRegion
        {
            get => GetStringSetting(nameof(AwsRegion), "us-east-1");
            set => SetSetting(nameof(AwsRegion), value);
        }

        public string ModelName
        {
            get => GetStringSetting(nameof(ModelName), "anthropic.claude-3-5-sonnet-20241022-v2:0");
            set => SetSetting(nameof(ModelName), value);
        }

        public int ContextLines
        {
            get => GetIntSetting(nameof(ContextLines), 50);
            set => SetSetting(nameof(ContextLines), value);
        }

        public bool ExcludeDeletes
        {
            get => GetBoolSetting(nameof(ExcludeDeletes), true);
            set => SetSetting(nameof(ExcludeDeletes), value);
        }

        public string FileExtensions
        {
            get => GetStringSetting(nameof(FileExtensions), "");
            set => SetSetting(nameof(FileExtensions), value);
        }

        public InterfaceLanguage InterfaceLanguage
        {
            get => GetEnumSetting(nameof(InterfaceLanguage), Models.InterfaceLanguage.English);
            set => SetSetting(nameof(InterfaceLanguage), value.ToString());
        }

        #endregion

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var settingsManager = new ShellSettingsManager(serviceProvider as System.IServiceProvider);
                var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                Instance = new ConfigurationService();
                Instance.settingsStore = settingsStore;

                // Ensure collection exists
                if (!settingsStore.CollectionExists(CollectionName))
                {
                    settingsStore.CreateCollection(CollectionName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ConfigurationService: {ex.Message}");
                // Create a basic instance even if settings store fails
                Instance = new ConfigurationService();
            }
        }

        /// <summary>
        /// Gets current configuration as ReviewConfig object
        /// </summary>
        public ReviewConfig GetReviewConfig()
        {
            return new ReviewConfig
            {
                LLMProvider = this.LLMProvider,
                SystemPrompt = this.SystemPrompt,
                ReviewPerspective = this.ReviewPerspective,
                AwsAccessKey = this.AwsAccessKey,
                AwsSecretKey = this.AwsSecretKey,
                AwsRegion = this.AwsRegion,
                ModelName = this.ModelName,
                ContextLines = this.ContextLines,
                ExcludeDeletes = this.ExcludeDeletes,
                FileExtensions = this.FileExtensions,
                InterfaceLanguage = this.InterfaceLanguage
            };
        }

        #region Private Methods

        private string GetStringSetting(string name, string defaultValue)
        {
            try
            {
                return settingsStore?.GetString(CollectionName, name, defaultValue) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int GetIntSetting(string name, int defaultValue)
        {
            try
            {
                return settingsStore?.GetInt32(CollectionName, name, defaultValue) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private bool GetBoolSetting(string name, bool defaultValue)
        {
            try
            {
                return settingsStore?.GetBoolean(CollectionName, name, defaultValue) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private T GetEnumSetting<T>(string name, T defaultValue) where T : struct, Enum
        {
            try
            {
                var stringValue = GetStringSetting(name, defaultValue.ToString());
                if (Enum.TryParse<T>(stringValue, out T result))
                {
                    return result;
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private void SetSetting(string name, object value)
        {
            try
            {
                if (settingsStore == null) return;

                switch (value)
                {
                    case string stringValue:
                        settingsStore.SetString(CollectionName, name, stringValue);
                        break;
                    case int intValue:
                        settingsStore.SetInt32(CollectionName, name, intValue);
                        break;
                    case bool boolValue:
                        settingsStore.SetBoolean(CollectionName, name, boolValue);
                        break;
                    default:
                        settingsStore.SetString(CollectionName, name, value?.ToString() ?? "");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting configuration: {ex.Message}");
            }
        }

        #endregion
    }
}
