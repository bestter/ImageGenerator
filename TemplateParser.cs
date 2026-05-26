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

            var currentPrompt = inputPrompt;
            var missingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int iterations = 0;
            const int maxIterations = 20;
            bool replacedAny;

            do
            {
                replacedAny = false;
                var matches = TemplateRegex().Matches(currentPrompt);

                if (matches.Count == 0)
                {
                    break;
                }

                // Process unique tags in the current iteration to optimize database queries and string replacements
                var uniqueTags = matches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();

                foreach (var tag in uniqueTags)
                {
                    // tag is e.g. "{subject:dog:red}"
                    // Extract inner content without the curly braces
                    var innerContent = tag[1..^1];

                    // Split key and parameters by colons
                    var parts = innerContent.Split(':');
                    var key = parts[0].Trim();

                    // Skip database roundtrip if this key was already checked and not found in this request
                    if (missingKeys.Contains(key))
                    {
                        continue;
                    }

                    var template = await _repository.GetByKeyAsync(key);
                    if (template == null)
                    {
                        missingKeys.Add(key);
                        continue;
                    }

                    var templateValue = template.Value;

                    // If parameters were supplied, format the placeholders ({0}, {1}, etc.) inside the template value
                    if (parts.Length > 1)
                    {
                        var parameters = parts.Skip(1).Select(p => p.Trim()).ToArray();
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            templateValue = templateValue.Replace($"{{{i}}}", parameters[i]);
                        }
                    }

                    // Update the prompt replacing all occurrences of this specific tag expression
                    currentPrompt = currentPrompt.Replace(tag, templateValue);
                    replacedAny = true;

                    // If requested, asynchronously increment usage stats for this key in the database
                    if (incrementUsageStats)
                    {
                        await _repository.UpdateUsageStatsAsync(key);
                    }
                }

                iterations++;

            } while (replacedAny && iterations < maxIterations);

            // Clean up double/multiple spaces and trim the final result
            currentPrompt = MultipleSpacesRegex().Replace(currentPrompt, " ").Trim();

            return currentPrompt;
        }
    }
}
