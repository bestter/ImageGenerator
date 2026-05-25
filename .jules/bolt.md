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
