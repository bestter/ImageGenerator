## 2024-05-14 - Prevent HTTP Header Injection via API Key
 **Vulnerability:** Unsanitized user input (`txtApiKey.Text`) being directly placed into HTTP headers (`Authorization: Bearer <key>`) can allow HTTP Header Injection if newlines are present.
 **Learning:** In C# `HttpClient`, passing strings with `\r` or `\n` to headers like `DefaultRequestHeaders.Add` or `HttpRequestMessage.Headers.Add` can allow malicious actors to inject custom headers.
 **Prevention:** Always trim and explicitly validate header values to ensure they do not contain newline characters `\r` or `\n`.

## 2026-05-15 - Prevent PII Leakage in API Requests
 **Vulnerability:** The code explicitly called `WindowsIdentity.GetCurrent().Name` and included it in the JSON payload sent to an external API, exposing Personally Identifiable Information (PII).
 **Learning:** Sending system user information directly to third-party APIs can violate privacy and expose sensitive internal details.
 **Prevention:** Avoid sending PII unless strictly necessary. If user tracking is required by an API, generate an opaque identifier (e.g., a hash or UUID) instead, or omit the field entirely if optional.
