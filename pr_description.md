🧪 [testing improvement] Add missing edge case tests for ImageGeneratorException

🎯 **What:**
Added missing edge-case unit tests to `ImageGeneratorExceptionTests.cs`. Previously, only the `(string message)` constructor was tested for null inputs. Now, other constructors are verified against empty strings, whitespace, nulls, and very long strings.

📊 **Coverage:**
- Tested `(string message, int statusCode)` with empty strings, whitespace, and null messages.
- Tested `(string message, int statusCode, Exception innerException)` with the same edge cases.
- Tested `(string message, Exception innerException)` with a null message.
- Tested extremely long message strings (100,000 characters).

✨ **Result:**
Enhanced the robustness of the unit tests, proving that `ImageGeneratorException` handles all expected constructor inputs safely.
