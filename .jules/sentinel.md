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
## 2026-05-22 - External API Timeout Missing
**Vulnerability:** The application used a static HttpClient without an explicit timeout, which could cause the application to hang indefinitely if the external API became unresponsive.
**Learning:** In C#, HttpClient uses a default timeout of 100 seconds. While present, setting an explicit timeout provides better, more predictable resilience when dealing with third-party APIs.
**Prevention:** Always set an explicit `Timeout` on shared `HttpClient` instances and handle `TaskCanceledException` gracefully.
## 2024-05-24 - Prevent Information Disclosure in Network Exceptions
**Vulnerability:** Directly returning raw inner exception messages (`ex.Message` and `ex.InnerException.Message`) in `ImageGeneratorException` when a network request fails (e.g. `HttpRequestException`) could expose sensitive internal details such as IP addresses, internal paths, or the underlying system topology to the UI.
**Learning:** Exception messages generated by low-level networking components should be sanitized before being displayed to users.
**Prevention:** Always use generic error messages for network failures instead of passing unhandled exception strings directly up to the UI.
## 2026-05-24 - Missing Input Length Limits
**Vulnerability:** Input fields (like the API key text box) lacked maximum length constraints, creating a potential vector for Denial of Service (DoS) attacks via memory exhaustion if an attacker pastes excessively large strings into the UI thread.
**Learning:** In desktop applications, unbounded text input can lead to high memory usage, UI thread freezing, and subsequent application crashes (OutOfMemoryException) when massive strings are processed or bound to HTTP headers.
**Prevention:** Always enforce reasonable `MaxLength` properties on UI text inputs (e.g., `TextBox`) to prevent memory exhaustion and buffer-related issues.
## 2026-05-25 - Prevent PII Leakage via Leftover Dead Code
**Vulnerability:** The `UserIdHelper.cs` file contained a `ComputeHash` method and accepted an `identityName` argument to generate opaque user IDs by hashing a local user's name with a hardcoded salt. Even though the application logic evolved to not use it directly, the legacy code and its tests were still present.
**Learning:** Leaving unused, insecure fallback code (like predictable hash generation with hardcoded salts) in the codebase is dangerous as it might be mistakenly re-introduced or used by other callers.
**Prevention:** Completely remove vulnerable legacy methods and parameters when transitioning to a more secure mechanism (e.g., GUID generation) and ensure unit tests reflect only the secure pathways.
## 2026-05-26 - Prevent Memory Exhaustion via Unbounded Error Responses
**Vulnerability:** The application was reading the entire response stream of failed API requests into a string (`await reader.ReadToEndAsync()`) without any size limits. A malicious or misconfigured server returning a massive error payload (e.g., an endless stream or multi-gigabyte HTML page) could cause the application to allocate huge strings, leading to memory exhaustion and Denial of Service (DoS).
**Learning:** External API error responses should not be implicitly trusted or fully loaded into memory without constraints, as they are not subject to standard payload validation and may be arbitrarily large.
**Prevention:** Always enforce a size limit when reading error responses by using bounded read methods (like `ReadBlockAsync` into a fixed-size buffer) instead of unbounded reads like `ReadToEndAsync`.
## 2026-05-27 - Missing Input Length Limits on Search Filter
**Vulnerability:** The search filter input (`txtSearch`) lacked maximum length constraints, creating a potential vector for Denial of Service (DoS) attacks via memory exhaustion and CPU spikes if an attacker pastes excessively large strings into the UI thread, causing the filtering logic to hang.
**Learning:** In desktop applications, unbounded text input can lead to high memory usage, UI thread freezing, and subsequent application crashes (OutOfMemoryException) when massive strings are processed, even if they aren't sent externally.
**Prevention:** Always enforce reasonable `MaxLength` properties on UI text inputs (e.g., `TextBox`) to prevent memory exhaustion and buffer-related issues, even on local filtering functionality.
## 2026-05-28 - Missing Input Length Limits on Search TextBoxes in Split Views
**Vulnerability:** The search filter input (`txtSearch`) in `HistoryViewerForm` lacked a maximum length constraint, similar to the previously fixed issue in `TemplatesManagerForm`. This acts as a potential Denial of Service (DoS) attack vector where pasting massive strings freezes the UI thread and consumes memory.
**Learning:** When fixing security issues like missing bounds in one location (e.g. `TemplatesManagerForm.cs`), always search the codebase for similar component instantiations (`TextBox` instances) to ensure the vulnerability isn't duplicated in similar views.
**Prevention:** Apply consistent secure defaults (like `MaxLength`) across all instances of a specific UI pattern (like search boxes) during code reviews.
## 2026-05-30 - TOCTOU File Read Memory Exhaustion on File.ReadAllTextAsync
**Vulnerability:** Checking file existence via `File.Exists` before using `File.ReadAllTextAsync` creates a TOCTOU (Time of check to time of use) race condition. If the file is maliciously replaced with an enormous file after the check, reading the text into memory can cause an OutOfMemoryException (DoS).
**Learning:** Checking file existence is insufficient when reading untrusted or potentially modified local files.
**Prevention:** Always open a `FileStream` first, and then perform security validations (like `stream.Length`) on the opened handle before reading the contents with a `StreamReader`.
## 2026-05-31 - TOCTOU File Read Memory Exhaustion on Image.LoadAsync
**Vulnerability:** Checking file existence via `File.Exists` before using `Image.LoadAsync` creates a TOCTOU (Time of check to time of use) race condition. An attacker can replace a small file with a large one or delete it after the check but before the read.
**Learning:** Checking file existence is insufficient when reading untrusted or potentially modified local files into third party libraries like ImageSharp.
**Prevention:** Always open a `FileStream` first with `FileShare.Read`, and then perform security validations (like `fs.Length`) on the opened handle before passing it to `Image.LoadAsync`.
## 2026-06-03 - Prevent Information Leakage in MessageBox Dailogs
**Vulnerability:** Calling `MessageBox.Show(ex.Message)` or interpolating `ex.Message` directly in user-facing dialogs can inadvertently expose sensitive internal details (such as database structure, exact file paths, or internal error states) to the user.
**Learning:** Any unhandled or explicitly caught exceptions that are passed to the UI layer must be sanitized to prevent leaking diagnostic internals.
**Prevention:** Always catch specific exceptions where possible, and present generic, safe error messages to the user in `MessageBox.Show` instead of relying on the raw exception string.
## 2026-06-04 - Unsecured Plain Text API Key Storage
**Vulnerability:** The API key was previously pulled directly from a TextBox without any secure persistence mechanism. Forcing users to repeatedly paste credentials increases the risk of clipboard scraping, key leakage, or user error.
**Learning:** Forcing users to paste credentials repeatedly without a secure local persistence mechanism is a usability and security issue. Using `ProtectedData` (Windows DPAPI) provides a secure way to store secrets tied to the current Windows user without managing explicit encryption keys.
**Prevention:** Use Windows DPAPI (`System.Security.Cryptography.ProtectedData.Protect` and `Unprotect`) to save and load secrets on disk to avoid requiring users to repeatedly type them or keeping them globally accessible in plain text.
## 2026-06-05 - SQL Wildcard Injection
**Vulnerability:** User-supplied search terms were directly placed into SQL `LIKE` clauses surrounded by `%`. If an attacker input a search term with `%`, `_`, or `[`, it could be interpreted as a wildcard by the database, leading to wildcard injection. This can bypass input intent or cause Denial of Service (DoS) due to expensive database operations.
**Learning:** Standard SQL parameters do not escape wildcard characters within a string used for a `LIKE` operator.
**Prevention:** Always explicitly escape `\`, `%`, and `_` characters in user input and append `ESCAPE '\'` to the SQL query when constructing parameterized `LIKE` queries.
## 2026-06-06 - Prevent Command Injection via Process.Start
**Vulnerability:** The application used `Process.Start` with `UseShellExecute = true` to open local files (like `LICENSE.txt`). If an attacker could control or manipulate the target path or environment variables, `UseShellExecute = true` could allow executing unintended shell commands or opening malicious applications instead of just reading the text file.
**Learning:** `UseShellExecute = true` inherently passes strings to the Windows shell (cmd.exe or similar), which applies its own parsing rules, making it dangerous for dynamic paths.
**Prevention:** Always invoke the specific target executable (e.g., `notepad.exe`) with `UseShellExecute = false`, and securely pass file paths or arguments using the `ArgumentList` property instead of string concatenation.
## 2026-06-08 - TOCTOU File Read Memory Exhaustion on API Key Storage
**Vulnerability:** Checking file existence via `File.Exists` before using `File.ReadAllBytes` creates a TOCTOU (Time of check to time of use) race condition. An attacker can replace a small file with a large one after the check but before the read. Furthermore, `File.ReadAllBytes` loads the entire file into memory without any size constraints, potentially leading to an `OutOfMemoryException` (Denial of Service) if a massive file is supplied.
**Learning:** Checking file existence is insufficient when reading untrusted or potentially modified local files. Unbounded file reads are dangerous for application stability.
**Prevention:** Always open a `FileStream` first, and then perform security validations (like `stream.Length <= 4096`) on the opened handle before allocating arrays and reading the contents securely in chunks or directly if sized appropriately.
## 2026-06-10 - Prevent Path Traversal in Custom File Name Utilities
**Vulnerability:** A file save utility directly used `Path.GetFileNameWithoutExtension(baseFileName)` to extract the base name. Since it didn't explicitly strip directory segments first, malicious directory traversal sequences (like `..\..\`) could bypass checks if the file lacked an extension, allowing an attacker to write files outside the intended target directory.
**Learning:** `Path.GetFileNameWithoutExtension` does not inherently sanitize directory traversal segments if an extension is absent or improperly formatted.
**Prevention:** Always use `Path.GetFileName` to definitively isolate the final file component from a user-provided or potentially tainted path before extracting its extension or combining it into a target folder.

## 2026-06-11 - Prevent Information Leakage in MessageBox Dialogs
**Vulnerability:** The application was exposing the full local path (`licensePath`) of the application directory when displaying a `MessageBox` for a missing license file.
**Learning:** Hardcoding or dynamically interpolating full system paths in user-facing UI elements (like `MessageBox`) can leak sensitive directory structure and execution context information, giving an attacker hints about the underlying system.
**Prevention:** Remove specific local paths from user-facing error dialogs and use generic messages.
## 2026-06-12 - Prevent TOCTOU File Read Before Process.Start
**Vulnerability:** Checking `File.Exists` before launching an external viewer like `notepad.exe` via `Process.Start` creates a TOCTOU (Time of Check to Time of Use) race condition. An attacker could delete or replace the file in the split second after the check passes.
**Learning:** `Process.Start` relies on the external executable to handle its own file opening, meaning we cannot directly stream a locked file handle to it.
**Prevention:** To validate file presence and prevent TOCTOU, wrap the `Process.Start` call inside a `FileStream` block using `FileShare.ReadWrite`. This ensures the file exists at execution time and prevents malicious modifications, while still allowing the external program to open it. Catch `FileNotFoundException` or `DirectoryNotFoundException` on the stream creation instead of relying on `File.Exists`.
## 2026-06-11 - Prevent TOCTOU File Read Before Displaying Image
**Vulnerability:** Checking `File.Exists` before loading an image file like `LoadWebpForWinFormsAsync` creates a TOCTOU (Time of Check to Time of Use) race condition. An attacker could delete or replace the file in the split second after the check passes.
**Learning:** Relying on `File.Exists` for existence checking before operating on a file can lead to race conditions where the file's state changes unexpectedly.
**Prevention:** To validate file presence and prevent TOCTOU, remove the `File.Exists` check and handle the `FileNotFoundException` and `DirectoryNotFoundException` explicitly when opening the file stream during loading.
## 2026-06-13 - Incomplete SQL Wildcard Injection Prevention
**Vulnerability:** While SQL wildcard characters like `%` and `_` were properly escaped for `LIKE` clauses, the opening bracket `[` was omitted. In certain SQL environments or when using specific collations, an unescaped `[` can act as a wildcard, leading to unexpected query results or potential Denial of Service (DoS).
**Learning:** Standard SQL parameters do not escape wildcard characters within a string used for a `LIKE` operator, and `[` must be included in the sanitization chain along with `%` and `_`.
**Prevention:** Always explicitly escape `\`, `%`, `_`, and `[` characters in user input and append `ESCAPE '\'` to the SQL query when constructing parameterized `LIKE` queries.
## 2026-06-19 - Prevent Command Injection via Process.Start
**Vulnerability:** The application used `Process.Start` with `UseShellExecute = true` to open local files (like `LICENSE.txt`). If an attacker could control or manipulate the target path or environment variables, `UseShellExecute = true` could allow executing unintended shell commands or opening malicious applications instead of just reading the text file.
**Learning:** `UseShellExecute = true` inherently passes strings to the Windows shell (cmd.exe or similar), which applies its own parsing rules, making it dangerous for dynamic paths.
**Prevention:** Always invoke the specific target executable (e.g., `notepad.exe`) with `UseShellExecute = false`, and securely pass file paths or arguments using the `ArgumentList` property instead of string concatenation.
## 2026-06-25 - Prevent Path Traversal in DPAPI Key Storage
**Vulnerability:** The API Key storage helper used `provider.Split(Path.GetInvalidFileNameChars())` to sanitize the provider name before writing the local file. While this strips `\` and `/`, it does not inherently guarantee safe extraction of the filename if a path is supplied, potentially leaving other traversal patterns or misinterpretations.
**Learning:** `Path.GetInvalidFileNameChars()` is meant for invalidating characters, not extracting safe filenames from potentially malicious paths.
**Prevention:** Always use `Path.GetFileName()` to definitively isolate the final file component from a user-provided or potentially tainted path before performing any further character sanitization or path combinations.
## 2026-07-02 - Prevent TOCTOU via EAFP
**Vulnerability:** Checking `Directory.Exists` before attempting to create a directory or save a file creates a TOCTOU race condition.
**Learning:** In async file writing methods where you also want to avoid blocking the thread-pool with synchronous `Directory.CreateDirectory` checks, relying on `Directory.Exists` is insecure.
**Prevention:** Use the EAFP (Easier to Ask for Forgiveness than Permission) pattern: try to write the file first, catch `DirectoryNotFoundException`, and only then execute `Directory.CreateDirectory` before retrying.
## 2026-07-08 - Prevent Memory Scraping of Sensitive Secrets
**Vulnerability:** The `ApiKeyStorageHelper` successfully encrypted API keys using `ProtectedData`, but it left the intermediate `plainBytes` array holding the unencrypted key in memory until garbage collected. This increases the risk of the secret being extracted via memory scraping, crash dumps, or side-channel attacks.
**Learning:** Secrets loaded into byte arrays should be explicitly destroyed as soon as they are no longer needed to minimize their exposure window in memory.
**Prevention:** Always wrap cryptographic operations involving plaintext secrets in a `try...finally` block and use `CryptographicOperations.ZeroMemory()` to explicitly clear the sensitive byte array from memory before the method returns.
## 2026-07-21 - Prevent Shoulder Surfing of Secrets in UI
**Vulnerability:** The API key `TextBox` (`txtApiKey`) was manually masked using `PasswordChar = '•'`. While this obfuscates the text, relying on `UseSystemPasswordChar = true` is the recommended, more robust standard in Windows Forms to defer to OS-level secure credential masking.
**Learning:** Hardcoding password characters can be visually inconsistent or lead to edge cases across different environments compared to native OS-level masking.
**Prevention:** To securely mask sensitive input (like API keys) in Windows Forms and prevent shoulder surfing, use `TextBox.UseSystemPasswordChar = true` rather than manually setting a `PasswordChar`. This defers to the OS's default masking character for a secure and consistent UI.
## 2026-06-28 - Prevent DoS via Exponential Template Expansion
**Vulnerability:** The prompt template engine used `string.Replace` in a loop to recursively resolve templates without limiting the final string length. An attacker or user could create templates that recursively expand exponentially (e.g., `{massive} {massive}` where each is already large), leading to memory exhaustion and a Denial of Service (DoS) due to unbounded string growth (Billion Laughs attack).
**Learning:** String manipulation loops that replace small tokens with larger contents can cause exponential memory growth if unconstrained.
**Prevention:** Always enforce a strict maximum length limit on the actively resolved string (`currentPrompt.Length > 100000`) before processing further replacements to prevent memory exhaustion and crash the app gracefully instead.
