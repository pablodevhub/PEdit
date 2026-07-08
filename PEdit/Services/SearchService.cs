using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PEdit.Services
{
    public class SearchResult
    {
        public int Index { get; set; }
        public int Length { get; set; }
    }

    public class SearchService
    {
        public List<SearchResult> FindAll(string content, string searchTerm, bool isRegex)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchTerm)) return results;

            if (isRegex)
            {
                try
                {
                    var matches = Regex.Matches(content, searchTerm);
                    foreach (Match match in matches)
                    {
                        results.Add(new SearchResult { Index = match.Index, Length = match.Length });
                    }
                }
                catch { /* Ignora regex non valide scritte dall'utente */ }
            }
            else
            {
                int index = content.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase);
                while (index != -1)
                {
                    results.Add(new SearchResult { Index = index, Length = searchTerm.Length });
                    index = content.IndexOf(searchTerm, index + searchTerm.Length, System.StringComparison.OrdinalIgnoreCase);
                }
            }

            return results;
        }
    }
}