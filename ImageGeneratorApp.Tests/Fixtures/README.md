# Imaging migration fixtures

Created locally on October 26, 2026 using SixLabors.ImageSharp **3.1.12**, before removing it from the application. These are generated test patterns, not provider API responses. Keep the committed files: generating them with Magick.NET would invalidate their compatibility purpose.

| File | Construction |
|---|---|
| `legacy-opaque.webp` | 32x24 RGBA8; pixel `(x,y)` = `(x*8,y*10,100,255)`; WebpEncoder quality 80 |
| `legacy-alpha.webp` | Same pattern, with alpha `x*8`; WebpEncoder quality 80 |
| `legacy-alpha-source.png` | 16x16 RGBA8, every pixel `(180,90,45,128)`; default PngEncoder |
| `legacy-alpha-export.jpg` | Same RGBA source; JpegEncoder quality 92, to lock down alpha removal without background compositing |
| `comparison.png` | 2048x2048 RGBA8; pixel `(x,y)` = `(x%256,y%256,(x+y)%256,255)`; default PngEncoder; synthetic local encoding comparison corpus |

The migration tests open the legacy WebP files through the actual WinForms image-processing service and compare the transparent JPEG against the old output. PNG test input also covers Q16 precision and ICC profiles using separate generated images. No compatibility fixture requires ImageSharp at test runtime.

Prepared by GPT-6 — Codex.
