using Microsoft.Extensions.DependencyInjection;
using NTextCat;

namespace GrammarNazi.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNTextCatLanguageService(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddTransient<BasicProfileFactoryBase<RankedLanguageIdentifier>, RankedLanguageIdentifierFactory>();
        }
    }
}