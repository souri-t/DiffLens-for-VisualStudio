using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using DiffLens.VisualStudio.Models;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for handling localization and language-specific resources
    /// </summary>
    public class LanguageService
    {
        public static LanguageService Instance { get; private set; }

        private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>();

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await Task.Run(() =>
            {
                Instance = new LanguageService();
                Instance.InitializeTranslations();
            });
        }

        /// <summary>
        /// Gets localized string for the current interface language
        /// </summary>
        public string GetString(string key)
        {
            var config = ConfigurationService.Instance;
            var language = config?.InterfaceLanguage ?? InterfaceLanguage.English;
            return GetString(key, language);
        }

        /// <summary>
        /// Gets localized string for a specific language
        /// </summary>
        public string GetString(string key, InterfaceLanguage language)
        {
            var langCode = language == InterfaceLanguage.Japanese ? "ja" : "en";
            
            if (translations.ContainsKey(langCode) && translations[langCode].ContainsKey(key))
            {
                return translations[langCode][key];
            }

            // Fallback to English if key not found
            if (language != InterfaceLanguage.English && 
                translations.ContainsKey("en") && 
                translations["en"].ContainsKey(key))
            {
                return translations["en"][key];
            }

            // Return key if no translation found
            return key;
        }

        /// <summary>
        /// Initializes translation dictionaries
        /// </summary>
        private void InitializeTranslations()
        {
            translations = new Dictionary<string, Dictionary<string, string>>();

            // English translations
            translations["en"] = new Dictionary<string, string>
            {
                // Commands
                {"ReviewCode", "Review Code with AI"},
                {"PreviewDiff", "Preview Git Diff"},
                {"OpenSettings", "Open DiffLens Settings"},
                
                // Tool Window
                {"ToolWindowTitle", "DiffLens"},
                {"GitRepositoryInfo", "Git Repository Information"},
                {"PromptInformation", "Prompt Information"},
                {"Settings", "Settings"},
                
                // Git Info
                {"Repository", "Repository:"},
                {"CurrentBranch", "Current Branch:"},
                {"RecentCommits", "Recent Commits:"},
                {"NoRepository", "No git repository found"},
                {"LoadingRepository", "Loading repository information..."},
                
                // Configuration
                {"LLMProvider", "LLM Provider:"},
                {"SystemPrompt", "System Prompt:"},
                {"ReviewPerspective", "Review Perspective:"},
                {"ContextLines", "Context Lines:"},
                {"ExcludeDeletes", "Exclude Deleted Files"},
                {"FileExtensions", "File Extensions:"},
                {"InterfaceLanguage", "Interface Language:"},
                
                // AWS Bedrock
                {"AWSAccessKey", "AWS Access Key:"},
                {"AWSSecretKey", "AWS Secret Key:"},
                {"AWSRegion", "AWS Region:"},
                {"ModelName", "Model Name:"},
                
                // Actions
                {"RunReview", "Run Code Review"},
                {"PreviewChanges", "Preview Changes"},
                {"SaveSettings", "Save Settings"},
                {"TestConnection", "Test Connection"},
                {"RefreshRepository", "Refresh Repository"},
                
                // Messages
                {"ReviewInProgress", "Code review in progress..."},
                {"ReviewComplete", "Code review completed successfully"},
                {"ReviewFailed", "Code review failed: {0}"},
                {"ConfigurationError", "Configuration error: {0}"},
                {"ConnectionSuccess", "Connection test successful"},
                {"ConnectionFailed", "Connection test failed"},
                {"SettingsSaved", "Settings saved successfully"},
                {"NoChangesFound", "No changes found to review"},
                {"InvalidConfiguration", "Invalid configuration. Please check your settings."},
                
                // Defaults
                {"DefaultSystemPrompt", "You are a senior software engineer conducting a code review. Please analyze the provided git diff and provide constructive feedback focusing on code quality, security, performance, and best practices."},
                {"DefaultReviewPerspective", "Focus on code quality, security vulnerabilities, performance issues, and adherence to best practices. Provide specific suggestions for improvement."}
            };

            // Japanese translations
            translations["ja"] = new Dictionary<string, string>
            {
                // Commands
                {"ReviewCode", "AIによるコードレビュー"},
                {"PreviewDiff", "Git差分をプレビュー"},
                {"OpenSettings", "DiffLens設定を開く"},
                
                // Tool Window
                {"ToolWindowTitle", "DiffLens"},
                {"GitRepositoryInfo", "Gitリポジトリ情報"},
                {"PromptInformation", "プロンプト情報"},
                {"Settings", "設定"},
                
                // Git Info
                {"Repository", "リポジトリ:"},
                {"CurrentBranch", "現在のブランチ:"},
                {"RecentCommits", "最近のコミット:"},
                {"NoRepository", "gitリポジトリが見つかりません"},
                {"LoadingRepository", "リポジトリ情報を読み込み中..."},
                
                // Configuration
                {"LLMProvider", "LLMプロバイダー:"},
                {"SystemPrompt", "システムプロンプト:"},
                {"ReviewPerspective", "レビュー観点:"},
                {"ContextLines", "コンテキスト行数:"},
                {"ExcludeDeletes", "削除ファイルを除外"},
                {"FileExtensions", "ファイル拡張子:"},
                {"InterfaceLanguage", "インターフェース言語:"},
                
                // AWS Bedrock
                {"AWSAccessKey", "AWSアクセスキー:"},
                {"AWSSecretKey", "AWSシークレットキー:"},
                {"AWSRegion", "AWSリージョン:"},
                {"ModelName", "モデル名:"},
                
                // Actions
                {"RunReview", "コードレビュー実行"},
                {"PreviewChanges", "変更をプレビュー"},
                {"SaveSettings", "設定を保存"},
                {"TestConnection", "接続テスト"},
                {"RefreshRepository", "リポジトリを更新"},
                
                // Messages
                {"ReviewInProgress", "コードレビューを実行中..."},
                {"ReviewComplete", "コードレビューが正常に完了しました"},
                {"ReviewFailed", "コードレビューが失敗しました: {0}"},
                {"ConfigurationError", "設定エラー: {0}"},
                {"ConnectionSuccess", "接続テストが成功しました"},
                {"ConnectionFailed", "接続テストが失敗しました"},
                {"SettingsSaved", "設定が正常に保存されました"},
                {"NoChangesFound", "レビューする変更が見つかりません"},
                {"InvalidConfiguration", "設定が無効です。設定を確認してください。"},
                
                // Defaults
                {"DefaultSystemPrompt", "あなたはシニアソフトウェアエンジニアとしてコードレビューを行います。提供されたgit差分を分析し、コード品質、セキュリティ、パフォーマンス、ベストプラクティスに焦点を当てて建設的なフィードバックを提供してください。"},
                {"DefaultReviewPerspective", "コード品質、セキュリティ脆弱性、パフォーマンス問題、ベストプラクティスの遵守に焦点を当てて、具体的な改善提案を提供してください。"}
            };
        }
    }
}
