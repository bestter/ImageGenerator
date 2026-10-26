using ImageMagick;
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
        private readonly string _historyFolder;

        /// <summary>
        /// Creates the service. When <paramref name="historyFolder"/> is omitted, images are saved
        /// under LocalApplicationData/ImageGeneratorApp/HistoryImages.
        /// </summary>
        /// <param name="historyFolder">Optional override used by tests to isolate the filesystem.</param>
        public ImageProcessingService(string? historyFolder = null)
        {
            _historyFolder = string.IsNullOrWhiteSpace(historyFolder)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageGeneratorApp",
                    "HistoryImages")
                : historyFolder;
        }

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

            var historyFolder = _historyFolder;

            // Treat baseFileName as a name, not a path: strip directories and any existing extension.
            var stem = Path.GetFileNameWithoutExtension(Path.GetFileName(baseFileName));
            if (string.IsNullOrWhiteSpace(stem))
            {
                throw new ArgumentException("Base file name cannot be null or whitespace.", nameof(baseFileName));
            }

            string normalizedHistoryFolder = Path.GetFullPath(historyFolder);
            string fullPath = Path.GetFullPath(Path.Combine(normalizedHistoryFolder, stem + ".webp"));
            if (!string.Equals(Path.GetDirectoryName(fullPath), Path.TrimEndingDirectorySeparator(normalizedHistoryFolder), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid file name resulting in path traversal.", nameof(baseFileName));
            }

            // Offload CPU-heavy image loading, encoding, and IO-heavy saving to a background thread to prevent UI freezing
            await Task.Run(() =>
            {
                using var image = ImageDecoder.Load(sourceImageBytes);

                if (metadata != null)
                {
                    ImageMetadataEmbedder.ApplyMetadata(image, metadata);
                }

                image.Quality = 80;
                image.Settings.SetDefine(MagickFormat.WebP, "lossless", false);
                image.Settings.SetDefine(MagickFormat.WebP, "alpha-quality", 100);
                Directory.CreateDirectory(normalizedHistoryFolder);
                // Open the exact sanitized path with .NET, bypassing native filename expansion.
                using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                image.Write(output, MagickFormat.WebP);
            });

            return fullPath;
        }

        /// <summary>
        /// Loads a WEBP image from disk using Magick.NET, converts it to a PNG stream,
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
            return await Task.Run(() =>
            {
                // 🛡️ Sentinel: Prevent TOCTOU race condition and handle file existence securely
                using var fs = new FileStream(webpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (fs.Length == 0)
                {
                    throw new ArgumentException("File is empty.", nameof(webpFilePath));
                }

                // 🛡️ Sentinel: Enforce maximum file size limit (20 MB) on the opened stream to prevent DoS via memory exhaustion.
                if (fs.Length > 20 * 1024 * 1024)
                {
                    throw new ArgumentException("L'image dépasse la limite de taille autorisée de 20 Mo.", nameof(webpFilePath));
                }

                using var image = ImageDecoder.Load(fs);
                // PNG is supported by GDI+ and retains partial alpha, unlike BMP3.
                using var memoryStream = new MemoryStream(image.ToByteArray(MagickFormat.Png));
                using var temporaryBitmap = new System.Drawing.Bitmap(memoryStream);
                return (System.Drawing.Image)new System.Drawing.Bitmap(temporaryBitmap);
            });
        }
    }

    /// <summary>
    /// Shared native decoding policy for exports and history. Ping inspects dimensions
    /// before allocating the pixel cache; resource limits also apply during native decoding.
    /// </summary>
    internal static class ImageDecoder
    {
        internal const uint MaximumDimension = 16384;
        internal const ulong MaximumPixels = 64000000;

        static ImageDecoder()
        {
            ResourceLimits.Width = MaximumDimension;
            ResourceLimits.Height = MaximumDimension;
            ResourceLimits.Memory = 256UL * 1024 * 1024;
            ResourceLimits.Disk = 1024UL * 1024 * 1024;
            ResourceLimits.MaxMemoryRequest = 256UL * 1024 * 1024;
        }

        internal static MagickImage Load(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            return Load(stream);
        }

        internal static MagickImage Load(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Le flux d'image doit être repositionnable (CanSeek = true).", nameof(stream));
            }

            long position = stream.Position;
            using (var header = new MagickImage())
            {
                header.Ping(stream);
                if (header.Width > MaximumDimension || header.Height > MaximumDimension
                    || (ulong)header.Width * header.Height > MaximumPixels)
                {
                    throw new ArgumentException("L'image dépasse les dimensions autorisées (16 384 pixels par côté, 64 millions de pixels).");
                }
            }
            stream.Position = position;
            var settings = new MagickReadSettings();
            // Preserve the actual ICC bytes instead of replacing recognized profiles with sRGB.
            settings.SetDefine(MagickFormat.Png, "preserve-iCCP", true);
            return new MagickImage(stream, settings);
        }
    }
}