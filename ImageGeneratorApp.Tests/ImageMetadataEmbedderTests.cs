using ImageMagick;
using System.Text;
using System.Xml.Linq;

namespace ImageGeneratorApp.Tests
{
    public class ImageMetadataEmbedderTests
    {
        internal static ImageGenerationMetadata Metadata(string prompt = "A test prompt") => new(
            "TestGenerator", prompt, "TestModel", new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), "1024x1024", "1:1", "TestApp");

        internal static byte[] ImageBytes(MagickFormat format = MagickFormat.Png)
        {
            using var image = new MagickImage(new MagickColor(12000, 24000, 36000, 32000), 10, 10);
            image.Depth = 16;
            return image.ToByteArray(format);
        }

        [Theory]
        [InlineData(".jpg", true)]
        [InlineData(".jpeg", true)]
        [InlineData(".JPG", true)]
        [InlineData(".JPEG", true)]
        [InlineData("jpg", true)]
        [InlineData("JPEG", true)]
        [InlineData(".png", false)]
        [InlineData("png", false)]
        [InlineData(".webp", false)]
        [InlineData(".unknown", false)]
        [InlineData("   ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Embed_SelectsFormatAndWritesFormatSpecificMetadata(string? extension, bool jpeg)
        {
            byte[] result = ImageMetadataEmbedder.Embed(ImageBytes(), Metadata(), extension);
            using var image = new MagickImage(result);
            image.Format.Should().Be(jpeg ? MagickFormat.Jpeg : MagickFormat.Png);
            image.Width.Should().Be(10);
            image.Height.Should().Be(10);
            if (jpeg)
            {
                // Inspect the actual JPEG container, not Magick's shared EXIF attributes.
                result.Take(2).Should().Equal(new byte[] { 255, 216 });
                Encoding.Latin1.GetString(result).Should().NotContain("Created With\0");
            }
            else
            {
                Dictionary<string, string> text = ImageFormatInspector.PngText(result);
                text["Software"].Should().Be("TestApp");
                text["Generator"].Should().Be("TestGenerator");
                text["AI Model"].Should().Be("TestModel");
                text["Prompt"].Should().Be("A test prompt");
                text["Created With"].Should().Be("TestApp");
                text["Resolution"].Should().Be("1024x1024");
                text["Aspect Ratio"].Should().Be("1:1");
            }
        }

        [Theory]
        [InlineData("png", 179)]
        [InlineData("png", 180)]
        [InlineData("png", 181)]
        [InlineData("jpg", 179)]
        [InlineData("jpg", 180)]
        [InlineData("jpg", 181)]
        [InlineData("webp", 179)]
        [InlineData("webp", 180)]
        [InlineData("webp", 181)]
        public void EncodedMetadata_RoundTripsAndIndependentContainerReaderFindsExactValues(string format, int promptLength)
        {
            var metadata = Metadata(new string('A', promptLength));
            byte[] result = Encode(format, metadata);
            using var image = new MagickImage(result);
            string description = promptLength > 180 ? new string('A', 177) + "..." : metadata.Prompt;
            var exif = image.GetExifProfile()!;
            exif.GetValue(ExifTag.Software)!.Value.Should().Be(metadata.AppCreator);
            exif.GetValue(ExifTag.DateTime)!.Value.Should().Be("2023:10:01 12:00:00");
            exif.GetValue(ExifTag.DateTimeOriginal)!.Value.Should().Be("2023:10:01 12:00:00");
            exif.GetValue(ExifTag.Artist)!.Value.Should().Be(metadata.Generator);
            exif.GetValue(ExifTag.ImageDescription)!.Value.Should().Be(description);

            Dictionary<ushort, string> independentExif = ImageFormatInspector.ExifStrings(ImageFormatInspector.Profile(result, format, "exif"));
            independentExif[0x0131].Should().Be(metadata.AppCreator);
            independentExif[0x0132].Should().Be("2023:10:01 12:00:00");
            independentExif[0x9003].Should().Be("2023:10:01 12:00:00");
            independentExif[0x010e].Should().Be(description);
            independentExif[0x013b].Should().Be(metadata.Generator);
            AssertXmp(ImageFormatInspector.Profile(result, format, "xmp"), metadata);
        }

        [Theory]
        [InlineData("png")]
        [InlineData("jpg")]
        [InlineData("webp")]
        public void EncodedMetadata_PreservesUnicodeAndXmlAndOmitsOptionalFields(string format)
        {
            var metadata = Metadata("Été à Montréal 🌈 日本語 <>&\"' " + new string('é', 200)) with { Resolution = null, AspectRatio = null };
            byte[] result = Encode(format, metadata);
            AssertXmp(ImageFormatInspector.Profile(result, format, "xmp"), metadata);
            if (format == "png")
            {
                var text = ImageFormatInspector.PngText(result);
                text["Prompt"].Should().Be(metadata.Prompt);
                text.Should().NotContainKey("Resolution");
                text.Should().NotContainKey("Aspect Ratio");
            }
        }

        private static void AssertXmp(byte[] bytes, ImageGenerationMetadata metadata)
        {
            var document = XDocument.Parse(Encoding.UTF8.GetString(bytes));
            XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            XNamespace ai = "http://ns.grokimagineapp.com/ai/1.0/";
            XNamespace xmp = "http://ns.adobe.com/xap/1.0/";
            XNamespace dc = "http://purl.org/dc/elements/1.1/";
            var description = document.Descendants(rdf + "Description").Single();
            ((string?)description.Attribute(ai + "Prompt")).Should().Be(metadata.Prompt);
            ((string?)description.Attribute(dc + "description")).Should().Be(metadata.Prompt);
            ((string?)description.Attribute(ai + "Generator")).Should().Be(metadata.Generator);
            ((string?)description.Attribute(ai + "ModelId")).Should().Be(metadata.ModelId);
            ((string?)description.Attribute(ai + "GeneratedAt")).Should().Be(metadata.GeneratedAtUtc.ToString("o"));
            ((string?)description.Attribute(dc + "creator")).Should().Be(metadata.Generator);
            ((string?)description.Attribute(xmp + "CreatorTool")).Should().Be(metadata.AppCreator);
            ((string?)description.Attribute(xmp + "CreateDate")).Should().Be(metadata.GeneratedAtUtc.ToString("o"));
            ((string?)description.Attribute(ai + "Resolution")).Should().Be(metadata.Resolution);
            ((string?)description.Attribute(ai + "AspectRatio")).Should().Be(metadata.AspectRatio);
        }

        private static byte[] Encode(string format, ImageGenerationMetadata metadata)
        {
            if (format != "webp") return ImageMetadataEmbedder.Embed(ImageBytes(), metadata, format);
            using var image = new MagickImage(ImageBytes());
            ImageMetadataEmbedder.ApplyMetadata(image, metadata);
            image.Quality = 80;
            image.Settings.SetDefine(MagickFormat.WebP, "lossless", false);
            return image.ToByteArray(MagickFormat.WebP);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Embed_LosslessPng_PreservesPixelsAlphaIccAndSixteenBitPrecision(bool srgb)
        {
            using var source = new MagickImage(ImageBytes());
            source.SetProfile(srgb ? ColorProfiles.SRGB : ColorProfiles.AdobeRGB1998);
            source.Settings.SetDefine(MagickFormat.Png, "preserve-iCCP", true);
            byte[] bytes = source.ToByteArray(MagickFormat.Png);
            byte[] output = ImageMetadataEmbedder.Embed(bytes, Metadata(), ".png");
            using var result = ImageDecoder.Load(output);
            result.Depth.Should().Be(16);
            result.HasAlpha.Should().BeTrue();
            result.GetColorProfile()!.ToByteArray().Should().Equal(source.GetColorProfile()!.ToByteArray());
            using var sourcePixels = source.GetPixels();
            using var resultPixels = result.GetPixels();
            resultPixels.ToArray().Should().Equal(sourcePixels.ToArray()!);
            ImageFormatInspector.PngChunks(output).Should().Contain(c => c.Type == "iCCP");
            ImageFormatInspector.PngChunks(output).Should().Contain(c => c.Type == "iTXt" && Encoding.ASCII.GetString(c.Data).StartsWith("XML:com.adobe.xmp"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Embed_JpegInput_CanBeExportedAsPngOrJpeg(bool jpeg)
        {
            using var image = new MagickImage(ImageMetadataEmbedder.Embed(ImageBytes(MagickFormat.Jpeg), Metadata(), jpeg ? "jpg" : "png"));
            image.Format.Should().Be(jpeg ? MagickFormat.Jpeg : MagickFormat.Png);
        }

        [Fact]
        public void InvalidArguments_PreservePublicErrors()
        {
            Action nullBytes = () => ImageMetadataEmbedder.Embed(null!, Metadata());
            Action emptyBytes = () => ImageMetadataEmbedder.Embed([], Metadata());
            Action nullMetadata = () => ImageMetadataEmbedder.Embed(ImageBytes(), null!);
            Action nullImage = () => ImageMetadataEmbedder.ApplyMetadata(null!, Metadata());
            using var image = new MagickImage(ImageBytes());
            Action nullAppliedMetadata = () => ImageMetadataEmbedder.ApplyMetadata(image, null!);
            nullBytes.Should().Throw<ArgumentException>().WithParameterName("sourceImageBytes");
            emptyBytes.Should().Throw<ArgumentException>().WithParameterName("sourceImageBytes");
            nullMetadata.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
            nullImage.Should().Throw<ArgumentNullException>().WithParameterName("image");
            nullAppliedMetadata.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
        }

        [Fact]
        public void Embed_MalformedAndTruncatedInput_Throws()
        {
            Action malformed = () => ImageMetadataEmbedder.Embed(new byte[] { 1, 2, 3 }, Metadata());
            Action truncated = () => ImageMetadataEmbedder.Embed(ImageBytes().Take(35).ToArray(), Metadata());
            malformed.Should().Throw<MagickException>();
            truncated.Should().Throw<MagickException>();
        }

        [Theory]
        [InlineData("grok-imagine-image", "Grok Imagine")]
        [InlineData("grok-imagine-image-quality", "Grok Imagine Quality")]
        [InlineData("nano-banana-pro", "Nano Banana Pro")]
        [InlineData("gpt-image-2", "OpenAI GPT Image")]
        [InlineData("future-model", "future-model")]
        [InlineData("", "Unknown")]
        public void GetFriendlyGeneratorName_PreservesMappings(string model, string expected)
        {
            ImageMetadataEmbedder.GetFriendlyGeneratorName(model).Should().Be(expected);
        }
    }
}