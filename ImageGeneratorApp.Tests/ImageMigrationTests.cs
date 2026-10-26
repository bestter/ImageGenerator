using ImageMagick;
using System.Buffers.Binary;
using System.Drawing;

namespace ImageGeneratorApp.Tests
{
    public class ImageMigrationTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "ImageGenerator335_" + Guid.NewGuid().ToString("N"));

        public ImageMigrationTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            Directory.Delete(_folder, recursive: true);
        }

        [Theory]
        [InlineData("legacy-opaque.webp", false)]
        [InlineData("legacy-alpha.webp", true)]
        public async Task LegacyHistory_BitmapSurvivesDisposalAndReleasesFile(string fixture, bool alpha)
        {
            string path = Path.Combine(_folder, fixture);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture), path);
            var service = new ImageProcessingService(_folder);
            using var bitmap = (Bitmap)await service.LoadWebpForWinFormsAsync(path);
            bitmap.Width.Should().Be(32);
            bitmap.Height.Should().Be(24);
            using (var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                exclusive.Length.Should().BeGreaterThan(0);
            }
            File.Delete(path);
            bitmap.GetPixel(16, 12).A.Should().Be(alpha ? (byte)128 : (byte)255);
            bitmap.GetPixel(0, 12).A.Should().Be(alpha ? (byte)0 : (byte)255);
            bitmap.Save(Path.Combine(_folder, "independent.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task History_AcceptsPngAndJpeg_WithOptionalMetadata(bool jpeg, bool metadata)
        {
            var service = new ImageProcessingService(_folder);
            var context = ImageMetadataEmbedderTests.Metadata("Été 🌈 <>&");
            byte[] bytes = ImageMetadataEmbedderTests.ImageBytes(jpeg ? MagickFormat.Jpeg : MagickFormat.Png);
            string path = await service.SaveImageAsWebpAsync(bytes, "new", metadata ? context : null);
            using var decoded = new MagickImage(path);
            decoded.Format.Should().Be(MagickFormat.WebP);
            decoded.Width.Should().Be(10);
            decoded.Height.Should().Be(10);
            decoded.HasAlpha.Should().Be(!jpeg);
            if (metadata)
            {
                decoded.GetExifProfile()!.GetValue(ExifTag.Software)!.Value.Should().Be(context.AppCreator);
                decoded.GetExifProfile()!.GetValue(ExifTag.DateTimeOriginal)!.Value.Should().Be("2023:10:01 12:00:00");
                System.Text.Encoding.UTF8.GetString(ImageFormatInspector.Profile(File.ReadAllBytes(path), "webp", "xmp"))
                    .Should().Contain("Été 🌈");
            }
            using var bitmap = (Bitmap)await service.LoadWebpForWinFormsAsync(path);
            bitmap.GetPixel(0, 0).A.Should().Be(jpeg ? (byte)255 : (byte)125);
        }

        [Fact]
        public async Task History_ExactlyTwentyMiB_IsAcceptedButOneExtraByteIsRejected()
        {
            string path = Path.Combine(_folder, "boundary.webp");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "legacy-opaque.webp"), path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) stream.SetLength(20 * 1024 * 1024);
            var service = new ImageProcessingService(_folder);
            using (var bitmap = await service.LoadWebpForWinFormsAsync(path)) bitmap.Width.Should().Be(32);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) stream.SetLength((20 * 1024 * 1024) + 1);
            Func<Task> rejected = () => service.LoadWebpForWinFormsAsync(path);
            await rejected.Should().ThrowAsync<ArgumentException>().WithMessage("*20 Mo*");
        }

        [Theory]
        [InlineData(16385, 1)]
        [InlineData(1, 16385)]
        [InlineData(8001, 8000)]
        public void Decoder_RejectsOversizedDimensionsBeforePixelAllocation(int width, int height)
        {
            byte[] bytes = ImageMetadataEmbedderTests.ImageBytes();
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), (uint)width);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)height);
            // Repair IHDR CRC so the fixture tests dimensions rather than corrupt input.
            uint crc = uint.MaxValue;
            foreach (byte value in bytes.AsSpan(12, 17))
            {
                crc ^= value;
                for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
            }
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(29, 4), ~crc);
            Action decode = () => { using var image = ImageDecoder.Load(bytes); };
            // Native dimension limits can fail during Ping before the managed pixel check.
            decode.Should().Throw<Exception>().Where(e => e is MagickException || e is ArgumentException);
        }

        [Fact]
        public async Task History_OldAndNewDatabaseEntriesReopenWithoutConversion()
        {
            string oldPath = Path.Combine(_folder, "old.webp");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "legacy-alpha.webp"), oldPath);
            byte[] oldBytes = File.ReadAllBytes(oldPath);
            string connectionString = "Data Source=" + Path.Combine(_folder, "history.db") + ";Pooling=False";
            var database = new DatabaseHelper(connectionString);
            var repository = new GenerationHistoryRepository(database);
            var oldRecord = new GenerationHistoryModel { ImagePath = oldPath, Prompt = "Old entry", ModelName = "legacy", CreatedAt = DateTime.UtcNow };
            await repository.InsertAsync(oldRecord);
            var orchestrator = new HistoryOrchestrator(new ImageProcessingService(_folder), repository);
            var newRecord = await orchestrator.LogGenerationAsync(ImageMetadataEmbedderTests.ImageBytes(), "New entry", "model");
            var reopened = new GenerationHistoryRepository(new DatabaseHelper(connectionString));
            var records = (await reopened.GetAllAsync()).ToList();
            records.Should().HaveCount(2);
            records.Should().Contain(r => r.Id == oldRecord.Id && r.ImagePath == oldPath);
            records.Should().Contain(r => r.Id == newRecord.Id && r.ImagePath == newRecord.ImagePath);
            foreach (var record in records)
            {
                using var bitmap = await new ImageProcessingService(_folder).LoadWebpForWinFormsAsync(record.ImagePath);
                bitmap.Width.Should().BeGreaterThan(0);
            }
            File.ReadAllBytes(oldPath).Should().Equal(oldBytes);
        }

        [Theory]
        [InlineData("png")]
        [InlineData("jpg")]
        [InlineData("webp")]
        public async Task EncodedFormats_PreserveEmbeddedIccProfile(string format)
        {
            using var source = new MagickImage(ImageMetadataEmbedderTests.ImageBytes());
            source.SetProfile(ColorProfiles.AdobeRGB1998);
            byte[] input = source.ToByteArray(MagickFormat.Png);
            byte[] output;
            if (format == "webp")
            {
                string path = await new ImageProcessingService(_folder).SaveImageAsWebpAsync(input, "icc", ImageMetadataEmbedderTests.Metadata());
                output = File.ReadAllBytes(path);
            }
            else
            {
                output = ImageMetadataEmbedder.Embed(input, ImageMetadataEmbedderTests.Metadata(), format);
            }
            using var decoded = ImageDecoder.Load(output);
            decoded.GetColorProfile()!.ToByteArray().Should().Equal(ColorProfiles.AdobeRGB1998.ToByteArray());
        }

        [Fact]
        public async Task History_FileNamePatternsAreTreatedLiterally()
        {
            var service = new ImageProcessingService(_folder);
            string path = await service.SaveImageAsWebpAsync(ImageMetadataEmbedderTests.ImageBytes(), "history%03d");
            path.Should().Be(Path.Combine(_folder, "history%03d.webp"));
            Directory.GetFiles(_folder).Should().Equal(path);
            using var bitmap = await service.LoadWebpForWinFormsAsync(path);
            bitmap.Width.Should().Be(10);
        }

        [Fact]
        public void TransparentJpegExport_MatchesLegacyRgbWithoutIntroducingBackgroundCompositing()
        {
            string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            byte[] output = ImageMetadataEmbedder.Embed(File.ReadAllBytes(Path.Combine(fixtures, "legacy-alpha-source.png")), ImageMetadataEmbedderTests.Metadata(), "jpg");
            using var stream = new MemoryStream(output);
            using var actual = new Bitmap(stream);
            using var expected = new Bitmap(Path.Combine(fixtures, "legacy-alpha-export.jpg"));
            Color actualColor = actual.GetPixel(8, 8);
            Color expectedColor = expected.GetPixel(8, 8);
            Math.Abs(actualColor.R - expectedColor.R).Should().BeLessThanOrEqualTo(2);
            Math.Abs(actualColor.G - expectedColor.G).Should().BeLessThanOrEqualTo(2);
            Math.Abs(actualColor.B - expectedColor.B).Should().BeLessThanOrEqualTo(2);
            actualColor.A.Should().Be(255);
        }

        [Fact]
        public void Decoder_AcceptsMaximumSideAndGeneratedImageDimensions()
        {
            foreach ((uint width, uint height) in new[] { (16384u, 1u), (1u, 16384u), (2048u, 2048u) })
            {
                using var source = new MagickImage(MagickColors.Red, width, height);
                using var result = ImageDecoder.Load(source.ToByteArray(MagickFormat.Png));
                result.Width.Should().Be(width);
                result.Height.Should().Be(height);
            }
        }

        [Fact]
        public async Task InvalidHistoryInput_DoesNotProduceAFileOrDatabaseRecord()
        {
            var service = new ImageProcessingService(_folder);
            var database = new DatabaseHelper("Data Source=" + Path.Combine(_folder, "history.db") + ";Pooling=False");
            var repository = new GenerationHistoryRepository(database);
            var orchestrator = new HistoryOrchestrator(service, repository);
            Func<Task> save = () => orchestrator.LogGenerationAsync(new byte[] { 1, 2, 3 }, "valid prompt", "model");
            await save.Should().ThrowAsync<MagickException>();
            Directory.GetFiles(_folder, "*.webp").Should().BeEmpty();
            using var connection = database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM GenerationHistory";
            Convert.ToInt64(command.ExecuteScalar()).Should().Be(0);
        }
    }
}