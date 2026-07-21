🎯 **What:** The file size check used `new FileInfo(file).Length` which introduces a Time-of-Check to Time-of-Use (TOCTOU) vulnerability where the file can be replaced between the check and its actual use.
⚠️ **Risk:** A malicious actor could swap the file for a massive one after the size check, potentially crashing the application or causing a Denial of Service (DoS) due to memory exhaustion.
🛡️ **Solution:** Open a `FileStream` securely and check its length directly on the open handle. This ensures that the size verified is for the exact file instance that is being processed.
