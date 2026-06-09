## 2024-05-20 - Streaming JSON deserialization is much faster
**Learning:** For large base64 string payloads (like 20MB image data), parsing the string into a `JsonDocument` DOM consumes significantly more memory and time on the Large Object Heap (LOH) than directly streaming deserialization using `JsonSerializer.DeserializeAsync` with strongly-typed objects.
**Action:** Always prefer `JsonSerializer.DeserializeAsync` over `JsonDocument.ParseAsync` for endpoints expecting massive string payload properties in this codebase.

## 2024-06-25 - Avoid manual SHA256 and StringBuilder loops
**Learning:** Instantiating `SHA256` explicitly and using a `StringBuilder` loop to append `ToString("x2")` for byte formatting causes excessive memory allocations (32 strings + StringBuilder per hash).
**Action:** Use modern .NET APIs `SHA256.HashData()` and `Convert.ToHexStringLower()` for immediate, low-allocation hex string generation.

## 2024-07-15 - Source Generation for JSON
**Learning:** Dynamic JSON serialization/deserialization requires reflection which is slow and memory intensive, particularly for apps hitting APIs frequently.
**Action:** Use .NET JSON Source Generation (`[JsonSerializable]`) via `JsonSerializerContext` to avoid reflection and improve performance.

## 2024-05-23 - Streaming JSON deserialization and Source Generation for Gemini API
**Learning:** Parsing the API response containing a ~20MB base64 string using `JsonDocument.ParseAsync` builds an enormous in-memory DOM, fragmenting the Large Object Heap (LOH). Additionally, serializing anonymous objects via `JsonSerializer.Serialize` incurs significant reflection overhead.
**Action:** Use strongly-typed models, register them in `JsonSerializerContext`, and use `JsonSerializer.DeserializeAsync` and `JsonContent.Create()` to leverage streaming and source generation, vastly reducing memory pressure and processing latency.

## 2024-05-24 - Enforce MaxLength on TextBox
**Learning:** Unbounded text inputs (like an API Key TextBox) in desktop apps can lead to severe memory exhaustion and UI thread freezing if a user accidentally pastes an enormous string (e.g., a massive log file or base64 data). This acts as a potential Denial of Service (DoS) and drastically affects application responsiveness.
**Action:** Always enforce a reasonable `MaxLength` property on `TextBox` controls in C# WinForms to prevent pasting of excessively large payloads.

## 2024-08-01 - Avoid reflection by removing anonymous types in JSON Source Generation
**Learning:** Even when using `JsonContent.Create` with a `JsonSerializerContext`, if properties like `Image` or `Images` are typed as `object` and passed anonymous types (`new { type = "image_url", ... }`), .NET's JSON Source Generator cannot generate serialization code for them. It falls back to slow reflection or fails entirely, causing performance degradation and memory pressure.
**Action:** Always create explicitly defined, strongly-typed classes (e.g., `ImageUrlObject`) for nested data structures and register them in the `JsonSerializerContext` to fully eliminate reflection overhead during JSON serialization.
## 2024-05-30 - Avoid duplicate base64 string decoding
**Learning:** Decoding large base64 strings (like ~20MB AI image results) multiple times using `Convert.FromBase64String` incurs heavy CPU usage and enormous allocations on the Large Object Heap (LOH), leading to noticeable UI stutter when saving or redisplaying an image.
**Action:** When a base64 string must be decoded, cache the resulting `byte[]` at the application level alongside the base64 string, so subsequent operations (like saving to disk) can reuse the raw bytes instantly.

## 2024-08-05 - Avoid redundant database queries during recursive prompt parsing
**Learning:** Calling `_repository.GetByKeyAsync(key)` inside a looping/parsing construct (like `TemplateParser.ProcessPromptAsync` which can iterate up to 20 times) causes redundant database lookups for the same key, creating an I/O bottleneck.
**Action:** Use a local dictionary cache `Dictionary<string, TemplateModel>(StringComparer.OrdinalIgnoreCase)` scoped to the parsing method's execution to cache and reuse previously fetched templates, turning repeated database calls into O(1) hash map lookups.

## 2026-05-28 - Debounce rapid UI inputs triggering database queries
**Learning:** Firing asynchronous database queries (`SearchAsync`) on every keystroke in WinForms `TextChanged` events can cause excessive I/O, UI blocking, and race conditions, leading to poor performance and an unresponsive UI.
**Action:** Always implement a debounce mechanism using `System.Windows.Forms.Timer` (e.g., 300ms interval) for text input events that trigger background queries.
## 2026-05-31 - Pre-allocate MemoryStream for uncompressed images
**Learning:** Writing uncompressed image data (e.g., BMP format) to a default `MemoryStream` causes the stream's internal buffer to repeatedly double in size as data is written. This leads to excessive copying and severe Large Object Heap (LOH) fragmentation, severely degrading performance.
**Action:** Always pre-allocate `MemoryStream` capacity using the formula `(Width * Height * 4) + 1024` before writing uncompressed image data to avoid LOH fragmentation.
## 2026-06-05 - Avoid fetching full entity models when only keys are needed
**Learning:** Fetching full entity models (e.g., using `GetAllAsync`) when only a single column like a `key` is required for an autocomplete cache causes unnecessary data loading, memory allocation for properties (especially large text fields), and object instantiation overhead.
**Action:** Always create targeted queries (e.g., `GetAllKeysAsync`) that select only the specifically required columns when populating UI caches or lists that don't need the entire entity model.

## 2026-06-08 - Suspend DataGridView BindingList events during bulk inserts
**Learning:** Adding items one-by-one to a `BindingList` that is bound to a `DataGridView` without suspending `RaiseListChangedEvents` triggers expensive UI layout and redraw operations on every single addition, heavily degrading performance and freezing the UI during bulk loads or searches.
**Action:** Set `RaiseListChangedEvents = false` on the `BindingList`, perform the bulk `Clear` and `Add` operations, then reset `RaiseListChangedEvents = true` and call `ResetBindings()` to update the UI only once.
## 2026-06-03 - Avoid N+1 queries during prompt parsing loop
**Learning:** Running queries iteratively inside a parsing loop causes N+1 problems and excessive DB lookups.
**Action:** Extract all keys and bulk load missing templates with a single IN query (e.g. `GetByKeysAsync`) before iterating to replace template strings, which brings parsing benchmark time from 1.1s down to ~0.3s.
## 2026-06-03 - Test ImageProcessingService ArgumentExceptions
**Learning:** Argument validation like `sourceImageBytes` or `baseFileName` being null/empty/whitespace are easy cases to test but critical to ensure robust file operations.
**Action:** Always add tests to ensure basic ArgumentExceptions are caught before more complex execution paths are traversed.
## 2026-06-10 - Batch insert items in UI collections
**Learning:** Adding items one-by-one to UI collections like `ListBox.Items` or `ComboBox.Items` inside a `foreach` loop forces repeated internal array resizing and recalculations.
**Action:** Always batch insert items using the `.AddRange(object[])` method when populating UI lists to optimize rendering performance.
## 2026-06-15 - Debounce rapid UI inputs triggering list filtering
**Learning:** Firing synchronous `ApplyFilters()` methods that clear and rebuild a `BindingList` on every keystroke in WinForms `TextChanged` events causes excessive UI stutters, even when list changed events are suspended.
**Action:** Always implement a debounce mechanism using `System.Windows.Forms.Timer` (e.g., 300ms interval) for text input events that trigger UI list filtering and DataGridView bindings.

## 2026-06-20 - Avoid loading full entity models with large strings in lists
**Learning:** Fetching full entity models (e.g., using `SELECT *` in `GetAllAsync` and `SearchAsync`) when populating a UI list causes unnecessary memory allocation for very large text fields (like `RawMetadata`), leading to Large Object Heap (LOH) fragmentation and I/O bottlenecks.
**Action:** Explicitly select only the columns needed for the list view, and lazily load large payload columns (like metadata JSON strings) via a separate targeted query (e.g., `GetRawMetadataAsync`) only when the user selects the specific record.
## 2026-06-25 - Ensure TOCTOU empty file tests are comprehensive
**Learning:** Testing explicitly thrown exceptions on empty files ensures robust application handling before parsing operations fail ambiguously.
**Action:** Always write tests to confirm explicitly caught errors (like empty files) fail predictably and generate appropriate exceptions.
