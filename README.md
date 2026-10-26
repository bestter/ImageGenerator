# Image Generator App

Image Generator App is a Windows Forms desktop application for generating AI images with xAI, Google, and OpenAI APIs. It targets .NET 10 on Windows and keeps API calls, image processing, metadata, templates, and generation history local to the desktop workflow.

Current application version: `2.1.0`

Imaging and version documentation reviewed against the source code: October 26, 2026

## Supported providers

| Provider | Model identifiers | Endpoint | Authentication | Image editing in this app |
|---|---|---|---|---|
| xAI Grok Imagine | `grok-imagine-image`, `grok-imagine-image-quality` | `POST https://api.x.ai/v1/images/generations` or `/edits` | `Authorization: Bearer` | Yes, with up to three reference images and multi-turn editing |
| Google Nano Banana Pro | `nano-banana-pro` | `POST https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent` | `x-goog-api-key` | No |
| OpenAI GPT Image | `gpt-image-2` | [`POST https://api.openai.com/v1/images/generations`](https://developers.openai.com/api/reference/resources/images/methods/generate) | `Authorization: Bearer` | No; generation only |

The application stores a separate encrypted API key for each provider. Switching models updates the key label and reloads the matching provider key. Google and OpenAI models disable and clear reference-image and multi-turn controls; selecting either Grok model enables them again.

xAI has [announced the retirement of `grok-imagine-image-quality` on November 2, 2026](https://docs.x.ai/developers/migration/imagine-image-quality-nov-2). The identifier remains available in this application. After that date, xAI says requests using it will be served by `grok-imagine-image-2.0` with `quality: "low"`.

## Features

- Multi-provider image generation from one responsive Windows Forms interface.
- Grok image editing with one to three reference images and iterative multi-turn refinement.
- `1k` and `2k` resolution presets with `1:1`, `16:9`, `9:16`, `4:3`, `3:2`, and `20:9` aspect ratios.
- Asynchronous provider requests and PNG/JPEG export; generated images are displayed on the UI thread.
- PNG/JPEG exports with EXIF and XMP provenance metadata, plus PNG text metadata, when embedding succeeds.
- Recursive SQLite prompt templates using `{key}` and `{key:param1:param2}`, with syntax validation, contextual autocomplete, and resolved-prompt preview.
- Best-effort local generation history with WebP quality 80 compression, SQLite search, and asynchronous preview loading.
- Provider-specific API keys protected with Windows DPAPI for the current Windows user.

Exports attempt to embed the prompt, model, generation date, resolution, and aspect ratio. If embedding fails, the application saves the original image bytes. History WebP files embed the prompt, model, and generation date; resolution and aspect ratio are stored separately in the SQLite history record. A history storage failure does not prevent a successful generation from being displayed.

## Image encoding and deployment

Version 2.1.0 replaces ImageSharp with **Magick.NET-Q16-AnyCPU 15.0.0** (ImageMagick 7.1.2-33). Q16 retains 16-bit PNG precision. PNG exports are lossless and preserve alpha and ICC profiles; JPEG exports use 8-bit quality 92 with 4:2:0 sampling. Transparent JPEG input keeps its straight RGB channels when alpha is dropped, matching the previous export policy. History remains lossy WebP quality 80 with lossless alpha; WebP itself is limited to 8-bit channels. Encoder output bytes, appearance and sizes can differ from the previous library.

EXIF stores Software, DateTime, DateTimeOriginal, ImageDescription (at most 180 characters), and Artist. EXIF ASCII fields cannot reliably represent emoji or all scripts: the full Unicode prompt is retained in XMP and PNG UTF-8 text. XMP retains the `http://ns.grokimagineapp.com/ai/1.0/` namespace and existing fields. PNG adds `Software`, `Generator`, `AI Model`, `Prompt`, `Created With`, and optional `Resolution` / `Aspect Ratio` text keys. JPEG selection accepts jpg/jpeg case-insensitively, with or without a leading dot; null/unknown extensions, including `.webp`, still select PNG.

Existing WebP history files and database records need no conversion. WinForms previews use an intermediate PNG and return an independent bitmap, preserving partial transparency and releasing the source file. Native decoding runs in the existing background image-processing workflows. The owner-approved decode limits are 16,384 pixels per side and 64,000,000 pixels total, checked before pixel decoding. Native pixel-cache memory is limited to 256 MiB with at most 1 GiB disk cache, and individual native allocation requests are capped at 256 MiB. These are resource controls, not a guarantee about total process memory. History reads still validate the opened stream and accept at most 20 MiB; provider-response limits remain unchanged.

The package contains Windows x64, x86 and ARM64 native libraries. The repository has no existing RID-specific publish profile; publish explicitly for the destination architecture, for example:

```powershell
dotnet publish ImageGeneratorApp.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
```

Use `win-x86` or `win-arm64` for the other package-supported Windows architectures. Distribute the entire publish directory, including native DLLs and `ThirdPartyNotices/`; no separate ImageMagick installation is required. The managed wrapper is Apache-2.0. ImageMagick and bundled codecs retain their own licenses, reproduced in the selected package's unmodified [NOTICE](ThirdPartyNotices/Magick.NET-15.0.0.NOTICE.txt) together with the [wrapper license](ThirdPartyNotices/Magick.NET-LICENSE.txt). See the [migration validation record](docs/imaging-migration-335.md) for measured observations and checks still requiring a clean target machine.

## OpenAI GPT Image 2

OpenAI support was added for [ticket #277](https://github.com/bestter/ImageGenerator/issues/277). The integration uses the direct Images API without the OpenAI SDK or any additional dependency.

The application sends only this request shape:

```json
{
  "model": "gpt-image-2",
  "prompt": "A lighthouse in a storm",
  "size": "1280x720",
  "user": "opaque-device-id"
}
```

It does not send `resolution`, `aspect_ratio`, `response_format`, `image`, `images`, `n`, `quality`, `background`, `moderation`, `output_format`, or streaming parameters. One image is requested implicitly. GPT Image models return base64 image data by default, which the application reads from `data[0].b64_json` and passes to the display, export, and best-effort history workflows.

The [OpenAI GPT Image 2 model documentation](https://developers.openai.com/api/docs/models/gpt-image-2) and [Images API reference](https://developers.openai.com/api/reference/resources/images/methods/generate) support arbitrary `WIDTHxHEIGHT` sizes within the model limits. The application intentionally exposes only the following validated mappings:

| Resolution | `1:1` | `16:9` | `9:16` | `4:3` | `3:2` | `20:9` |
|---|---|---|---|---|---|---|
| `1k` | `1024x1024` | `1280x720` | `720x1280` | `1024x768` | `1056x704` | `1280x576` |
| `2k` | `2048x2048` | `2048x1152` | `1152x2048` | `2048x1536` | `2016x1344` | `1920x864` |

Unsupported resolution/aspect-ratio combinations and reference images are rejected before any OpenAI HTTP request is made. OpenAI image editing remains outside the application scope even if the upstream model supports it. Model access, billing, and any account or organization verification requirements are managed externally in the OpenAI account.

## Requirements

- Windows, matching the `net10.0-windows10.0.22621.0` target framework.
- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- At least one provider credential:
  - xAI API key for Grok Imagine;
  - Google Cloud API key for Nano Banana Pro;
  - OpenAI API key with access to `gpt-image-2` for OpenAI GPT Image.

## Build and run

```powershell
dotnet build ImageGeneratorApp.csproj
dotnet run --project ImageGeneratorApp.csproj
```

All network tests use a mocked `HttpMessageHandler`; the test suite never calls a live provider API.

## Local data

Application data is stored below `%LocalAppData%\ImageGeneratorApp\`:

| Data | Location |
|---|---|
| Provider API keys | `ApiKey_xAI.dat`, `ApiKey_Google.dat`, and `ApiKey_OpenAI.dat` |
| Opaque device identifier | `device_id.txt` |
| Prompt templates and generation history | `templates.db` |
| Compressed history images | `HistoryImages\` |

API key files are encrypted with Windows DPAPI using `DataProtectionScope.CurrentUser`. Key loading is limited to 4,096 encrypted bytes. The opaque API user identifier is a generated GUID and does not expose the Windows user name. Successful generated images are limited to 50 MiB of decoded data before they enter the display and persistence workflows.

## Architecture

| Component | Responsibility |
|---|---|
| `Form1.cs` | Code-first UI, provider state, generation workflow, display, export, and validation |
| `ImageGeneratorClient.cs` | Provider routing, HTTP requests, defensive error parsing, response parsing, and image-size limits |
| `ImageGeneratorRequest.cs` | xAI generation and edit request model |
| `OpenAIImageRequest.cs` | Minimal OpenAI generation request model |
| `ImageProviderCatalog.cs` | Model identifiers, storage provider names, editing support, and friendly generator names |
| `GeminiModels.cs` | Google Gemini request and response models |
| `ImageGeneratorResponse.cs` | Shared xAI/OpenAI `data[].b64_json` response model |
| `ImageGeneratorJsonContext.cs` | Source-generated `System.Text.Json` serialization metadata |
| `ApiKeyStorageHelper.cs` | Provider-specific DPAPI key persistence |
| `TemplateParser.cs` and `TemplateRepository.cs` | Recursive prompt templates and SQLite persistence |
| `HistoryOrchestrator.cs` | Coordination of WebP history storage and SQLite logging |
| `ImageProcessingService.cs` | Image decoding, WebP encoding, and WinForms-compatible bitmap conversion |
| `ImageMetadataEmbedder.cs` | EXIF, XMP, PNG, and friendly generator metadata |

The UI is defined programmatically in `Form1.InitializeControls()`. Network logic stays in `ImageGeneratorClient`, and the shared `HttpClient` is injected into the client.

## Testing

The test project uses xUnit v3, Moq, and FluentAssertions. OpenAI coverage includes:

- all twelve resolution/aspect-ratio mappings;
- exact URL, HTTP method, Bearer token, and JSON payload;
- confirmation that xAI-specific and editing properties are absent;
- reference-image and unknown-size rejection before network access;
- HTTP 401 and 429 errors;
- malformed JSON and successful responses without `b64_json`;
- provider key isolation and friendly OpenAI metadata.

For code changes, run the repository validation sequence:

```powershell
dotnet build ImageGeneratorApp.csproj
dotnet run --project ImageGeneratorApp.Tests/ImageGeneratorApp.Tests.csproj
git diff --check
```

Report the observed build warnings and test results rather than assuming a fixed outcome.

Imaging regression tests also check the decoder's seekable-stream contract and independent WinForms bitmap lifetime. The published-imaging validation script removes its temporary build directory and copied harness files after success or failure, including failed builds and partial copies.

## Documentation maintenance

Documentation is part of the implementation. Any change to providers, model identifiers, endpoints, authentication, request or response payloads, supported sizes, editing behavior, API-key storage, metadata, user-visible workflows, prerequisites, or tests must update this README in the same change. OpenAI behavior must be checked against current official OpenAI documentation before it is documented.

Repository instruction files such as `AGENTS.md`, `ANTIGRAVITY.md`, and `.editorconfig` have separate protection rules and must not be edited without explicit owner authorization.

## License

Copyright (C) 2026 Martin Labelle (@bestter)

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version.

This program is distributed without any warranty; without even the implied warranty of merchantability or fitness for a particular purpose. See [LICENSE.txt](LICENSE.txt) for the complete license text.
