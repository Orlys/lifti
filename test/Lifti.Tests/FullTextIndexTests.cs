using FluentAssertions;
using PerformanceProfiling;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests : FullTextIndexTestBase
    {
        [Fact]
        public void IndexedItemsShouldBeRetrievable()
        {
            this.WithIndexedStrings();

            var results = this.sut.Search("this test");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void IndexShouldBeSearchableWithHypenatedText()
        {
            this.WithIndexedStrings();

            var results = this.sut.Search("third-eye");

            results.Should().HaveCount(1);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedBySimpleStringsShouldBeAssociatedToDefaultField()
        {
            this.WithIndexedStrings();

            var results = this.sut.Search("this");

            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Unspecified")).Should().BeTrue();
        }

        [Fact]
        public void SearchingByMultipleWildcards_ShouldReturnResult()
        {
            this.WithIndexedStrings();

            var results = this.sut.Search("fo* te*");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void SearchingWithinFieldsShouldObeyTokenizationOptionsForFields()
        {
            this.WithIndexedSingleStringPropertyObjects();

            this.sut.Search("Text1=one").Should().HaveCount(0);
            this.sut.Search("Text1=One").Should().HaveCount(2);

            this.sut.Search("Text3=summer").Should().HaveCount(1);
            this.sut.Search("Text3=summers").Should().HaveCount(1);
            this.sut.Search("Text3=drum").Should().HaveCount(1);
            this.sut.Search("Text3=drumming").Should().HaveCount(1);
            this.sut.Search("Text3=drums").Should().HaveCount(1);
        }

        [Fact]
        public void IndexedObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.WithIndexedSingleStringPropertyObjects();

            this.sut.Search("two").Should().HaveCount(2);
            this.sut.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void IndexingIndividuallyShouldResultInSameIndexStructureWhenIndexedAsRange()
        {
            var words = new[]
            {
                "ITIVI",
                "ILSOU",
                "ILBA",
                "ILB"
            };

            var builder = new FullTextIndexBuilder<string>().WithItemTokenization<string>(o => o.WithKey(s => s).WithField("Text", s => s));
            var indexSingle = builder.Build();
            var indexRange = builder.Build();

            indexRange.AddRange(words);
            foreach (var word in words)
            {
                indexSingle.Add(word);
            }

            indexSingle.ToString().Should().Be(indexRange.ToString());
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByObjectsShouldBeAssociatedToCorrectFields()
        {
            this.WithIndexedSingleStringPropertyObjects();

            var results = this.sut.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text1")).Should().BeTrue();
            results = this.sut.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text2")).Should().BeTrue();
            results = this.sut.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text3")).Should().BeTrue();
        }

        [Fact]
        public async Task IndexedAsyncFieldsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            await this.WithIndexedAsyncFieldObjectsAsync();

            this.sut.Search("text").Should().HaveCount(1);
            this.sut.Search("one").Should().HaveCount(2);
            this.sut.Search("two").Should().HaveCount(2);
            this.sut.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void IndexingFieldWithoutAsyncWhenAsyncRequired_ShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(new AsyncTestObject("1", "!", "11")));
        }

        [Fact]
        public void IndexedMultiStringPropertyObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.WithIndexedMultiStringPropertyObjects();

            this.sut.Search("text").Should().HaveCount(1);
            this.sut.Search("one").Should().HaveCount(2);
            this.sut.Search("two").Should().HaveCount(2);
            this.sut.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByMultiStringPropertyObjectsShouldBeAssociatedToCorrectFields()
        {
            this.WithIndexedMultiStringPropertyObjects();

            var results = this.sut.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.sut.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.sut.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
        }

        [Fact]
        public void RemovingItemFromIndex_ShouldMakeItUnsearchable()
        {
            this.WithIndexedStrings();

            this.sut.Search("foo").Should().HaveCount(1);

            this.sut.Remove("C").Should().BeTrue();

            this.sut.Search("foo").Should().HaveCount(0);
        }

        [Fact]
        public void RemovingItemFromIndexThatIsntIndexed_ShouldReturnFalse()
        {
            this.WithIndexedStrings();

            this.sut.Remove("Z").Should().BeFalse();
            this.sut.Remove("C").Should().BeTrue();
            this.sut.Remove("C").Should().BeFalse();
        }

        [Fact]
        public void WhenLoadingLotsOfDataShouldNotError()
        {
            var wikipediaTests = WikipediaDataLoader.Load(this.GetType());
            foreach (var (name, text) in wikipediaTests)
            {
                this.sut.Add(name, text);
            }

            this.sut.Remove(wikipediaTests[10].name);
            this.sut.Remove(wikipediaTests[9].name);
            this.sut.Remove(wikipediaTests[8].name);
        }

        [Fact]
        public void ConvertingIndexToString_ShouldReturnNodeStructureFormattedAsText()
        {
            this.WithIndexedSingleStringPropertyObjects();
            this.sut.ToString().Should().Be(@"
  T 
    e 
      x 
        t  [1 matche(s)]
    E 
      X 
        T  [1 matche(s)]
    W 
      O  [2 matche(s)]
    H 
      R 
        E E [2 matche(s)]
  O 
    n 
      e  [2 matche(s)]
  D 
    R 
      U 
        M  [1 matche(s)]
  N 
    o 
      t  [1 matche(s)]
    O 
      T  [1 matche(s)]
  S 
    U 
      M 
        M ER [1 matche(s)]");
        }
    }
}
