using System;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Represents an AI image generation history entry stored in the database.
    /// </summary>
    public class GenerationHistoryModel
    {
        /// <summary>
        /// Gets or sets the unique primary key identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the file path where the generated image is saved.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt string used to generate the image.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the AI model.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional version of the AI model.
        /// </summary>
        public string? ModelVersion { get; set; }

        /// <summary>
        /// Gets or sets the optional raw JSON metadata or API response.
        /// </summary>
        public string? RawMetadata { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the generation history record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
