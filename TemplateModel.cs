using System;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Represents an AI prompt template stored in the database.
    /// </summary>
    public class TemplateModel
    {
        /// <summary>
        /// Gets or sets the unique primary key identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the unique, case-insensitive key identifier for the template.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actual template text / prompt value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional group category of the template.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets optional comma-separated tags associated with the template.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets the total number of times this template has been utilized.
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Gets or sets the last date and time this template was utilized (in UTC).
        /// </summary>
        public DateTime? LastUsed { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the template record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the template record was last modified.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
