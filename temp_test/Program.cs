using System;
using System.Linq;

public static class Extensions
{
    public static bool ContainsAny(this string str, StringComparison comparisonType = StringComparison.Ordinal, params string[] values)
    {
        Console.WriteLine($"Called with {comparisonType}");
        return values.Any(x => str.Contains(x, comparisonType));
    }
}

public class Program
{
    public static void Main()
    {
        "hello".ContainsAny("h", "e");
    }
}
