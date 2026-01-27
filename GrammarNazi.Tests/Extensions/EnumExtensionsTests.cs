using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Enums;
using System.Linq;
using Xunit;

namespace GrammarNazi.Tests.Extensions;

public class EnumExtensionsTests
{
    [Theory]
    [InlineData(GrammarAlgorithms.LanguageToolApi)]
    [InlineData(GrammarAlgorithms.Gemini)]
    public void IsDisabled_DisabledAlgorithm_Should_ReturnTrue(GrammarAlgorithms algorithm)
    {
        // Act
        var result = algorithm.IsDisabled();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(GrammarAlgorithms.InternalAlgorithm)]
    [InlineData(GrammarAlgorithms.YandexSpellerApi)]
    [InlineData(GrammarAlgorithms.DatamuseApi)]
    [InlineData(GrammarAlgorithms.GroqApi)]
    public void IsDisabled_EnabledAlgorithm_Should_ReturnFalse(GrammarAlgorithms algorithm)
    {
        // Act
        var result = algorithm.IsDisabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetEnabledValues_Should_NotContainDisabledAlgorithms()
    {
        // Act
        var enabledValues = EnumUtils.GetEnabledValues<GrammarAlgorithms>().ToList();

        // Assert
        Assert.DoesNotContain(GrammarAlgorithms.LanguageToolApi, enabledValues);
        Assert.DoesNotContain(GrammarAlgorithms.Gemini, enabledValues);
    }

    [Fact]
    public void GetEnabledValues_Should_ContainEnabledAlgorithms()
    {
        // Act
        var enabledValues = EnumUtils.GetEnabledValues<GrammarAlgorithms>().ToList();

        // Assert
        Assert.Contains(GrammarAlgorithms.InternalAlgorithm, enabledValues);
        Assert.Contains(GrammarAlgorithms.YandexSpellerApi, enabledValues);
        Assert.Contains(GrammarAlgorithms.DatamuseApi, enabledValues);
        Assert.Contains(GrammarAlgorithms.GroqApi, enabledValues);
    }

    [Fact]
    public void GetEnabledValues_Should_ReturnCorrectCount()
    {
        // Act
        var enabledValues = EnumUtils.GetEnabledValues<GrammarAlgorithms>().ToList();

        // Assert
        Assert.Equal(4, enabledValues.Count); // InternalAlgorithm, YandexSpellerApi, DatamuseApi, GroqApi
    }
}
