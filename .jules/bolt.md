## 2026-05-14 - [Avoid HttpClient Instantiation and Large String Allocations]
**Learning:** Instantiating `HttpClient` per request causes socket exhaustion and adds TCP/TLS handshake latency to every call. Additionally, reading large JSON responses (like base64 images) into strings with `ReadAsStringAsync` creates significant memory pressure.
**Action:** Always use a shared `HttpClient` instance for the lifetime of the application. Stream large responses directly using `ReadAsStreamAsync` and `JsonDocument.ParseAsync` to avoid large string allocations.
