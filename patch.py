content = """        private static string XmlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // ⚡ Bolt Optimization: Use a single StringBuilder to avoid multiple string allocations
            // from chaining string.Replace() for character escaping.
            var sb = new StringBuilder(value.Length + 10);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }"""

with open("ImageMetadataEmbedder.cs", "r") as f:
    file_content = f.read()

new_content = """        private static readonly char[] XmlCharsToEscape = { '&', '<', '>', '"', '\\'' };

        private static string XmlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Fast-path: avoid any allocations if no escaping is required
            if (value.IndexOfAny(XmlCharsToEscape) < 0)
                return value;

            // ⚡ Bolt Optimization: Use a single StringBuilder to avoid multiple string allocations
            // from chaining string.Replace() for character escaping.
            var sb = new StringBuilder(value.Length + 10);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\\'': sb.Append("&apos;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }"""

file_content = file_content.replace(content, new_content)

with open("ImageMetadataEmbedder.cs", "w") as f:
    f.write(file_content)
