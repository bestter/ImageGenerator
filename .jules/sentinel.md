## 2024-05-23 - [Privacy/PII Leak in API Request]
**Vulnerability:** The application was sending the plaintext Windows username (`WindowsIdentity.GetCurrent().Name`) as the `user` field in API requests to xAI.
**Learning:** In desktop applications, the local system username often contains PII (First/Last name, or domain login). Exposing this to external third-party APIs without explicit consent is a privacy violation.
**Prevention:** Always use an opaque identifier (like a SHA-256 hash of the identifier or a random GUID) when providing user tracking fields to external APIs.
## 2026-05-14 - [API Error Handling Leakage]
**Vulnerability:** Raw API error responses and unhandled exceptions were displayed directly in the UI MessageBox.
**Learning:** Displaying raw exception messages (ex.Message) or raw HTTP response strings can inadvertently leak stack traces, internal application logic, or third-party API keys/details to the end user.
**Prevention:** Always parse error responses to extract safe user-facing messages, and use generic fallback messages for unhandled exceptions.
