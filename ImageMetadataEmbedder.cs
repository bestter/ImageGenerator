// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using ImageMagick;
using System;
using System.Text;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Immutable snapshot of generation context captured at the moment an image is produced.
    /// Used by the embedder to inject provenance metadata (EXIF + XMP + PNG text chunks).
    /// </summary>
    public sealed record ImageGenerationMetadata(
        string Generator,
        string Prompt,
        string ModelId,
        DateTime GeneratedAtUtc,
        string? Resolution,
        string? AspectRatio,
        string AppCreator);

    /// <summary>
    /// Reusable service for embedding AI generation metadata into exported images.
    /// Supports JPEG (EXIF + XMP in APP1) and PNG (EXIF where applicable + XMP iTXt + tEXt/iTXt text chunks).
    /// Designed to be robust: callers should fallback to original bytes on any exception.
    /// </summary>
    public static class ImageMetadataEmbedder
    {
        /// <summary>
        /// Application name and version string written into Software / CreatorTool fields.
        /// Update when releasing new versions. This is the value used for "Standard / Creator metadata".
        /// </summary>
        public const string AppNameVersion = "GrokImagineApp 2.1.0";

        /// <summary>
        /// Returns a human-readable generator name for the given model ID.
        /// Falls back to the raw model ID if unknown (supports future providers).
        /// </summary>
        public static string GetFriendlyGeneratorName(string modelId)
        {
            return ImageProviderCatalog.GetFriendlyGeneratorName(modelId);
        }

        /// <summary>
        /// Embeds generation metadata into the provided image bytes and returns the new bytes
        /// encoded in the format indicated by fileExtension (or PNG if null/unknown).
        ///
        /// Embedded fields (standards-based and tool-friendly):
        /// - EXIF: Software, DateTime, ImageDescription (prompt)
        /// - XMP: CreatorTool, CreateDate, dc:description (prompt), plus ai:Generator, ai:Model, ai:Prompt etc.
        /// - PNG only: additional tEXt/iTXt chunks for "Prompt", "Generator", "Software" (human readable, survives many uploaders).
        ///
        /// The metadata is designed to be readable by ExifTool, Adobe tools, gallery sites, and OS viewers.
        /// </summary>
        /// <param name="sourceImageBytes">Original image bytes (typically PNG from the generator APIs).</param>
        /// <param name="metadata">Captured generation context.</param>
        /// <param name="fileExtension">Target extension (".png", ".jpg", ".jpeg"). Determines output encoder.</param>
        /// <returns>New image bytes with metadata embedded, or the original bytes if input is invalid for processing.</returns>
        public static byte[] Embed(byte[] sourceImageBytes, ImageGenerationMetadata metadata, string? fileExtension = null)
        {
            if (sourceImageBytes == null || sourceImageBytes.Length == 0)
                throw new ArgumentException("Source image bytes are required.", nameof(sourceImageBytes));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            bool wantJpeg = IsJpegExtension(fileExtension);

            using var image = ImageDecoder.Load(sourceImageBytes);
            ApplyMetadata(image, metadata);

            if (wantJpeg)
            {
                // Keep straight RGB when dropping alpha, matching the previous JPEG encoder.
                image.Alpha(AlphaAction.Off);
                image.Depth = 8;
                image.Quality = 92;
                image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "2x2");
                return image.ToByteArray(MagickFormat.Jpeg);
            }

            // Attributes are shared across formats in Magick.NET: add PNG text only here.
            image.SetAttribute("Software", metadata.AppCreator);
            image.SetAttribute("Generator", metadata.Generator);
            image.SetAttribute("AI Model", metadata.ModelId);
            image.SetAttribute("Prompt", metadata.Prompt);
            image.SetAttribute("Created With", metadata.AppCreator);
            // The native PNG writer can omit XMP when it emits an sRGB chunk.
            // An explicit UTF-8 text property writes the standard XMP iTXt keyword.
            image.SetAttribute("XML:com.adobe.xmp", BuildXmpPacket(metadata));
            if (!string.IsNullOrEmpty(metadata.Resolution))
                image.SetAttribute("Resolution", metadata.Resolution);
            if (!string.IsNullOrEmpty(metadata.AspectRatio))
                image.SetAttribute("Aspect Ratio", metadata.AspectRatio);

            // Preserve source precision; Q16 avoids silently quantizing 16-bit PNG input.
            image.Settings.SetDefine(MagickFormat.Png, "bit-depth", image.Depth > 8 ? "16" : "8");
            image.Settings.SetDefine(MagickFormat.Png, "preserve-iCCP", true);
            return image.ToByteArray(MagickFormat.Png);
        }

        /// <summary>
        /// Applies EXIF and XMP profiles before encoding into PNG, JPEG or WebP.
        /// </summary>
        public static void ApplyMetadata(IMagickImage<ushort> image, ImageGenerationMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(metadata);

            var exif = image.GetExifProfile() ?? new ExifProfile();
            exif.SetValue(ExifTag.Software, metadata.AppCreator);
            string date = metadata.GeneratedAtUtc.ToString("yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            exif.SetValue(ExifTag.DateTime, date);
            exif.SetValue(ExifTag.DateTimeOriginal, date);
            string description = metadata.Prompt.Length > 180 ? metadata.Prompt.Substring(0, 177) + "..." : metadata.Prompt;
            exif.SetValue(ExifTag.ImageDescription, description);
            exif.SetValue(ExifTag.Artist, metadata.Generator);
            image.SetProfile(exif);
            image.SetProfile(new XmpProfile(Encoding.UTF8.GetBytes(BuildXmpPacket(metadata))));
        }

        private static bool IsJpegExtension(string? ext)
        {
            if (string.IsNullOrEmpty(ext))
                return false;

            // ⚡ Bolt Optimization: Avoid chained string allocations (.ToLower().TrimStart('.')) for file extension parsing
            ReadOnlySpan<char> span = ext.AsSpan();
            if (span.Length > 0 && span[0] == '.')
            {
                span = span.Slice(1);
            }

            return span.Equals("jpg", StringComparison.OrdinalIgnoreCase) ||
                   span.Equals("jpeg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds a minimal but complete XMP packet containing both standard fields and a custom
        /// AI namespace (ai:) with the generator, full prompt, model, and timing information.
        /// This structure is readable by ExifTool ("XMP-ai"), Photoshop, and modern asset managers.
        /// </summary>
        private static string BuildXmpPacket(ImageGenerationMetadata m)
        {
            string escapedPrompt = XmlEscape(m.Prompt);
            string escapedGenerator = XmlEscape(m.Generator);
            string escapedModel = XmlEscape(m.ModelId);
            string escapedApp = XmlEscape(m.AppCreator);
            string isoDate = m.GeneratedAtUtc.ToString("o");

            var sb = new StringBuilder(1024);
            sb.Append("<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>");
            sb.Append(@"
<x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""");
            sb.Append(escapedApp);
            sb.Append(@""">
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
         xmlns:ai=""http://ns.grokimagineapp.com/ai/1.0/"">
  <rdf:Description rdf:about=""""
      dc:description=""");
            sb.Append(escapedPrompt);
            sb.Append(@"""
      dc:creator=""");
            sb.Append(escapedGenerator);
            sb.Append(@"""
      xmp:CreatorTool=""");
            sb.Append(escapedApp);
            sb.Append(@"""
      xmp:CreateDate=""");
            sb.Append(isoDate);
            sb.Append(@"""
      ai:Generator=""");
            sb.Append(escapedGenerator);
            sb.Append(@"""
      ai:ModelId=""");
            sb.Append(escapedModel);
            sb.Append(@"""
      ai:Prompt=""");
            sb.Append(escapedPrompt);
            sb.Append(@"""
      ai:GeneratedAt=""");
            sb.Append(isoDate);
            sb.Append(@"""");

            if (!string.IsNullOrEmpty(m.Resolution))
            {
                sb.Append(@"
      ai:Resolution=""");
                sb.Append(XmlEscape(m.Resolution));
                sb.Append(@"""");
            }
            if (!string.IsNullOrEmpty(m.AspectRatio))
            {
                sb.Append(@"
      ai:AspectRatio=""");
                sb.Append(XmlEscape(m.AspectRatio));
                sb.Append(@"""");
            }

            sb.Append(@"
  />
</rdf:RDF>
</x:xmpmeta>
<?xpacket end=""w""?>");

            return sb.ToString();
        }

        private static readonly char[] XmlSpecialChars = { '&', '<', '>', '"', '\'' };

        private static string XmlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // ⚡ Bolt Optimization: Zero-allocation fast path for strings that don't need escaping
            if (value.IndexOfAny(XmlSpecialChars) == -1)
            {
                return value;
            }

            // ⚡ Bolt Optimization: Use StringBuilder and loop to avoid multiple intermediate string allocations from chaining Replace() calls
            var sb = new StringBuilder(value.Length + 10);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}