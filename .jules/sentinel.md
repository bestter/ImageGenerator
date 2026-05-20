## 2024-05-14 - Prevent HTTP Header Injection via API Key
 **Vulnerability:** Unsanitized user input (`txtApiKey.Text`) being directly placed into HTTP headers (`Authorization: Bearer <key>`) can allow HTTP Header Injection if newlines are present.
 **Learning:** In C# `HttpClient`, passing strings with `\r` or `\n` to headers like `DefaultRequestHeaders.Add` or `HttpRequestMessage.Headers.Add` can allow malicious actors to inject custom headers.
 **Prevention:** Always trim and explicitly validate header values to ensure they do not contain newline characters `\r` or `\n`.
## 2024-05-15 - Unhandled UI Exceptions Leaking Stack Traces
**Vulnerability:** Unhandled exceptions in UI event handlers (like file saving or reading) caused Windows Forms to show default error dialogs that leak internal stack traces to the user.
**Learning:** In C# WinForms applications without global exception handling, any uncaught exception inside an event handler bubbles up and exposes internal structure.
**Prevention:** Always wrap file I/O and external resource access in `try-catch` blocks within UI handlers, and present a generic, secure error message to the user instead of letting the application crash or show default error dialogs.
## 2026-05-16 - TOCTOU File Read Memory Exhaustion
**Vulnerability:** Checking file length via `FileInfo.Length` before using `File.ReadAllBytesAsync` creates a TOCTOU (Time of check to time of use) race condition. An attacker can replace a small file with a large one after the check but before the read, causing an out-of-memory denial of service.
**Learning:** Performing a security check (size, permissions) on a path before opening it is insecure because the file system can change.
**Prevention:** Always open a `FileStream` first, and then perform security validations (like `stream.Length`) on the opened handle before reading.

## 2026-05-15 - Prevent PII Leakage in API Requests
 **Vulnerability:** The code explicitly called `WindowsIdentity.GetCurrent().Name` and included it in the JSON payload sent to an external API, exposing Personally Identifiable Information (PII).
 **Learning:** Sending system user information directly to third-party APIs can violate privacy and expose sensitive internal details.
 **Prevention:** Avoid sending PII unless strictly necessary. If user tracking is required by an API, generate an opaque identifier (e.g., a hash or UUID) instead, or omit the field entirely if optional.
## 2024-05-15 - Unhandled UI Exceptions Leaking Stack Traces
**Vulnerability:** Unhandled exceptions in UI event handlers (like file saving or reading) caused Windows Forms to show default error dialogs that leak internal stack traces to the user.
**Learning:** In C# WinForms applications without global exception handling, any uncaught exception inside an event handler bubbles up and exposes internal structure.
**Prevention:** Always wrap file I/O and external resource access in `try-catch` blocks within UI handlers, and present a generic, secure error message to the user instead of letting the application crash or show default error dialogs.
## 2026-05-16 - TOCTOU File Read Memory Exhaustion
**Vulnerability:** Checking file length via `FileInfo.Length` before using `File.ReadAllBytesAsync` creates a TOCTOU (Time of check to time of use) race condition. An attacker can replace a small file with a large one after the check but before the read, causing an out-of-memory denial of service.
**Learning:** Performing a security check (size, permissions) on a path before opening it is insecure because the file system can change.
**Prevention:** Always open a `FileStream` first, and then perform security validations (like `stream.Length`) on the opened handle before reading.
## 2026-05-18 - Prevent Information Disclosure in Error Messages
**Vulnerability:** When the API returned a non-JSON error (e.g., 502 Bad Gateway HTML), the raw response body was used as the exception message and shown in the UI, potentially leaking server details or proxy versions.
**Learning:** Unhandled fallback error messages should never expose raw HTTP response bodies directly to the user.
**Prevention:** Always use a generic, safe string as a fallback when an error response cannot be safely parsed as JSON.

## 2024-05-15 - PII Leakage via Predictable User Hashes
**Vulnerability:** The application was hashing `Environment.UserName` to create an opaque user ID for the xAI API. Since the username space is small and the salt is hardcoded, these hashes could be subjected to dictionary attacks to reveal the user's local operating system account name, leaking PII to external services.
**Learning:** Using simple hashing on low-entropy OS-level personal data (like Windows usernames) is insufficient for anonymization when the data is sent to a third-party service, especially when the salt is public/hardcoded.
**Prevention:** For long-term user tracking that requires anonymity, generate a high-entropy stable identifier (like a random GUID), store it securely on the client side (e.g., in `LocalApplicationData`), and send that instead.

## 2024-05-14 - Prevent Information Leakage in Unhandled Exceptions
**Vulnerability:** Unhandled WinForms application exceptions were not caught globally, allowing raw exception details (e.g., stack traces, file paths) to be leaked if the app crashed.
**Learning:** In C# WinForms applications, global unhandled exception handlers (`Application.ThreadException` and `AppDomain.CurrentDomain.UnhandledException`) should be defined. By default, unhandled thread exceptions trigger a default dialog that can show stack traces to users.
**Prevention:** Register `Application.ThreadException` and `AppDomain.CurrentDomain.UnhandledException` at app startup (`Program.Main()`) and call `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)` to catch exceptions on the UI thread and display sanitized, generic error messages.
