using System;

namespace ImageGeneratorApp.Tests
{
    public class AutocompleteQueryCacheTests
    {
        [Fact]
        public void Get_ReturnsCachedMatchesCaseInsensitively()
        {
            AutocompleteQueryCache cache = new AutocompleteQueryCache();
            string[] matches = { "Alpha", "Beta" };

            cache.Add("alp", matches);

            cache.Get("ALP").Should().BeSameAs(matches);
        }

        [Fact]
        public void Add_EvictsOldestEntryAfter64DistinctQueries()
        {
            AutocompleteQueryCache cache = new AutocompleteQueryCache();
            for (int i = 0; i < 64; i++)
            {
                cache.Add($"query-{i}", [$"match-{i}"]);
            }

            cache.Get("query-0").Should().NotBeNull();
            cache.Add("QUERY-1", ["replacement"]);
            cache.Add("query-64", Array.Empty<string>());

            cache.Get("query-0").Should().BeNull();
            cache.Get("query-1").Should().ContainSingle().Which.Should().Be("match-1");
            cache.Get("query-64").Should().BeEmpty();
        }

        [Fact]
        public void Clear_RemovesEntriesAndResetsEvictionOrder()
        {
            AutocompleteQueryCache cache = new AutocompleteQueryCache();
            cache.Add("old", ["match"]);

            cache.Clear();
            cache.Add("old", ["new match"]);
            for (int i = 0; i < 63; i++)
            {
                cache.Add($"new-{i}", Array.Empty<string>());
            }

            cache.Get("old").Should().ContainSingle().Which.Should().Be("new match");
            cache.Add("new-63", Array.Empty<string>());

            cache.Get("old").Should().BeNull();
            cache.Get("new-0").Should().BeEmpty();
            cache.Get("new-63").Should().BeEmpty();
        }
    }
}