namespace GrammarNazi.Domain.Services
{
    public interface IStringDiffService
    {
        int ComputeDistance(string a, string b);

        bool IsInComparableRange(string a, string b);
    }
}