﻿using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class NearQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new NearQueryOperator(
                new FakeQueryPart(
                    QueryWordMatch(7, FieldMatch(1, 8, 20, 100), FieldMatch(2, 9, 14)),
                    QueryWordMatch(8, FieldMatch(1, 11, 101), FieldMatch(2, 8, 104))),
                new FakeQueryPart(
                    QueryWordMatch(7, FieldMatch(1, 6, 14, 102)),
                    QueryWordMatch(8, FieldMatch(1, 5, 106), FieldMatch(2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: (8, 6) (100, 102)
            // Field 2: None
            // Item 8 matches:
            // Field 1: (101, 106)
            // Field 2: (8, 3) (104, 105)
            results.Matches.Should().BeEquivalentTo(
                QueryWordMatch(
                    7,
                    new FieldMatch(1, CompositeMatch(8, 6), CompositeMatch(100, 102))),
                QueryWordMatch(
                    8,
                    new FieldMatch(1, CompositeMatch(101, 106)),
                    new FieldMatch(2, CompositeMatch(8, 3), CompositeMatch(104, 105))));
        }
    }
}
