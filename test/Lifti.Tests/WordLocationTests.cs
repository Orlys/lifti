﻿using FluentAssertions;
using Xunit;

namespace Lifti.Tests
{
    public class WordLocationTests
    {
        [Theory]
        [InlineData(5, 8)]
        [InlineData(5, 6)]
        [InlineData(0, 1)]
        [InlineData(10000, 10001)]
        public void WordsWithLowerStartValues_ShouldBeLessThanWordsWithHigherStartValues(int firstWordStart, int secondWordStart)
        {
            var firstWord = new WordLocation(1, firstWordStart, 100);
            var secondWord = new WordLocation(1, secondWordStart, 100);

            (firstWord < secondWord).Should().BeTrue();
            (secondWord < firstWord).Should().BeFalse();
            (firstWord > secondWord).Should().BeFalse();
            (secondWord > firstWord).Should().BeTrue();
            (firstWord <= secondWord).Should().BeTrue();
            (secondWord <= firstWord).Should().BeFalse();
            (firstWord >= secondWord).Should().BeFalse();
            (secondWord >= firstWord).Should().BeTrue();

            firstWord.Should().BeLessThan(secondWord);
            secondWord.Should().BeGreaterThan(firstWord);
            firstWord.Should().NotBe(secondWord);
        }

        [Fact]
        public void WordsWithTheSameValues_ShouldBeEqual()
        {
            var firstWord = new WordLocation(1, 3, 100);
            var secondWord = new WordLocation(1, 3, 100);

            (firstWord == secondWord).Should().BeTrue();
            (firstWord != secondWord).Should().BeFalse();
            firstWord.Should().Be(secondWord);
        }

        [Fact]
        public void WordsWithDifferentValues_ShouldNotBeEqual()
        {
            var firstWord = new WordLocation(1, 3, 100);
            var secondWord = new WordLocation(1, 4, 100);

            (firstWord == secondWord).Should().BeFalse();
            (firstWord != secondWord).Should().BeTrue();
            firstWord.Should().NotBe(secondWord);
        }
    }
}
