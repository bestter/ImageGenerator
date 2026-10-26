# Imaging migration validation — issue #335

Date: October 26, 2026. Application version: **2.1.0**.
Baseline: commit `087d84e5f071646633a5fa6ef49effd1af01652f`.

## Implementation and decisions

Both application and test projects replace ImageSharp 3.1.12 with Magick.NET-Q16-AnyCPU 15.0.0, backed by ImageMagick 7.1.2-33. The package has native Windows x64, x86 and ARM64 assets. Q16 preserves 16-bit PNG data; JPEG remains 8-bit, and WebP has its existing 8-bit format limitation.

Service boundaries and byte-array/path/Bitmap contracts remain unchanged. `ApplyMetadata` now accepts `IMagickImage<ushort>`. Image decoding/encoding stays in the existing background workflows. Export and history failure handlers in `Form1.cs` still catch all processing exceptions, preserving the original export bytes and successful generation display respectively. No UI layout, database migration or existing-history rewrite was added.

The owner explicitly approved a maximum of 16,384 pixels on either side and 64,000,000 total pixels. The shared decoder inspects the header before decoding pixels; native limits additionally bound dimensions, pixel-cache memory (256 MiB), disk cache (1 GiB), and individual native allocation requests (256 MiB). These are not bounds on total process memory. Existing history file limits are checked on the opened stream: exactly 20 MiB is accepted; one additional byte is rejected. The provider-response size limit is unchanged.

PNG exports preserve pixels, partial alpha, ICC bytes and 16-bit precision. JPEG uses quality 92, 4:2:0 sampling and the previous straight-RGB alpha-removal policy, checked against a legacy fixture. WebP history explicitly uses lossy quality 80 and alpha quality 100. WinForms conversion uses PNG because default ImageMagick BMP headers are not a reliable GDI+ interchange format; the returned Bitmap is independent of the temporary stream and source file.

EXIF fields and prompt truncation boundaries are preserved. Full Unicode survives XMP and PNG text; EXIF ASCII fields remain unsuitable as the authoritative full Unicode prompt. The existing AI namespace/field names are preserved. PNG custom text is applied only to PNG exports. An explicit `XML:com.adobe.xmp` UTF-8 property prevents the native PNG writer from dropping XMP when an sRGB chunk is emitted. The XMP packet now uses a real U+FEFF marker rather than a literal backslash escape. PNG read/write settings retain ICC profiles instead of replacing recognized profiles with only an sRGB chunk.

Redistribution includes the package's unmodified 428,101-byte NOTICE (ImageMagick and bundled codec notices), plus the wrapper's Apache-2.0 license, under `ThirdPartyNotices/`. NOTICE SHA-256: `A08CE0F18E36F83C971AB35A7504523FA25A797E99EEC57E67C96AB4648B1409`. They are copied to build and publish outputs. Sources: [Magick.NET 15.0.0](https://github.com/dlemstra/Magick.NET/releases/tag/15.0.0), [native PNG writer](https://github.com/ImageMagick/ImageMagick/blob/7.1.2-33/coders/png.c), [WebP encoder settings](https://imagemagick.org/webp/).

## Automated and independent checks

Observed on Windows build `10.0.26200`, Intel Core i5-13600K, .NET SDK 10.0.403 / runtime 10.0.14, x64.

```powershell
dotnet build ImageGeneratorApp.csproj
dotnet run --project ImageGeneratorApp.Tests/ImageGeneratorApp.Tests.csproj
git diff --check
dotnet list ImageGeneratorApp.csproj package --include-transitive
dotnet list ImageGeneratorApp.Tests/ImageGeneratorApp.Tests.csproj package --include-transitive
```

Build: successful, zero compiler warnings/errors. Tests: **307 passed**, zero failed/skipped. The existing FluentAssertions runtime license notice is printed by the test runner. `git diff --check`: passed. Neither resolved dependency graph contains ImageSharp; both resolve Magick.NET-Q16-AnyCPU and Magick.NET.Core 15.0.0. Production and test C# source contains no ImageSharp usages.

Tests cover PNG/JPEG-to-WebP with/without metadata; legacy opaque/transparent WebP; independent Bitmap lifetime and file release; all export extension cases; EXIF values including DateTimeOriginal and prompt lengths 179/180/181; Unicode/emoji/non-Latin/XML prompts; optional-field omission; PNG text's seven keys; PNG lossless Q16 pixels and ICC profiles; JPEG alpha policy; ICC preservation across PNG/JPEG/WebP; malformed/truncated input; empty/missing/oversized history files; exact file-size boundaries; oversized dimensions; approved boundary dimensions; sanitized filenames and literal native filename patterns; directory creation; and old/new SQLite entries reopened without modifying the old image. A failed decode leaves no history file or database entry.

`ImageFormatInspector.cs` independently parses serialized PNG chunks, JPEG APP1 segments, WebP RIFF profiles and TIFF EXIF directories. It verifies actual EXIF/XMP payloads without using Magick.NET for the metadata read. XML is parsed with `XDocument`, and the PNG tests require standard XMP iTXt and iCCP chunks. No extra production dependency or live generation request is used.

## Publish and native smoke checks

```powershell
dotnet publish ImageGeneratorApp.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
dotnet publish ImageGeneratorApp.csproj -c Release -r win-x86 --self-contained true -o artifacts/publish/win-x86
dotnet publish ImageGeneratorApp.csproj -c Release -r win-arm64 --self-contained true -o artifacts/publish/win-arm64
./scripts/ValidatePublishedImaging.ps1 -PublishDirectory artifacts/publish/win-x64
./scripts/ValidatePublishedImaging.ps1 -PublishDirectory artifacts/publish/win-x86
```

All three publications succeeded. Native smoke checks passed as actual X64 and X86 processes, using the self-contained publish directories and their native DLLs. The harness references the published application assembly, creates PNG/JPEG exports with metadata, creates and reads new WebP, reads both legacy fixtures, checks alpha and Bitmap lifetime, and deletes files while the returned Bitmap remains alive. The harness's temporary files are removed from the publish directory afterward. This validates the published imaging path on the development host; it does not establish clean-machine installation or a manual GUI workflow. ARM64 native execution was not attempted on this x64 host.

## Local encoding observations

The single synthetic 2048x2048 corpus is `ImageGeneratorApp.Tests/Fixtures/comparison.png`. Old outputs were generated by ImageSharp 3.1.12 in a separate temporary console project; new outputs used Magick.NET Q16 15.0.0 and the same encoder settings as the service. Both measurement programs ran in Release on the host above. Stopwatch readings cover encoding; peak working set is process-wide, includes setup/native-library loading, and is not per-operation allocation. The new harness also clones the loaded Q16 source; setup differs, so memory readings cannot isolate the encoder's overhead.

| Encoder | Old elapsed | New elapsed | Old bytes | New bytes | Old process peak | New process peak |
|---|---:|---:|---:|---:|---:|---:|
| WebP quality 80 | 1,034 ms | 174 ms | 85,006 | 85,506 | 80,289,792 B | 89,706,496 B |
| JPEG quality 92 | 64 ms | 30 ms | 423,401 | 303,823 | 82,427,904 B | 132,497,408 B |

Normalized RMSE against source: WebP old `0.0259248231`, new `0.0258920008`; JPEG old `0.0236040798`, new `0.0236366054`. The JPEG previews showed the same broad color/gradient pattern at the rendered preview scale. These are single-run synthetic observations, not evidence of an overall speed, quality or memory improvement on generated artwork. Byte equality is intentionally not required.

An original x64 self-contained publish was also built from the baseline Git archive with the same release flags. Its directory totaled 154,508,379 bytes versus 180,333,792 bytes for the final migrated x64 publication. The native migration increases the local uncompressed distribution by about 24.6 MiB; no package-size reduction is claimed. Baseline restore reported the existing ImageSharp vulnerability advisories; the migrated dependency restores did not report those warnings.

The local comparison harness commands were:

```powershell
dotnet run --project C:/Users/marti/AppData/Local/Temp/ImageGenerator335Baseline/Baseline.csproj -c Release --no-restore
dotnet run --project C:/Users/marti/AppData/Local/Temp/ImageGenerator335Probe/Probe.csproj -c Release
git archive --format=zip --output=artifacts/baseline-source.zip HEAD
Expand-Archive -LiteralPath artifacts/baseline-source.zip -DestinationPath C:/Users/marti/AppData/Local/Temp/ImageGenerator335Original
dotnet publish C:/Users/marti/AppData/Local/Temp/ImageGenerator335Original/ImageGeneratorApp.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/baseline-win-x64
```

The temporary comparison programs are diagnostics for this session, not application dependencies. Generated comparison outputs are retained locally under ignored `artifacts/imaging-comparison/`. Legacy fixture construction is documented next to the committed fixtures.

## Checks still requiring a target environment

- Run the published application on a clean Windows machine without a separately installed ImageMagick; execute ARM64 imaging smoke on ARM64 hardware.
- Manually open a real pre-migration history, generate new entries, restart the GUI and reopen both generations. Automated database/service reopening passed, but this exact GUI sequence was not performed.
- Run repeated large-image GUI exports/previews and assess responsiveness, long-duration resource release and real-artwork appearance. Automated lifetime/locking checks and the synthetic sample do not replace that check.
- Inspect representative real exported images in an external metadata viewer such as ExifTool. The independent in-repository binary readers already verify generated test outputs; ExifTool was not used.

No additional intentional color/background policy change or unsupported legitimate size was introduced. The newly approved decoded-dimension limits and inherent encoder-byte differences are documented above. The GitHub issue remains open for the outstanding target-environment checks. Publication is tracked by the associated Git commit and pull request.

Prepared by GPT-6 — Codex.
