🛡️ Sentinel: Prevent HTTP Header Injection via API Key

🎯 **What:** Validated `txtApiKey.Text` by rejecting any input containing newline characters (`\r` or `\n`) prior to using it in the `Authorization` header.
⚠️ **Risk:** An attacker or malicious user could input newline characters to inject arbitrary HTTP headers, potentially manipulating requests or exploiting server behavior via HTTP Header Injection.
🛡️ **Solution:** The variable `apiKey` is extracted via `.Trim()`, validated for carriage returns and line feeds (which prompts an error message if found), and used safely when adding the header to the HTTP Request Message.
