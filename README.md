# Image Generator App

Image Generator App is a Windows Forms desktop application for generating AI images with xAI, Google, and OpenAI APIs. It targets .NET 10 on Windows and keeps API calls, image processing, metadata, templates, and generation history local to the desktop workflow.

Current application version: `2.0.1`

Documentation last verified against the codebase: August 25, 2026

## Supported providers

| Provider | Model identifiers | Endpoint | Authentication | Image editing in this app |
|---|---|---|---|---|
| xAI Grok Imagine | `grok-imagine-image`, `grok-imagine-image-quality` | `POST https://api.x.ai/v1/images/generations` or `/edits` | `Authorization: Bearer` | Yes, with up to three reference images and multi-turn editing |
| Google Nano Banana Pro | `nano-banana-pro` | `POST https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent` | `x-goog-api-key` | No |
| OpenAI GPT Image | `gpt-image-2` | [`POST https://api.openai.com/v1/images/generations`](https://developers.openai.com/api/reference/resources/images/methods/generate) | `Authorization: Bearer` | No; generation only |

The application stores a separate encrypted API key for each provider. Switching models updates the key label and reloads the matching provider key. Google and OpenAI models disable and clear reference-image and multi-turn controls; selecting either Grok model enables them again.

## Features

- Multi-provider image generation from one responsive Windows Forms interface.
- Grok image editing with one to three reference images and iterative multi-turn refinement.
- `1k` and `2k` resolution presets with `1:1`, `16:9`, `9:16`, `4:3`, `3:2`, and `20:9` aspect ratios.
- Asynchronous image display and PNG/JPEG export without blocking the UI.
- Automatic EXIF, XMP, and PNG metadata containing the prompt, model, generation date, resolution, and other provenance information.
- Recursive SQLite prompt templates using `{key}` and `{key:param1:param2}`, with syntax validation, contextual autocomplete, and resolved-prompt preview.
- Automatic local generation history with WebP quality 80 compression, metadata preservation, SQLite search, and asynchronous preview loading.
- Provider-specific API keys protected with Windows DPAPI for the current Windows user.

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

It does not send `resolution`, `aspect_ratio`, `response_format`, `image`, `images`, `n`, `quality`, `background`, `moderation`, `output_format`, or streaming parameters. One image is requested implicitly. GPT Image models return base64 image data by default, which the application reads from `data[0].b64_json` and routes through the existing display, export, metadata, and history workflows.

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
dotnet test --verbosity normal
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

Run the complete suite before submitting changes:

```powershell
dotnet test --verbosity normal
```

The expected result is a successful build with every test passing and zero compiler warnings.

## Documentation maintenance

Documentation is part of the implementation. Any change to providers, model identifiers, endpoints, authentication, request or response payloads, supported sizes, editing behavior, API-key storage, metadata, user-visible workflows, prerequisites, or tests must update this README in the same change. OpenAI behavior must be checked against current official OpenAI documentation before it is documented.

Repository instruction files such as `AGENTS.md`, `ANTIGRAVITY.md`, and `.editorconfig` have separate protection rules and must not be edited without explicit owner authorization.

## License

Copyright (C) 2026 Martin Labelle (@bestter)

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version.

This program is distributed without any warranty; without even the implied warranty of merchantability or fitness for a particular purpose. See [LICENSE.txt](LICENSE.txt) for the complete license text.
