🎯 **What:**
Extracted `LoadWebpForWinFormsAsync` and `SaveImageAsWebpAsync` tests from `HistoryOrchestratorTests.cs` into a dedicated `ImageProcessingServiceTests.cs` file. Added specific test coverage for `SaveImageAsWebpAsync` error conditions (null/empty bytes, null/whitespace base file name).

📊 **Coverage:**
The new `ImageProcessingServiceTests.cs` isolated test class now explicitly covers:
- `LoadWebpForWinFormsAsync` resolving to a valid `System.Drawing.Bitmap` correctly
- `LoadWebpForWinFormsAsync` rejecting an empty WebP file (with `ArgumentException` stating "File is empty.*")
- `LoadWebpForWinFormsAsync` properly validating invalid paths (null/whitespace paths) and handling non-existent files.
- `SaveImageAsWebpAsync` properly throwing `ArgumentException` for null or empty source image byte arrays.
- `SaveImageAsWebpAsync` properly throwing `ArgumentException` for null or whitespace base file names.

✨ **Result:**
The codebase has significantly improved reliability by structurally segregating integration tests from unit tests. `ImageProcessingService` now has full dedicated test coverage for all edge cases including the TOCTOU protection and file existence assertions.
