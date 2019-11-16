using Lifti.ItemTokenization;
using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti
{
    public class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;
        private readonly TokenizationOptions defaultTokenizationOptions;
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;
        private readonly IdPool<TKey> idPool;

        internal FullTextIndex(
            ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions,
            IIndexNodeFactory indexNodeFactory,
            ITokenizerFactory tokenizerFactory,
            IQueryParser queryParser,
            TokenizationOptions defaultTokenizationOptions)
        {
            this.itemTokenizationOptions = itemTokenizationOptions ?? throw new ArgumentNullException(nameof(itemTokenizationOptions));
            this.IndexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
            this.queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
            this.defaultTokenizationOptions = defaultTokenizationOptions ?? throw new ArgumentNullException(nameof(defaultTokenizationOptions));

            this.idPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup(
                this.itemTokenizationOptions.GetAllConfiguredFields(),
                tokenizerFactory, 
                defaultTokenizationOptions);

            this.Root = this.IndexNodeFactory.CreateEmptyNode();
        }

        internal IIndexNodeFactory IndexNodeFactory { get; }
        internal IndexNode Root { get; set; }

        public IIdLookup<TKey> IdLookup => this.idPool;

        internal IIdPool<TKey> IdPool => this.idPool;

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.IdLookup.Count;

        public IIndexNavigator CreateNavigator()
        {
            return new IndexNavigator(this.Root);
        }

        public void Add(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null)
        {
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, this.FieldLookup.DefaultField, word);
            }
        }

        public void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            var newRoot = this.IndexNodeFactory.CloneNode(this.Root);
            foreach (var word in tokenizer.Process(text))
            {
                newRoot.Index(itemId, this.FieldLookup.DefaultField, word);
            }

            this.Root = newRoot;
        }

        public void AddRange<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            this.ReplaceRoot(r =>
            {
                foreach (var item in items)
                {
                    this.Add(r, item);
                }
            });
        }

        public void Add<TItem>(TItem item)
        {
            this.ReplaceRoot(r => this.Add(r, item));
        }

        public async ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            await this.ReplaceRootAsync(async r =>
            {
                foreach (var item in items)
                {
                    await this.AddAsync(r, item);
                }
            }).ConfigureAwait(false);
        }

        public async ValueTask AddAsync<TItem>(TItem item)
        {
            await this.ReplaceRootAsync(async r => await this.AddAsync(r, item))
                .ConfigureAwait(false);
        }

        public bool Remove(TKey itemKey)
        {
            if (!this.idPool.Contains(itemKey))
            {
                return false;
            }

            var id = this.idPool.ReleaseItem(itemKey);
            this.Root.Remove(id);

            return true;
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null)
        {
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }

        private void ReplaceRoot(Action<IndexNode> indexMutation)
        {
            var newRoot = this.IndexNodeFactory.CloneNode(this.Root);
            indexMutation(newRoot);
            this.Root = newRoot;
        }

        private async Task ReplaceRootAsync(Func<IndexNode, Task> asyncIndexMutation)
        {
            var newRoot = this.IndexNodeFactory.CloneNode(this.Root);
            await asyncIndexMutation(newRoot).ConfigureAwait(false);
            this.Root = newRoot;
        }

        private async ValueTask AddAsync<TItem>(IndexNode root, TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();

            var itemKey = options.KeyReader(item);
            var itemId = this.idPool.Add(itemKey);

            foreach (var field in options.FieldTokenization)
            {
                var (fieldId, tokenizer) = this.FieldLookup.GetFieldInfo(field.Name);
                var tokens = await field.TokenizeAsync(tokenizer, item);

                foreach (var word in tokens)
                {
                    root.Index(itemId, fieldId, word);
                }
            }
        }

        private void Add<TItem>(IndexNode root, TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();

            var itemKey = options.KeyReader(item);
            var itemId = this.idPool.Add(itemKey);

            foreach (var field in options.FieldTokenization)
            {
                var (fieldId, tokenizer) = this.FieldLookup.GetFieldInfo(field.Name);
                var tokens = field.Tokenize(tokenizer, item);

                foreach (var word in tokens)
                {
                    root.Index(itemId, fieldId, word);
                }
            }
        }

        private ITokenizer GetTokenizer(TokenizationOptions tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? this.defaultTokenizationOptions);
        }
    }
}
