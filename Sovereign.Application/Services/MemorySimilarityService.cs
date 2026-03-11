using System.Text.RegularExpressions;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.Services;

public sealed class MemorySimilarityService
{
    public IReadOnlyList<(MemoryEntry Entry, double Score)> Search(IReadOnlyList<MemoryEntry> memories, string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query) || memories.Count == 0)
            return [];

        var queryVector = Vectorize(query);

        return memories
            .Select(memory =>
            {
                var score = CosineSimilarity(queryVector, Vectorize($"{memory.Key} {memory.Value}"));
                return (Entry: memory, Score: Math.Round(score, 4));
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Entry.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 20))
            .ToList();
    }

    private static Dictionary<string, double> Vectorize(string input)
    {
        var tokens = Regex.Matches(input.ToLowerInvariant(), "[a-z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToList();

        if (tokens.Count == 0)
            return new Dictionary<string, double>();

        return tokens.GroupBy(token => token).ToDictionary(group => group.Key, group => (double)group.Count());
    }

    private static double CosineSimilarity(IReadOnlyDictionary<string, double> left, IReadOnlyDictionary<string, double> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;

        var dot = left.Where(kvp => right.ContainsKey(kvp.Key)).Sum(kvp => kvp.Value * right[kvp.Key]);
        if (dot <= 0)
            return 0;

        var leftMagnitude = Math.Sqrt(left.Values.Sum(x => x * x));
        var rightMagnitude = Math.Sqrt(right.Values.Sum(x => x * x));
        if (leftMagnitude == 0 || rightMagnitude == 0)
            return 0;

        return dot / (leftMagnitude * rightMagnitude);
    }
}
