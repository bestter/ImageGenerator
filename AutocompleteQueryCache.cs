using System;
using System.Collections.Generic;

namespace ImageGeneratorApp
{
    internal sealed class AutocompleteQueryCache
    {
        private const int MaxEntries = 64;

        private readonly Dictionary<string, string[]> _matches = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _insertionOrder = new Queue<string>();

        internal string[]? Get(string query)
        {
            return _matches.TryGetValue(query, out string[]? matches) ? matches : null;
        }

        internal void Add(string query, string[] matches)
        {
            if (!_matches.TryAdd(query, matches))
            {
                return;
            }

            _insertionOrder.Enqueue(query);
            if (_insertionOrder.Count > MaxEntries)
            {
                _matches.Remove(_insertionOrder.Dequeue());
            }
        }

        internal void Clear()
        {
            _matches.Clear();
            _insertionOrder.Clear();
        }
    }
}