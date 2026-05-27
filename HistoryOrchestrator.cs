using System;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Coordinates the post-generation workflow: converting the image to WEBP, saving it locally, 
    /// and logging the details into the SQLite generation history database.
    /// </summary>
    public class HistoryOrchestrator
    {
        private readonly ImageProcessingService _imageProcessingService;
        private readonly GenerationHistoryRepository _historyRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryOrchestrator"/> class.
        /// </summary>
        /// <param name="imageProcessingService">The image processing service.</param>
        /// <param name="historyRepository">The generation history repository.</param>
        /// <exception cref="ArgumentNullException">Thrown when a dependency is null.</exception>
        public HistoryOrchestrator(
            ImageProcessingService imageProcessingService, 
            GenerationHistoryRepository historyRepository)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        }

        /// <summary>
        /// Converts the generated image to a highly compressed WEBP file, writes it to disk, 
        /// and records the generation context in the SQLite history database.
        /// </summary>
        /// <param name="imageBytes">The raw bytes of the generated image (PNG/JPEG).</param>
        /// <param name="prompt">The prompt used for generation.</param>
        /// <param name="modelName">The name of the AI model used.</param>
        /// <param name="modelVersion">The optional version of the AI model.</param>
        /// <param name="rawMetadata">Optional JSON metadata representing the API response or parameters.</param>
        /// <returns>The newly logged and populated GenerationHistoryModel record containing the SQLite generated Id.</returns>
        /// <exception cref="ArgumentException">Thrown when critical parameters are missing.</exception>
        public async Task<GenerationHistoryModel> LogGenerationAsync(
            byte[] imageBytes,
            string prompt,
            string modelName,
            string? modelVersion = null,
            string? rawMetadata = null)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image bytes cannot be null or empty.", nameof(imageBytes));
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or whitespace.", nameof(prompt));
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be null or whitespace.", nameof(modelName));
            }

            // Generate a unique base filename incorporating a timestamp and unique segment
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var baseFileName = $"gen_{timestamp}_{uniqueId}";

            // 1. Construct standard AI metadata to embed in the WebP file
            var friendlyGenerator = ImageMetadataEmbedder.GetFriendlyGeneratorName(modelName);
            var imageMetadata = new ImageGenerationMetadata(
                Generator: friendlyGenerator,
                Prompt: prompt,
                ModelId: modelName,
                GeneratedAtUtc: DateTime.UtcNow,
                Resolution: null,
                AspectRatio: null,
                AppCreator: ImageMetadataEmbedder.AppNameVersion
            );

            // 2. Convert PNG/JPEG bytes to WEBP format, embed EXIF/XMP metadata, and save on disk
            var savedPath = await _imageProcessingService.SaveImageAsWebpAsync(imageBytes, baseFileName, imageMetadata);

            // 3. Build the model object
            var historyRecord = new GenerationHistoryModel
            {
                ImagePath = savedPath,
                Prompt = prompt.Trim(),
                ModelName = modelName.Trim(),
                ModelVersion = modelVersion?.Trim(),
                RawMetadata = rawMetadata?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // 4. Persist the record in SQLite using the repository from Step 1
            var recordId = await _historyRepository.InsertAsync(historyRecord);
            historyRecord.Id = recordId;

            return historyRecord;
        }
    }
}
