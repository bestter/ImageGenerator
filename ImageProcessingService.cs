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

            // Clean the base file name, strip any existing extension, and append .webp
            var safeBaseName = Path.GetFileName(baseFileName);
            var cleanFileName = Path.GetFileNameWithoutExtension(safeBaseName) + ".webp";
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

                // 🛡️ Sentinel: Prevent TOCTOU race condition and avoid blocking the thread pool.
                // Use EAFP (Easier to Ask for Forgiveness than Permission) pattern.
                try
                {
                    image.Save(fullPath, encoder);
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(historyFolder);
                    image.Save(fullPath, encoder);
                }
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
                // ⚡ Bolt Optimization: Enable asynchronous OS-level I/O to prevent Thread Pool starvation during async reads
                using var fs = new FileStream(webpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

                if (fs.Length == 0)
                {
                    throw new ArgumentException("File is empty.", nameof(webpFilePath));
                }

                // 🛡️ Sentinel: Prevent memory exhaustion (DoS) by enforcing a maximum file size limit before loading
                if (fs.Length > 20 * 1024 * 1024)
                {
                    throw new InvalidDataException("WebP file exceeds the maximum allowed size (20 MB).");
                }

                MemoryStream? memoryStream = null;

                try
                {
                    // Load WEBP using ImageSharp asynchronously from the stream
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(fs))
                    {
                        // ⚡ Bolt Optimization: Pre-allocate MemoryStream capacity based on image dimensions
                        // (Width * Height * 4 bytes for RGBA + 1024 bytes for BMP headers).
                        // This prevents excessive Large Object Heap (LOH) fragmentation caused by default buffer doubling
                        // when saving uncompressed image data.
                        int estimatedCapacity = (image.Width * image.Height * 4) + 1024;
                        memoryStream = new MemoryStream(estimatedCapacity);

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
                    memoryStream?.Dispose();
                }
            });
        }
    }
}