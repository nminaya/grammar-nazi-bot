using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GrammarNazi.Tests.Extensions;

public class EnumerableIndexTests
{
    [Fact]
    public void Index_OnEnumerable_ReturnsIndexAndItemInCorrectOrder()
    {
        // Arrange
        var items = new List<string> { "first", "second", "third" };

        // Act
        var indexed = items.Index().ToList();

        // Assert
        Assert.Equal(3, indexed.Count);

        // Enumerable.Index() yields (Index, Item)
        var (index0, item0) = indexed[0];
        Assert.Equal(0, index0);
        Assert.Equal("first", item0);

        var (index1, item1) = indexed[1];
        Assert.Equal(1, index1);
        Assert.Equal("second", item1);

        var (index2, item2) = indexed[2];
        Assert.Equal(2, index2);
        Assert.Equal("third", item2);
    }
}
