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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using System;
using System.Collections.Generic;
using System.IO;
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
        public const string AppNameVersion = "GrokImagineApp 2.0.1";

        private static readonly Dictionary<string, string> ModelToGenerator = new(StringComparer.OrdinalIgnoreCase)
        {
            ["grok-imagine-image"] = "Grok Imagine",
            ["grok-imagine-image-quality"] = "Grok Imagine Quality",
            ["nano-banana-pro"] = "Nano Banana Pro"
            // Future: add "dall-e-3" = "DALL-E 3", etc. Extensible without code changes in Form1.
        };

        /// <summary>
        /// Returns a human-readable generator name for the given model ID.
        /// Falls back to the raw model ID if unknown (supports future providers).
        /// </summary>
        public static string GetFriendlyGeneratorName(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return "Unknown";

            return ModelToGenerator.TryGetValue(modelId.Trim(), out var friendly)
                ? friendly
                : modelId.Trim();
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

            // Use fully qualified name to avoid ambiguity with System.Drawing.Image brought in by the WinForms project
            using var image = SixLabors.ImageSharp.Image.Load(sourceImageBytes);

            // Apply standard EXIF and XMP metadata profiles
            ApplyMetadata(image, metadata);

            // === PNG-specific text chunks (iTXt/tEXt) for maximum readability and survival ===
            // These appear as simple key/value text in ExifTool ("PNG-tEXt") and many viewers.
            // Keys are chosen to be both human-friendly and consistent with the XMP fields.
            // Note: For a fresh image from the API we simply Add (no prior custom keys exist).
            if (!wantJpeg)
            {
                // v3 API: GetFormatMetadata requires the format key (PngFormat.Instance)
                var pngMeta = image.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
                if (pngMeta != null)
                {
                    // Use string.Empty for optional language/translated fields (v3 non-nullable)
                    pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Software", metadata.AppCreator, string.Empty, string.Empty));
                    pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Generator", metadata.Generator, string.Empty, string.Empty));
                    pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("AI Model", metadata.ModelId, string.Empty, string.Empty));
                    pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Prompt", metadata.Prompt, string.Empty, string.Empty));
                    pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Created With", metadata.AppCreator, string.Empty, string.Empty));
                    if (!string.IsNullOrEmpty(metadata.Resolution))
                        pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Resolution", metadata.Resolution, string.Empty, string.Empty));
                    if (!string.IsNullOrEmpty(metadata.AspectRatio))
                        pngMeta.TextData.Add(new SixLabors.ImageSharp.Formats.Png.Chunks.PngTextData("Aspect Ratio", metadata.AspectRatio, string.Empty, string.Empty));
                }
            }

            // ⚡ Bolt Optimization: Pre-allocate MemoryStream capacity based on the original source image size.
            // Since we are primarily embedding metadata and re-encoding, the output size will be very similar
            // to the input size. Pre-allocating prevents repeated buffer doubling and severe Large Object Heap (LOH) fragmentation.
            using var outputStream = new MemoryStream(sourceImageBytes.Length + 4096);
            if (wantJpeg)
            {
                // Quality 92 gives excellent visual fidelity for AI art while keeping file size reasonable.
                var encoder = new JpegEncoder { Quality = 92 };
                image.Save(outputStream, encoder);
            }
            else
            {
                // PNG: lossless, preserves the original quality from the generator.
                image.Save(outputStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            }

            return outputStream.ToArray();
        }

        /// <summary>
        /// Applies EXIF and XMP metadata directly to an ImageSharp Image object.
        /// This does not encode or save the image, allowing callers to save it in their desired format (PNG, JPEG, WEBP, etc.).
        /// </summary>
        /// <param name="image">The ImageSharp Image instance to enrich.</param>
        /// <param name="metadata">The metadata model containing prompt and model information.</param>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
        public static void ApplyMetadata(SixLabors.ImageSharp.Image image, ImageGenerationMetadata metadata)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            // === EXIF (standard fields, widely supported) ===
            var exif = image.Metadata.ExifProfile ?? new ExifProfile();

            exif.SetValue(ExifTag.Software, metadata.AppCreator);
            exif.SetValue(ExifTag.DateTime, metadata.GeneratedAtUtc.ToString("yyyy:MM:dd HH:mm:ss"));
            exif.SetValue(ExifTag.DateTimeOriginal, metadata.GeneratedAtUtc.ToString("yyyy:MM:dd HH:mm:ss"));

            // ImageDescription holds the prompt (truncated for EXIF length limits in some containers)
            string desc = metadata.Prompt;
            if (desc.Length > 180)
                desc = desc.Substring(0, 177) + "...";
            exif.SetValue(ExifTag.ImageDescription, desc);

            // Artist/Creator can also carry the generator for tools that only read classic EXIF
            exif.SetValue(ExifTag.Artist, metadata.Generator);

            image.Metadata.ExifProfile = exif;

            // === XMP (rich structured data - recommended for AI-generated images) ===
            string xmpXml = BuildXmpPacket(metadata);
            image.Metadata.XmpProfile = new XmpProfile(Encoding.UTF8.GetBytes(xmpXml));
        }

        private static bool IsJpegExtension(string? ext)
        {
            if (string.IsNullOrEmpty(ext))
                return false;

            ext = ext.TrimStart('.').ToLowerInvariant();
            return ext is "jpg" or "jpeg";
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
            sb.Append(@"<?xpacket begin=""\ufeff"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
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

        private static string XmlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}