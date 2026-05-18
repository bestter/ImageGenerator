## 2026-05-14 - [Avoid HttpClient Instantiation and Large String Allocations]
**Learning:** Instantiating `HttpClient` per request causes socket exhaustion and adds TCP/TLS handshake latency to every call. Additionally, reading large JSON responses (like base64 images) into strings with `ReadAsStringAsync` creates significant memory pressure.
**Action:** Always use a shared `HttpClient` instance for the lifetime of the application. Stream large responses directly using `ReadAsStreamAsync` and `JsonDocument.ParseAsync` to avoid large string allocations.
## 2026-05-16 - [Optimize JSON Serialization]
**Learning:** Instantiating `HttpClient` per request causes socket exhaustion, and reading/writing large JSON payloads using `JsonSerializer.Serialize` into strings creates significant memory pressure. `JsonContent.Create()` should be used instead to stream the JSON serialization directly to the HTTP request without allocating a huge string in memory first.
**Action:** Always use `JsonContent.Create()` instead of `JsonSerializer.Serialize` combined with `StringContent` when sending large JSON payloads via HttpClient to avoid huge memory allocations.
## 2026-05-17 - [Optimize OS-level User Identity Lookups]
**Learning:** `WindowsIdentity.GetCurrent().Name` makes expensive Windows interop calls to look up the current user, causing CPU overhead and latency. It can also cause compilation errors on cross-platform targets if the compatibility pack isn't referenced correctly.
**Action:** Replace `WindowsIdentity.GetCurrent().Name` with `Environment.UserName` which avoids P/Invoke and works natively cross-platform. Additionally, cache stable values like user hashes to prevent redundant computations on every request.
## 2026-05-18 - [Optimize Large File Reading]
**Learning:** Using `MemoryStream` chunking to read large files (e.g., images up to 20MB) causes excessive Large Object Heap (LOH) allocations and memory copying due to buffer resizing. This drastically impacts performance.
**Action:** Prefer `File.ReadAllBytesAsync` over reading chunks into a `MemoryStream` when reading entire files into memory.
