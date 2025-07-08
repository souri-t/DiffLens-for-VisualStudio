using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for handling diff operations and formatting
    /// </summary>
    public class DiffService
    {
        public static DiffService Instance { get; private set; }

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await Task.Run(() =>
            {
                Instance = new DiffService();
            });
        }

        /// <summary>
        /// Formats git diff output as Markdown for better readability
        /// </summary>
        public string FormatDiffAsMarkdown(string diff)
        {
            if (string.IsNullOrEmpty(diff))
                return "No changes detected.";

            var lines = diff.Split('\n');
            var result = new StringBuilder();
            string currentFile = "";
            var fileContent = new StringBuilder();

            foreach (var line in lines)
            {
                // Check for file header patterns
                if (line.StartsWith("diff --git "))
                {
                    // Process previous file if exists
                    if (!string.IsNullOrEmpty(currentFile) && fileContent.Length > 0)
                    {
                        ProcessFileContent(result, currentFile, fileContent.ToString());
                        fileContent.Clear();
                    }

                    // Extract file paths from "diff --git a/path b/path"
                    var parts = line.Split(' ');
                    if (parts.Length >= 4)
                    {
                        currentFile = parts[3].Substring(2); // Remove "b/" prefix
                    }
                    else
                    {
                        currentFile = "Unknown file";
                    }
                    continue;
                }

                // Skip git metadata lines but keep tracking file headers
                if (line.StartsWith("index ") ||
                    line.StartsWith("--- ") ||
                    line.StartsWith("+++ "))
                {
                    continue;
                }

                // Add content lines to current file
                if (!string.IsNullOrEmpty(currentFile))
                {
                    fileContent.AppendLine(line);
                }
            }

            // Process the last file
            if (!string.IsNullOrEmpty(currentFile) && fileContent.Length > 0)
            {
                ProcessFileContent(result, currentFile, fileContent.ToString());
            }

            // If no files were processed, return original diff in a code block
            if (result.Length == 0)
            {
                return $"```diff\n{diff}\n```";
            }

            return result.ToString();
        }

        /// <summary>
        /// Processes file content and adds it to the result
        /// </summary>
        private void ProcessFileContent(StringBuilder result, string fileName, string content)
        {
            result.AppendLine($"## {fileName}");
            result.AppendLine();
            result.AppendLine("```diff");
            result.AppendLine(content.TrimEnd());
            result.AppendLine("```");
            result.AppendLine();
        }

        /// <summary>
        /// Gets a preview of the diff content
        /// </summary>
        public async Task<string> GetDiffPreviewAsync(string repositoryPath, string commitHash, int contextLines, bool excludeDeletes, string fileExtensions)
        {
            try
            {
                var gitService = GitService.Instance;
                if (gitService == null)
                    return "Git service not available.";

                var diff = await gitService.GetDiffAsync(repositoryPath, commitHash, contextLines, excludeDeletes, fileExtensions);

                if (string.IsNullOrEmpty(diff))
                    return "No changes detected in the specified commit or range.";

                var shortHash = commitHash?.Length > 8 ? commitHash.Substring(0, 8) : commitHash ?? "HEAD~1";
                var filterInfo = !string.IsNullOrEmpty(fileExtensions) ? $"\nFile Extensions Filter: {fileExtensions}" : "";

                var previewContent = $@"# Git Diff Preview

**Comparison:** Current HEAD vs Commit {shortHash}  
**Context Lines (git diff -U{contextLines}):** {contextLines}  
**Options:** {(excludeDeletes ? "Exclude deleted files" : "Include all changes")}{filterInfo}  
**Generated at:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}

---

```diff
{diff}
```";

                return previewContent;
            }
            catch (Exception ex)
            {
                return $"Error generating diff preview: {ex.Message}";
            }
        }

        /// <summary>
        /// Validates file extensions format
        /// </summary>
        public bool ValidateFileExtensions(string fileExtensions)
        {
            if (string.IsNullOrEmpty(fileExtensions))
                return true;

            try
            {
                var extensions = fileExtensions
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim())
                    .Where(ext => !string.IsNullOrEmpty(ext));

                // Basic validation - all extensions should start with * or contain valid characters
                return extensions.All(ext => 
                    ext.StartsWith("*") || 
                    ext.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '*' || c == '/' || c == '\\'));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets statistics about the diff
        /// </summary>
        public DiffStatistics GetDiffStatistics(string diff)
        {
            if (string.IsNullOrEmpty(diff))
                return new DiffStatistics();

            var lines = diff.Split('\n');
            var stats = new DiffStatistics();

            foreach (var line in lines)
            {
                if (line.StartsWith("diff --git "))
                {
                    stats.FilesChanged++;
                }
                else if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    stats.LinesAdded++;
                }
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                {
                    stats.LinesRemoved++;
                }
            }

            return stats;
        }
    }

    /// <summary>
    /// Statistics about a diff
    /// </summary>
    public class DiffStatistics
    {
        public int FilesChanged { get; set; }
        public int LinesAdded { get; set; }
        public int LinesRemoved { get; set; }

        public int TotalChanges => LinesAdded + LinesRemoved;

        public override string ToString()
        {
            return $"{FilesChanged} files changed, {LinesAdded} insertions(+), {LinesRemoved} deletions(-)";
        }
    }
}
