## 2024-05-14 - Prevent HTTP Header Injection via API Key
 **Vulnerability:** Unsanitized user input (`txtApiKey.Text`) being directly placed into HTTP headers (`Authorization: Bearer <key>`) can allow HTTP Header Injection if newlines are present.
 **Learning:** In C# `HttpClient`, passing strings with `\r` or `\n` to headers like `DefaultRequestHeaders.Add` or `HttpRequestMessage.Headers.Add` can allow malicious actors to inject custom headers.
 **Prevention:** Always trim and explicitly validate header values to ensure they do not contain newline characters `\r` or `\n`.
