using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tests
{
    public abstract class FullTextIndexTestBase
    {
        protected readonly FullTextIndex<string> sut;

        protected FullTextIndexTestBase()
        {
            this.sut = new FullTextIndexBuilder<string>()
                .WithItemTokenization<TestObject>(
                    o => o.WithKey(i => i.Id)
                        .WithField("Text1", i => i.Text1, opts => opts.CaseInsensitive(false))
                        .WithField("Text2", i => i.Text2)
                        .WithField("Text3", i => i.Text3, opts => opts.WithStemming()))
                .WithItemTokenization<TestObject2>(
                    o => o.WithKey(i => i.Id)
                        .WithField("MultiText", i => i.Text))
                .WithItemTokenization<AsyncTestObject>(
                    o => o.WithKey(i => i.Id)
                        .WithField("TextAsync", i => Task.Run(() => i.Text))
                        .WithField("MultiTextAsync", i => Task.Run(() => (IEnumerable<string>)i.MultiText)))
                .Build();
        }

        protected void WithIndexedSingleStringPropertyObjects()
        {
            this.sut.Add(new TestObject("A", "Text One", "Text Two", "Text Three Drumming"));
            this.sut.Add(new TestObject("B", "Not One", "Not Two", "Not Three Summers"));
        }

        protected void WithIndexedMultiStringPropertyObjects()
        {
            this.sut.Add(new TestObject2("A", "Text One", "Text Two", "Text Three"));
            this.sut.Add(new TestObject2("B", "Not One", "Not Two", "Not Three"));
        }

        protected async Task WithIndexedAsyncFieldObjectsAsync()
        {
            await this.sut.AddAsync(new AsyncTestObject("A", "Text One", "Text Two", "Text Three"));
            await this.sut.AddAsync(new AsyncTestObject("B", "Not One", "Not Two", "Not Three"));
        }

        protected void WithIndexedStrings()
        {
            this.sut.Add("A", "This is a test");
            this.sut.Add("B", "This is another test");
            this.sut.Add("C", "Foo is testing this as well");
            this.sut.Add("D", new[] { "One last test just for testing sake", "with hypenated text: third-eye" });
        }

        public class TestObject
        {
            public TestObject(string id, string text1, string text2, string text3)
            {
                this.Id = id;
                this.Text1 = text1;
                this.Text2 = text2;
                this.Text3 = text3;
            }

            public string Id { get; }
            public string Text1 { get; }
            public string Text2 { get; }
            public string Text3 { get; }
        }

        public class TestObject2
        {
            public TestObject2(string id, params string[] text)
            {
                this.Id = id;
                this.Text = text;
            }

            public string Id { get; }
            public string[] Text { get; }
        }

        public class AsyncTestObject
        {
            public AsyncTestObject(string id, string text, params string[] multiText)
            {
                this.Id = id;
                this.Text = text;
                this.MultiText = multiText;
            }

            public string Id { get; }
            public string Text { get; }
            public string[] MultiText { get; }
        }
    }
}