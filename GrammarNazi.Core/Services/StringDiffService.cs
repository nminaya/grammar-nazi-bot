using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System;

namespace GrammarNazi.Core.Services;

public class StringDiffService : IStringDiffService
{
    /// <summary>
    /// Compute the distance between two strings using Levenshtein distance algorithm.
    /// Uses a single-row rolling array instead of a full 2D matrix to reduce allocations.
    /// </summary>
    public int ComputeDistance(string a, string b)
    {
        int n = a.Length;
        int m = b.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        // Ensure we iterate over the shorter string in the inner loop
        if (n > m)
        {
            (a, b) = (b, a);
            (n, m) = (m, n);
        }

        var previousRow = new int[n + 1];
        var currentRow = new int[n + 1];

        for (int i = 0; i <= n; i++)
            previousRow[i] = i;

        for (int j = 1; j <= m; j++)
        {
            currentRow[0] = j;

            for (int i = 1; i <= n; i++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                currentRow[i] = Math.Min(
                    Math.Min(currentRow[i - 1] + 1, previousRow[i] + 1),
                    previousRow[i - 1] + cost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[n];
    }

    /// <summary>
    /// Compares the characters of two strings and returns true if it's in comparable range.
    /// Uses a simple counting approach instead of LINQ Except to avoid allocations.
    /// </summary>
    public bool IsInComparableRange(string a, string b)
    {
        return CountCharsNotIn(a, b) < Defaults.StringComparableRange
            && CountCharsNotIn(b, a) < Defaults.StringComparableRange;
    }

    private static int CountCharsNotIn(string source, string target)
    {
        int count = 0;
        foreach (char c in source)
        {
            if (target.IndexOf(c) < 0)
                count++;
        }
        return count;
    }
}