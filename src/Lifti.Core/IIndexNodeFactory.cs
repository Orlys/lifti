using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredBy<AdvancedOptions>
    {
        IndexNode CreateEmptyNode();
        IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth);
        IndexNode CloneNode(IndexNode original);
        IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            IDictionary<char, IndexNode> childNodes,
            IDictionary<int, ImmutableList<IndexedWord>> matches);
    }
}