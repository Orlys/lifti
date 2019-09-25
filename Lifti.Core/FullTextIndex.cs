﻿using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public partial class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;

        public FullTextIndex()
            : this(new FullTextIndexOptions<TKey>())
        {
        }

        public FullTextIndex(
            FullTextIndexOptions<TKey> options,
            IIndexNodeFactory indexNodeFactory = default,
            ITokenizerFactory tokenizerFactory = default,
            IQueryParser queryParser = default)
        {
            this.indexNodeFactory = indexNodeFactory ?? new IndexNodeFactory();
            this.tokenizerFactory = tokenizerFactory ?? new TokenizerFactory();
            this.queryParser = queryParser ?? new QueryParser();

            this.indexNodeFactory.Configure(options);

            this.IdPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup();
            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public IIdPool<TKey> IdPool { get; }

        public IIndexedFieldLookup FieldLookup { get; }

        public void Index(TKey itemKey, string text, TokenizationOptions? tokenizationOptions = default)
        {
            var itemId = this.IdPool.CreateIdFor(itemKey);

            var tokenizer = GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text.AsSpan()))
            {
                this.Root.Index(itemId, this.FieldLookup.DefaultField, word);
            }
        }

        public void Index<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> itemTokenizationOptions)
        {
            if (itemTokenizationOptions is null)
            {
                throw new ArgumentNullException(nameof(itemTokenizationOptions));
            }

            var itemKey = itemTokenizationOptions.KeyReader(item);
            var itemId = this.IdPool.CreateIdFor(itemKey);

            foreach (var field in itemTokenizationOptions.FieldTokenization)
            {
                var fieldId = this.FieldLookup.GetOrCreateIdForField(field.Name);
                var tokenizer = this.tokenizerFactory.Create(field.TokenizationOptions);
                foreach (var word in tokenizer.Process(field.Reader(item).AsSpan()))
                {
                    this.Root.Index(itemId, fieldId, word);
                }
            }
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions? tokenizationOptions = default)
        {
            var query = this.queryParser.Parse(searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this);
        }

        private ITokenizer GetTokenizer(TokenizationOptions? tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
        }
    }
}
