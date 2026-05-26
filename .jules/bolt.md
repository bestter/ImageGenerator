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
