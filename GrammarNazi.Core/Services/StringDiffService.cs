using GrammarNazi.Domain.Services;
using System;
using System.Linq;

namespace GrammarNazi.Core.Services
{
    public class StringDiffService : IStringDiffService
    {
        /// <summary>
        /// Compute the distance between two strings using Levenshtein distance algorithm
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Levenshtein distance</returns>
        public int ComputeDistance(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public bool IsInComparableRange(string a, string b)
        {
            return a.Except(b).Count() < 2 && b.Except(a).Count() < 2;
        }
    }
}