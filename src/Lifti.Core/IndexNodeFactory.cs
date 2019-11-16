using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    public class IndexNodeFactory : ConfiguredBy<AdvancedOptions>, IIndexNodeFactory
    {
        private int supportIntraNodeTextAtDepth;

        public IndexNode CreateEmptyNode()
        {
            return new IndexNode(this, null, null, null);
        }

        public IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth)
        {
            return depth >= this.supportIntraNodeTextAtDepth ?
                IndexSupportLevelKind.IntraNodeText :
                IndexSupportLevelKind.CharacterByCharacter;
        }

        public IndexNode CloneNode(IndexNode original)
        {
            if (original is null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            return this.CreateNode(original.IntraNodeText, original.ChildNodes, original.Matches);
        }

        protected override void OnConfiguring(AdvancedOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.supportIntraNodeTextAtDepth = options.SupportIntraNodeTextAfterIndexDepth;
        }

        public IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            IDictionary<char, IndexNode> childNodes,
            IDictionary<int, ImmutableList<IndexedWord>> matches)
        {
            return new IndexNode(this, intraNodeText, childNodes, matches);
        }
    }
}
