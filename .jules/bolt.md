## 2024-05-20 - Streaming JSON deserialization is much faster
**Learning:** For large base64 string payloads (like 20MB image data), parsing the string into a `JsonDocument` DOM consumes significantly more memory and time on the Large Object Heap (LOH) than directly streaming deserialization using `JsonSerializer.DeserializeAsync` with strongly-typed objects.
**Action:** Always prefer `JsonSerializer.DeserializeAsync` over `JsonDocument.ParseAsync` for endpoints expecting massive string payload properties in this codebase.

## 2024-06-25 - Avoid manual SHA256 and StringBuilder loops
**Learning:** Instantiating `SHA256` explicitly and using a `StringBuilder` loop to append `ToString("x2")` for byte formatting causes excessive memory allocations (32 strings + StringBuilder per hash).
**Action:** Use modern .NET APIs `SHA256.HashData()` and `Convert.ToHexStringLower()` for immediate, low-allocation hex string generation.

## 2024-07-15 - Source Generation for JSON
**Learning:** Dynamic JSON serialization/deserialization requires reflection which is slow and memory intensive, particularly for apps hitting APIs frequently.
**Action:** Use .NET JSON Source Generation (`[JsonSerializable]`) via `JsonSerializerContext` to avoid reflection and improve performance.
