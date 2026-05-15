## 2024-05-14 - Prevent HTTP Header Injection via API Key
 **Vulnerability:** Unsanitized user input (`txtApiKey.Text`) being directly placed into HTTP headers (`Authorization: Bearer <key>`) can allow HTTP Header Injection if newlines are present.
 **Learning:** In C# `HttpClient`, passing strings with `\r` or `\n` to headers like `DefaultRequestHeaders.Add` or `HttpRequestMessage.Headers.Add` can allow malicious actors to inject custom headers.
 **Prevention:** Always trim and explicitly validate header values to ensure they do not contain newline characters `\r` or `\n`.
## 2024-05-15 - Unhandled UI Exceptions Leaking Stack Traces
**Vulnerability:** Unhandled exceptions in UI event handlers (like file saving or reading) caused Windows Forms to show default error dialogs that leak internal stack traces to the user.
**Learning:** In C# WinForms applications without global exception handling, any uncaught exception inside an event handler bubbles up and exposes internal structure.
**Prevention:** Always wrap file I/O and external resource access in `try-catch` blocks within UI handlers, and present a generic, secure error message to the user instead of letting the application crash or show default error dialogs.
