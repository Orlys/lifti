using FluentAssertions;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexImmutabilityTests : FullTextIndexTestBase
    {
        public FullTextIndexImmutabilityTests()
        {
            this.sut.Add("A", "This is a test");
        }

        [Fact]
        public void Adding_Text()
        {
            var oldRoot = this.sut.Root;
            this.sut.Add("B", "Other test");
            var newRoot = this.sut.Root;

            oldRoot.Should().NotBe(newRoot);
            oldRoot.ChildNodes.Should().NotBeSameAs(newRoot.ChildNodes);
            oldRoot.Matches.Should().BeNull();
            newRoot.Matches.Should().BeNull();

            // 'I' path was not modified and should be preserved
            oldRoot.ChildNodes['I'].Should().BeSameAs(newRoot.ChildNodes['I']);

            // 'T' path was modified and should have been cloned
            oldRoot.ChildNodes['T'].Should().NotBeSameAs(newRoot.ChildNodes['T']);
        }
    }
}
