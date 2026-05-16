## 2026-05-14 - [Avoid HttpClient Instantiation and Large String Allocations]
**Learning:** Instantiating `HttpClient` per request causes socket exhaustion and adds TCP/TLS handshake latency to every call. Additionally, reading large JSON responses (like base64 images) into strings with `ReadAsStringAsync` creates significant memory pressure.
**Action:** Always use a shared `HttpClient` instance for the lifetime of the application. Stream large responses directly using `ReadAsStreamAsync` and `JsonDocument.ParseAsync` to avoid large string allocations.
## 2026-05-16 - [Optimize JSON Serialization]
**Learning:** Instantiating `HttpClient` per request causes socket exhaustion, and reading/writing large JSON payloads using `JsonSerializer.Serialize` into strings creates significant memory pressure. `JsonContent.Create()` should be used instead to stream the JSON serialization directly to the HTTP request without allocating a huge string in memory first.
**Action:** Always use `JsonContent.Create()` instead of `JsonSerializer.Serialize` combined with `StringContent` when sending large JSON payloads via HttpClient to avoid huge memory allocations.
