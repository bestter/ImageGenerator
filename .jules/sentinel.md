## 2025-02-27 - 🛡️ Fix Missing Entropy in Windows DPAPI

**Vulnerability:** The `ApiKeyStorageHelper` class was using `ProtectedData.Protect` and `ProtectedData.Unprotect` to store API keys but passed `null` for the optional entropy parameter. This meant the data was encrypted only with the standard user-specific key, making it slightly more susceptible to automated decryption tools if an attacker gains access to the user's profile or if the data is stolen and analyzed offline.

**Learning:** When using Windows DPAPI (`System.Security.Cryptography.ProtectedData`), always provide additional entropy (a secondary, application-specific byte array) to increase the difficulty of offline attacks and tie the encrypted blob specifically to the application, not just the user.

**Prevention:** Ensure that calls to `ProtectedData.Protect` and `Unprotect` include a static, non-null byte array for the `optionalEntropy` parameter.
