🎯 **What:** Fixed a potential Path Traversal vulnerability in `ImageProcessingService.SaveImageAsWebpAsync` and added a unit test.

⚠️ **Risk:** Although `Path.GetFileName` provides some initial protection, if a `baseFileName` containing an absolute path (or a path that bypassed previous validations depending on environment differences) was supplied, it could potentially allow writing WebP files outside the intended history directory.

🛡️ **Solution:** Added a rigorous path traversal check using `Path.GetFullPath`. The code now resolves the absolute path of the combined target file and ensures it strictly starts with the normalized absolute path of the intended `historyFolder`. A unit test `SaveImageAsWebpAsync_PathTraversalAttempt_ThrowsArgumentException` was added to verify this fix.
