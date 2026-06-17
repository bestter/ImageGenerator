using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using ImageGeneratorApp;

namespace ImageGeneratorApp.Tests
{
    public class ImageMetadataEmbedderTests
    {
        private byte[] CreateDummyImageBytes(bool isJpeg = false)
        {
            using var image = new Image<Rgba32>(10, 10);
            using var ms = new MemoryStream();
            if (isJpeg)
            {
                image.Save(ms, new JpegEncoder());
            }
            else
            {
                image.Save(ms, new PngEncoder());
            }
            return ms.ToArray();
        }

        private ImageGenerationMetadata CreateDummyMetadata(string prompt = "A test prompt")
        {
            return new ImageGenerationMetadata(
                Generator: "TestGenerator",
                Prompt: prompt,
                ModelId: "TestModel",
                GeneratedAtUtc: new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                Resolution: "1024x1024",
                AspectRatio: "1:1",
                AppCreator: "TestApp"
            );
        }

        [Fact]
        public void Embed_NullSourceBytes_ThrowsArgumentException()
        {
            var metadata = CreateDummyMetadata();
            Action action = () => ImageMetadataEmbedder.Embed(null!, metadata);
            action.Should().Throw<ArgumentException>().WithParameterName("sourceImageBytes");
        }

        [Fact]
        public void Embed_EmptySourceBytes_ThrowsArgumentException()
        {
            var metadata = CreateDummyMetadata();
            Action action = () => ImageMetadataEmbedder.Embed(Array.Empty<byte>(), metadata);
            action.Should().Throw<ArgumentException>().WithParameterName("sourceImageBytes");
        }

        [Fact]
        public void Embed_NullMetadata_ThrowsArgumentNullException()
        {
            var bytes = CreateDummyImageBytes();
            Action action = () => ImageMetadataEmbedder.Embed(bytes, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
        }

        [Fact]
        public void Embed_ValidInputPng_ReturnsNewBytesWithMetadata()
        {
            var sourceBytes = CreateDummyImageBytes(false);
            var metadata = CreateDummyMetadata();

            var resultBytes = ImageMetadataEmbedder.Embed(sourceBytes, metadata, ".png");

            resultBytes.Should().NotBeNull();
            resultBytes.Should().NotBeEmpty();

            using var resultImage = SixLabors.ImageSharp.Image.Load(resultBytes);
            resultImage.Metadata.ExifProfile.Should().NotBeNull();
            resultImage.Metadata.XmpProfile.Should().NotBeNull();

            var pngMeta = resultImage.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
            pngMeta.Should().NotBeNull();
            pngMeta!.TextData.Should().Contain(td => td.Keyword == "Generator" && td.Value == metadata.Generator);
            pngMeta.TextData.Should().Contain(td => td.Keyword == "AI Model" && td.Value == metadata.ModelId);
            pngMeta.TextData.Should().Contain(td => td.Keyword == "Resolution" && td.Value == metadata.Resolution);
            pngMeta.TextData.Should().Contain(td => td.Keyword == "Aspect Ratio" && td.Value == metadata.AspectRatio);
            pngMeta.TextData.Should().Contain(td => td.Keyword == "Software" && td.Value == metadata.AppCreator);
        }

        [Fact]
        public void Embed_ValidInputJpeg_ReturnsNewBytesWithMetadata()
        {
            var sourceBytes = CreateDummyImageBytes(true);
            var metadata = CreateDummyMetadata();

            var resultBytes = ImageMetadataEmbedder.Embed(sourceBytes, metadata, ".jpg");

            resultBytes.Should().NotBeNull();
            resultBytes.Should().NotBeEmpty();

            using var resultImage = SixLabors.ImageSharp.Image.Load(resultBytes);
            resultImage.Metadata.ExifProfile.Should().NotBeNull();
            resultImage.Metadata.XmpProfile.Should().NotBeNull();

            // Should not contain PNG metadata
            var pngMeta = resultImage.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
            pngMeta.TextData.Should().BeEmpty();
        }

        [Fact]
        public void Embed_NullExtension_DefaultsToPng()
        {
            var sourceBytes = CreateDummyImageBytes(false);
            var metadata = CreateDummyMetadata();

            var resultBytes = ImageMetadataEmbedder.Embed(sourceBytes, metadata, null);

            resultBytes.Should().NotBeNull();
            resultBytes.Should().NotBeEmpty();

            using var resultImage = SixLabors.ImageSharp.Image.Load(resultBytes);
            var pngMeta = resultImage.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
            pngMeta.Should().NotBeNull();
            pngMeta!.TextData.Should().NotBeEmpty();
        }

        [Fact]
        public void ApplyMetadata_NullImage_ThrowsArgumentNullException()
        {
            var metadata = CreateDummyMetadata();
            Action action = () => ImageMetadataEmbedder.ApplyMetadata(null!, metadata);
            action.Should().Throw<ArgumentNullException>().WithParameterName("image");
        }

        [Fact]
        public void ApplyMetadata_NullMetadata_ThrowsArgumentNullException()
        {
            using var image = new Image<Rgba32>(10, 10);
            Action action = () => ImageMetadataEmbedder.ApplyMetadata(image, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
        }

        [Fact]
        public void ApplyMetadata_ValidInput_SetsExifAndXmp()
        {
            using var image = new Image<Rgba32>(10, 10);
            var metadata = CreateDummyMetadata();

            ImageMetadataEmbedder.ApplyMetadata(image, metadata);

            image.Metadata.ExifProfile.Should().NotBeNull();
            image.Metadata.XmpProfile.Should().NotBeNull();

            var exif = image.Metadata.ExifProfile!;
            exif.Values.FirstOrDefault(v => v.Tag == ExifTag.Software)!.GetValue().Should().Be(metadata.AppCreator);
            exif.Values.FirstOrDefault(v => v.Tag == ExifTag.ImageDescription)!.GetValue().Should().Be(metadata.Prompt);
            exif.Values.FirstOrDefault(v => v.Tag == ExifTag.Artist)!.GetValue().Should().Be(metadata.Generator);
            exif.Values.FirstOrDefault(v => v.Tag == ExifTag.DateTime)!.GetValue().Should().Be("2023:10:01 12:00:00");
        }

        [Fact]
        public void ApplyMetadata_LongPrompt_TruncatesImageDescription()
        {
            using var image = new Image<Rgba32>(10, 10);
            var longPrompt = new string('A', 200);
            var metadata = CreateDummyMetadata(prompt: longPrompt);

            ImageMetadataEmbedder.ApplyMetadata(image, metadata);

            var exif = image.Metadata.ExifProfile!;
            var description = exif.Values.FirstOrDefault(v => v.Tag == ExifTag.ImageDescription)!.GetValue()!.ToString()!;

            description.Length.Should().Be(180);
            description.Should().EndWith("...");
        }

        [Fact]
        public void GetFriendlyGeneratorName_MapsKnownModelsAndFallsBackForUnknown()
        {
            ImageMetadataEmbedder.GetFriendlyGeneratorName("grok-imagine-image").Should().Be("Grok Imagine");
            ImageMetadataEmbedder.GetFriendlyGeneratorName("grok-imagine-image-quality").Should().Be("Grok Imagine Pro");
            ImageMetadataEmbedder.GetFriendlyGeneratorName("nano-banana-pro").Should().Be("Nano Banana Pro");
            ImageMetadataEmbedder.GetFriendlyGeneratorName("future-dall-e-4").Should().Be("future-dall-e-4");
            ImageMetadataEmbedder.GetFriendlyGeneratorName("").Should().Be("Unknown");
        }

        [Fact]
        public void BuildXmpPacket_HasExpectedContent()
        {
            var meta = CreateDummyMetadata();
            using var image = new Image<Rgba32>(10, 10);
            ImageMetadataEmbedder.ApplyMetadata(image, meta);

            var xmpXml = System.Text.Encoding.UTF8.GetString(image.Metadata.XmpProfile!.ToByteArray());

            xmpXml.Should().Contain("TestGenerator");
            xmpXml.Should().Contain("A test prompt");
            xmpXml.Should().Contain("TestModel");
            xmpXml.Should().Contain("1024x1024");
            xmpXml.Should().Contain("1:1");
            xmpXml.Should().Contain("TestApp");
        }

        [Fact]
        public void XmlEscape_HandlesSpecialChars()
        {
            var meta = new ImageGenerationMetadata(
                Generator: "Test <Generator>",
                Prompt: "Test <>&\"'",
                ModelId: "TestModel",
                GeneratedAtUtc: new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                Resolution: "1024x1024",
                AspectRatio: "1:1",
                AppCreator: "TestApp"
            );
            using var image = new Image<Rgba32>(10, 10);
            ImageMetadataEmbedder.ApplyMetadata(image, meta);

            var xmpXml = System.Text.Encoding.UTF8.GetString(image.Metadata.XmpProfile!.ToByteArray());

            xmpXml.Should().Contain("Test &lt;Generator&gt;");
            xmpXml.Should().Contain("Test &lt;&gt;&amp;&quot;&apos;");
        }

        [Fact]
        public void BuildXmpPacket_MissingResolution_OmitsElement()
        {
            var meta = new ImageGenerationMetadata(
                Generator: "TestGenerator",
                Prompt: "A test prompt",
                ModelId: "TestModel",
                GeneratedAtUtc: new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                Resolution: null,
                AspectRatio: null,
                AppCreator: "TestApp"
            );
            using var image = new Image<Rgba32>(10, 10);
            ImageMetadataEmbedder.ApplyMetadata(image, meta);

            var xmpXml = System.Text.Encoding.UTF8.GetString(image.Metadata.XmpProfile!.ToByteArray());

            xmpXml.Should().NotContain("ai:Resolution=");
            xmpXml.Should().NotContain("ai:AspectRatio=");
        }

        [Theory]
        [InlineData(".jpg", true)]
        [InlineData(".jpeg", true)]
        [InlineData(".JPG", true)]
        [InlineData(".JPEG", true)]
        [InlineData("jpg", true)]
        [InlineData("jpeg", true)]
        [InlineData(".png", false)]
        [InlineData("png", false)]
        [InlineData(".webp", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsJpegExtension_ShouldWorkCorrectly(string? extension, bool expectJpeg)
        {
            var bytes = CreateDummyImageBytes(expectJpeg); // To match the logic in the class
            var meta = CreateDummyMetadata();

            var resultBytes = ImageMetadataEmbedder.Embed(bytes, meta, extension);

            // If it's JPEG, there shouldn't be PNG text chunks
            using var resultImage = SixLabors.ImageSharp.Image.Load(resultBytes);
            var pngMeta = resultImage.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);

            if (expectJpeg)
            {
                pngMeta.TextData.Should().BeEmpty();
            }
            else
            {
                pngMeta.TextData.Should().NotBeEmpty();
            }
        }
    }
}
