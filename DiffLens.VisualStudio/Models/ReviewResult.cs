namespace DiffLens.VisualStudio.Models
{
    /// <summary>
    /// Result of AI code review
    /// </summary>
    public class ReviewResult
    {
        public string ModelName { get; set; } = string.Empty;
        public string Review { get; set; } = string.Empty;
        public System.DateTime Timestamp { get; set; } = System.DateTime.Now;

        public ReviewResult()
        {
        }

        public ReviewResult(string modelName, string review)
        {
            ModelName = modelName;
            Review = review;
        }
    }
}
