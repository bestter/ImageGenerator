using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Processes prompts by identifying curly brace template placeholders and recursively
    /// replacing them with resolved values from the template repository.
    /// </summary>
    public partial class TemplateParser
    {
        private readonly TemplateRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateParser"/> class.
        /// </summary>
        /// <param name="repository">The template repository to fetch templates from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is null.</exception>
        public TemplateParser(TemplateRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// A source-generated regular expression that matches templates in the format {key} or {key:param1:param2}.
        /// </summary>
        [GeneratedRegex(@"\{([^{}]+)\}")]
        private static partial Regex TemplateRegex();

        /// <summary>
        /// A source-generated regular expression that matches two or more consecutive spaces.
        /// </summary>
        [GeneratedRegex(@" {2,}")]
        private static partial Regex MultipleSpacesRegex();

        /// <summary>
        /// Recursively resolves template placeholders within a prompt, formats them with any provided parameters,
        /// and post-processes the final output by trimming and clearing double spaces.
        /// </summary>
        /// <param name="inputPrompt">The raw prompt containing template placeholders.</param>
        /// <param name="incrementUsageStats">If true, increments the usage count and updates the last used date in the database for each successfully resolved template.</param>
        /// <returns>The fully-processed prompt string.</returns>
        public async Task<string> ProcessPromptAsync(string inputPrompt, bool incrementUsageStats = false)
        {
            if (string.IsNullOrWhiteSpace(inputPrompt))
            {
                return string.Empty;
            }

            // Validate braces matching (fast-scan) to prevent syntax errors
            int braceCount = 0;
            for (int i = 0; i < inputPrompt.Length; i++)
            {
                char c = inputPrompt[i];
                if (c == '{')
                {
                    braceCount++;
                    if (braceCount > 1)
                    {
                        throw new FormatException("Accolades imbriquées non supportées dans le prompt.");
                    }
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount < 0)
                    {
                        throw new FormatException("Accolade fermante '}' inattendue ou non ouverte.");
                    }
                }
            }
            if (braceCount != 0)
            {
                throw new FormatException("Accolade ouvrante '{' non fermée.");
            }

            var currentPrompt = inputPrompt;

            int iterations = 0;
            const int maxIterations = 20;
            bool replacedAny;

            // ⚡ Bolt Optimization: Local cache to prevent redundant database queries for the same template key during the parsing loop.
            // This significantly reduces I/O latency when processing complex or recursive prompts containing duplicate keys.
            var localCache = new Dictionary<string, TemplateModel>(StringComparer.OrdinalIgnoreCase);

            // ⚡ Bolt Optimization: Completely avoid N+1 queries inside the template resolution loop.
            // Pre-fetch all required templates recursively in bulk using a single IN query per depth level,
            // before beginning any string replacements.
            var keysToFetch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ⚡ Bolt Optimization: Track keys used to batch update usage stats outside the loop
            var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var initialMatches = TemplateRegex().Matches(inputPrompt);
            foreach (System.Text.RegularExpressions.Match match in initialMatches)
            {
                // ⚡ Bolt Optimization: Avoid allocating string arrays via Split(':') just to get the key
                var innerContent = match.Value[1..^1];
                int colonIndex = innerContent.IndexOf(':');
                var key = colonIndex == -1 ? innerContent.Trim() : innerContent.Substring(0, colonIndex).Trim();
                keysToFetch.Add(key);
            }

            int fetchIterations = 0;
            const int maxFetchIterations = 20;

            while (keysToFetch.Count > 0 && fetchIterations < maxFetchIterations)
            {
                var fetchedTemplates = await _repository.GetByKeysAsync(keysToFetch);
                keysToFetch.Clear();

                foreach (var template in fetchedTemplates)
                {
                    localCache[template.Key] = template;

                    var templateMatches = TemplateRegex().Matches(template.Value);
                    foreach (System.Text.RegularExpressions.Match match in templateMatches)
                    {
                        // ⚡ Bolt Optimization: Avoid allocating string arrays via Split(':') just to get the key
                        var innerContent = match.Value[1..^1];
                        int colonIndex = innerContent.IndexOf(':');
                        var innerKey = colonIndex == -1 ? innerContent.Trim() : innerContent.Substring(0, colonIndex).Trim();
                        if (!localCache.ContainsKey(innerKey))
                        {
                            keysToFetch.Add(innerKey);
                        }
                    }
                }
                fetchIterations++;
            }

            do
            {
                replacedAny = false;
                var matches = TemplateRegex().Matches(currentPrompt);

                if (matches.Count == 0)
                {
                    break;
                }

                if (iterations >= maxIterations)
                {
                    throw new InvalidOperationException("Une récursion infinie a été détectée dans les modèles (limite de 20 itérations atteinte).");
                }

                // ⚡ Bolt Optimization: Avoid LINQ chains (.Cast().Select().Distinct().ToList()) in the parsing hot loop.
                // Using a HashSet directly prevents intermediate array allocations, closures, and enumerator overhead.
                // ⚡ Bolt Optimization: Use a string array instead of HashSet since the number of matches is typically very small.
                // This completely avoids the memory allocation and hashing overhead of a HashSet on the hot path.
                var uniqueTags = new string[matches.Count];
                int uniqueCount = 0;
                for (int i = 0; i < matches.Count; i++)
                {
                    string matchVal = matches[i].Value;
                    bool exists = false;
                    for (int j = 0; j < uniqueCount; j++)
                    {
                        if (uniqueTags[j] == matchVal)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        uniqueTags[uniqueCount++] = matchVal;
                    }
                }

                for (int u = 0; u < uniqueCount; u++)
                {
                    var tag = uniqueTags[u];
                    // tag is e.g. "{subject:dog:red}"
                    // Extract inner content without the curly braces
                    var innerContent = tag[1..^1];

                    // ⚡ Bolt Optimization: Avoid string array allocations via Split(':') for templates without parameters
                    int colonIndex = innerContent.IndexOf(':');
                    var key = colonIndex == -1 ? innerContent.Trim() : innerContent.Substring(0, colonIndex).Trim();

                    if (!localCache.TryGetValue(key, out var template))
                    {
                        throw new KeyNotFoundException($"Le modèle '{key}' n'est pas reconnu.");
                    }

                    var templateValue = template.Value;

                    // If parameters were supplied, format the placeholders ({0}, {1}, etc.) inside the template value
                    if (colonIndex != -1)
                    {
                        var paramString = innerContent.Substring(colonIndex + 1);

                        // ⚡ Bolt Optimization: Avoid intermediate array allocations via Split(':') during template resolution.
                        // Iterate through the parameter string using IndexOf to extract parameters without array creation.
                        int paramIndex = 0;
                        int currentIndex = 0;

                        // ⚡ Bolt Optimization: Use a single StringBuilder to apply all parameter replacements
                        // without creating a new string instance on every loop iteration, reducing GC pressure.
                        var sb = new System.Text.StringBuilder(templateValue, templateValue.Length + paramString.Length);

                        do
                        {
                            int nextColon = paramString.IndexOf(':', currentIndex);
                            string paramValue;
                            if (nextColon == -1)
                            {
                                paramValue = paramString.Substring(currentIndex);
                                currentIndex = paramString.Length + 1; // force exit
                            }
                            else
                            {
                                paramValue = paramString.Substring(currentIndex, nextColon - currentIndex);
                                currentIndex = nextColon + 1;
                            }

                            sb.Replace($"{{{paramIndex}}}", paramValue.Trim());
                            paramIndex++;
                        } while (currentIndex <= paramString.Length);

                        templateValue = sb.ToString();
                    }

                    // Update the prompt replacing all occurrences of this specific tag expression
                    currentPrompt = currentPrompt.Replace(tag, templateValue);
                    replacedAny = true;

                    // If requested, asynchronously increment usage stats for this key in the database
                    if (incrementUsageStats)
                    {
                        usedKeys.Add(key);
                    }
                }

                iterations++;

            } while (replacedAny);

            // Clean up double/multiple spaces and trim the final result
            currentPrompt = MultipleSpacesRegex().Replace(currentPrompt, " ").Trim();

            if (incrementUsageStats && usedKeys.Count > 0)
            {
                await _repository.UpdateUsageStatsBulkAsync(usedKeys);
            }

            return currentPrompt;
        }
    }
}