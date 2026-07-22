1. **Implement an MRU cache for Images in `HistoryViewerForm`:**
   Currently, every time a user clicks on an item in the `HistoryViewerForm` grid, the application loads and decodes the WEBP image from the disk via `_imageProcessingService.LoadWebpForWinFormsAsync`. During rapid navigation (e.g., using arrow keys), this causes high CPU usage, heavy disk I/O, and UI delays. We can implement a small LRU/MRU (Most Recently Used) cache (e.g., keeping the last 10 images in memory) to make switching between recently viewed images instantaneous.
   - Use `replace_with_git_merge_diff` to modify `HistoryViewerForm.cs`.
   - Add a `Dictionary<string, System.Drawing.Image> _imageCache` and a `List<string> _imageCacheOrder` tracking the recently loaded images.
   - In `UpdateSelectionDetails`, check if `history.ImagePath` is in `_imageCache`. If it is, use it directly (and update the MRU queue).
   - If it's a miss, load from disk and add to the cache. When the cache exceeds 10, dispose of the oldest image and remove it from the cache. Note that we must avoid calling `oldImage?.Dispose();` if the image is in the cache.
   - Modify the `protected override void Dispose(bool disposing)` method located in `HistoryViewerForm.cs` (lines 681-692) to clear and dispose of all cached images.

2. **Read `HistoryViewerForm.cs`:**
   - Read the file `HistoryViewerForm.cs` to confirm the MRU cache logic was written correctly.

3. **Run `dotnet build` to verify compilation:**
   - Run `dotnet build` to verify that the application and test code compile successfully without regressions, as `dotnet test` is unavailable.

4. Complete pre commit steps to ensure proper testing, verification, review, and reflection are done.

5. **Submit the PR:**
   - Call the `submit` tool with title "⚡ Bolt: [performance improvement] Cache decoded images in History Viewer" and the necessary description details.
