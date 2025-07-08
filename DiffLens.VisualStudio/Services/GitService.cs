using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using DiffLens.VisualStudio.Models;

namespace DiffLens.VisualStudio.Services
{
    /// <summary>
    /// Service for Git operations
    /// </summary>
    public class GitService
    {
        public static GitService? Instance { get; private set; }

        public static async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            await Task.Run(() =>
            {
                Instance = new GitService();
            });
        }

        /// <summary>
        /// Checks if the current solution directory is a Git repository
        /// </summary>
        public async Task<bool> IsGitRepositoryAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    return false;

                var result = await RunGitCommandAsync(path, "rev-parse --git-dir");
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the Git repository root path
        /// </summary>
        public async Task<string?> GetRepositoryRootAsync(string path)
        {
            try
            {
                var result = await RunGitCommandAsync(path, "rev-parse --show-toplevel");
                if (result.Success)
                {
                    return result.Output.Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets recent commits from the repository
        /// </summary>
        public async Task<List<GitCommit>> GetRecentCommitsAsync(string repositoryPath, int maxCount = 20)
        {
            try
            {
                var command = $"log --oneline --pretty=format:\"%H|%s|%ai|%an|%ae\" -n {maxCount}";
                var result = await RunGitCommandAsync(repositoryPath, command);

                if (!result.Success)
                    return new List<GitCommit>();

                var commits = new List<GitCommit>();
                var lines = result.Output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        commits.Add(new GitCommit
                        {
                            Hash = parts[0],
                            Message = parts[1],
                            AuthorDate = DateTime.TryParse(parts[2], out var date) ? date : DateTime.MinValue,
                            AuthorName = parts[3],
                            AuthorEmail = parts[4]
                        });
                    }
                }

                return commits;
            }
            catch
            {
                return new List<GitCommit>();
            }
        }

        /// <summary>
        /// Gets the current branch name
        /// </summary>
        public async Task<string> GetCurrentBranchAsync(string repositoryPath)
        {
            try
            {
                var result = await RunGitCommandAsync(repositoryPath, "branch --show-current");
                return result.Success ? result.Output.Trim() : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Gets git diff between commits or from a specific commit to HEAD
        /// </summary>
        public async Task<string> GetDiffAsync(string repositoryPath, string? fromCommit = null, int contextLines = 50, bool excludeDeletes = true, string fileExtensions = "")
        {
            try
            {
                var command = new StringBuilder("diff");

                // Add context lines
                command.Append($" --unified={contextLines}");

                // Add file filter if specified
                if (!string.IsNullOrEmpty(fileExtensions))
                {
                    var extensions = ParseFileExtensions(fileExtensions);
                    foreach (var ext in extensions)
                    {
                        command.Append($" \"{ext}\"");
                    }
                }

                // Add commit range
                if (!string.IsNullOrEmpty(fromCommit))
                {
                    command.Append($" {fromCommit}..HEAD");
                }
                else
                {
                    command.Append(" HEAD~1..HEAD");
                }

                var result = await RunGitCommandAsync(repositoryPath, command.ToString());

                if (!result.Success)
                    return "";

                var diff = result.Output;

                // Filter out deleted files if requested
                if (excludeDeletes)
                {
                    diff = FilterDeletedFiles(diff);
                }

                return diff;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Parses file extensions filter string
        /// </summary>
        private List<string> ParseFileExtensions(string fileExtensions)
        {
            if (string.IsNullOrEmpty(fileExtensions))
                return new List<string>();

            return fileExtensions
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext))
                .ToList();
        }

        /// <summary>
        /// Filters out deleted files from git diff output
        /// </summary>
        private string FilterDeletedFiles(string diff)
        {
            var lines = diff.Split('\n');
            var result = new List<string>();
            bool skipCurrentFile = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("diff --git "))
                {
                    skipCurrentFile = false;
                }
                else if (line.StartsWith("deleted file mode"))
                {
                    skipCurrentFile = true;
                    continue;
                }

                if (!skipCurrentFile)
                {
                    result.Add(line);
                }
            }

            return string.Join("\n", result);
        }

        /// <summary>
        /// Runs a git command and returns the result
        /// </summary>
        private async Task<GitCommandResult> RunGitCommandAsync(string workingDirectory, string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit());

                    return new GitCommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new GitCommandResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
    }

    /// <summary>
    /// Result of a git command execution
    /// </summary>
    internal class GitCommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public int ExitCode { get; set; }
    }
}
