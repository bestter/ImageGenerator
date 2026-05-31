using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Service responsible for image manipulation, format conversions, and filesystem operations.
    /// Handles asynchronous file operations and WEBP compression to optimize storage.
    /// </summary>
    public class ImageProcessingService
    {
        /// <summary>
        /// Loads an image from a byte array (PNG/JPEG), encodes it to WEBP format at 80% quality, 
        /// and saves it to a dedicated history subfolder in LocalApplicationData.
        /// Optionally embeds AI generation metadata into the WebP file.
        /// </summary>
        /// <param name="sourceImageBytes">Raw bytes of the source image.</param>
        /// <param name="baseFileName">Target base filename without extension.</param>
        /// <param name="metadata">Optional generation metadata to embed into the WebP image.</param>
        /// <returns>The absolute path of the saved `.webp` file.</returns>
        /// <exception cref="ArgumentException">Thrown when source bytes are empty or base file name is invalid.</exception>
        public async Task<string> SaveImageAsWebpAsync(byte[] sourceImageBytes, string baseFileName, ImageGenerationMetadata? metadata = null)
        {
            if (sourceImageBytes == null || sourceImageBytes.Length == 0)
            {
                throw new ArgumentException("Source image bytes cannot be null or empty.", nameof(sourceImageBytes));
            }

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                throw new ArgumentException("Base file name cannot be null or whitespace.", nameof(baseFileName));
            }

            // Create target history subfolder in local app data: MyApp/HistoryImages
            var historyFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp",
                "HistoryImages"
            );
            Directory.CreateDirectory(historyFolder);

            // Clean the base file name, strip any existing extension, and append .webp
            var cleanFileName = Path.GetFileNameWithoutExtension(baseFileName) + ".webp";
            var fullPath = Path.Combine(historyFolder, cleanFileName);

            // Offload CPU-heavy image loading, encoding, and IO-heavy saving to a background thread to prevent UI freezing
            await Task.Run(() =>
            {
                // Fully qualified name to prevent any ambiguity with System.Drawing.Image in WinForms
                using var image = SixLabors.ImageSharp.Image.Load(sourceImageBytes);

                if (metadata != null)
                {
                    ImageMetadataEmbedder.ApplyMetadata(image, metadata);
                }

                var encoder = new WebpEncoder
                {
                    Quality = 80
                };

                image.Save(fullPath, encoder);
            });

            return fullPath;
        }

        /// <summary>
        /// Loads a WEBP image from disk using ImageSharp, converts it to a standard BMP stream, 
        /// and returns a GDI+ compatible System.Drawing.Image suitable for WinForms PictureBox.
        /// Properly clones the bitmap to prevent GDI+ dependency on the underlying memory stream.
        /// </summary>
        /// <param name="webpFilePath">The absolute path to the WebP file on disk.</param>
        /// <returns>A System.Drawing.Image instance representing the WebP file.</returns>
        /// <exception cref="ArgumentException">Thrown when file path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public async Task<System.Drawing.Image> LoadWebpForWinFormsAsync(string webpFilePath)
        {
            if (string.IsNullOrWhiteSpace(webpFilePath))
            {
                throw new ArgumentException("WebP file path cannot be null or whitespace.", nameof(webpFilePath));
            }

            // Perform image loading and conversion on a background thread
            return await Task.Run(async () =>
            {
                // 🛡️ Sentinel: Prevent TOCTOU race condition and handle file existence securely
                using var fs = new FileStream(webpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (fs.Length == 0)
                {
                    throw new ArgumentException("File is empty.", nameof(webpFilePath));
                }

                var memoryStream = new MemoryStream();

                try
                {
                    // Load WEBP using ImageSharp asynchronously from the stream
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(fs))
                    {
                        // Encode to BMP format (native and extremely fast for WinForms/GDI+)
                        var bmpEncoder = new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder();
                        await image.SaveAsync(memoryStream, bmpEncoder);
                    }

                    memoryStream.Position = 0;

                    // CRITICAL WinForms/GDI+ detail: A Bitmap constructed from a stream requires
                    // the stream to remain open for the bitmap's lifetime.
                    // Cloning the bitmap decouples it from the stream so we can safely dispose of it.
                    using (var tempBitmap = new System.Drawing.Bitmap(memoryStream))
                    {
                        return new System.Drawing.Bitmap(tempBitmap);
                    }
                }
                finally
                {
                    memoryStream.Dispose();
                }
            });
        }
    }
}