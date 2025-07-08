using System;

namespace DiffLens.VisualStudio.Models
{
    /// <summary>
    /// Git commit information
    /// </summary>
    public class GitCommit
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime AuthorDate { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;

        public GitCommit()
        {
        }

        public GitCommit(string hash, string message, DateTime authorDate, string authorName, string authorEmail)
        {
            Hash = hash;
            Message = message;
            AuthorDate = authorDate;
            AuthorName = authorName;
            AuthorEmail = authorEmail;
        }

        /// <summary>
        /// Returns short hash (first 8 characters)
        /// </summary>
        public string ShortHash => Hash?.Length > 8 ? Hash.Substring(0, 8) : Hash ?? "";

        /// <summary>
        /// Returns first line of commit message
        /// </summary>
        public string ShortMessage
        {
            get
            {
                if (string.IsNullOrEmpty(Message))
                    return "";

                var lines = Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Length > 0 ? lines[0] : "";
            }
        }

        public override string ToString()
        {
            return $"{ShortHash} - {ShortMessage}";
        }
    }
}
