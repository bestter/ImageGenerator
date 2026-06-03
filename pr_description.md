🎯 **What:** Added missing unit tests for `DatabaseHelper.InitializeDatabase` to verify that the SQLite database schema is correctly initialized.
📊 **Coverage:** The new test `InitializeDatabase_ShouldCreateRequiredTablesAndIndexes` covers the table creation (both `templates` and `GenerationHistory` tables) and the creation of necessary indexes (`IX_templates_key`, `IX_templates_category`).
✨ **Result:** Increased test coverage for database schema initialization, ensuring that the application sets up its necessary data structures accurately on startup without regressions.
