using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lifti
{
    public class IndexNode
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private HashSet<char> mutatedChildren;

        internal IndexNode(
            IIndexNodeFactory indexNodeFactory, 
            ReadOnlyMemory<char> intraNodeText,
            IDictionary<char, IndexNode> childNodes, 
            IDictionary<int, ImmutableList<IndexedWord>> matches)
        {
            this.indexNodeFactory = indexNodeFactory;
            this.IntraNodeText = intraNodeText;
            this.ChildNodes = childNodes;
            this.Matches = matches;
        }

        internal ReadOnlyMemory<char> IntraNodeText { get; private set; }
        internal IDictionary<char, IndexNode> ChildNodes { get; private set; }
        internal IDictionary<int, ImmutableList<IndexedWord>> Matches { get; private set; }

        internal void Index(int itemId, byte fieldId, Token word)
        {
            if (word is null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Debug.Assert(word.Locations.Select((l, i) => i == 0 || l.WordIndex > word.Locations[i - 1].WordIndex).All(v => v));

            this.Index(0, itemId, fieldId, word.Locations, word.Value.AsMemory());
        }

        internal void AddMatchedItem(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations)
        {
            if (this.Matches == null)
            {
                this.Matches = new Dictionary<int, ImmutableList<IndexedWord>>();
            }

            if (!this.Matches.TryGetValue(itemId, out var itemFieldLocations))
            {
                itemFieldLocations = ImmutableList<IndexedWord>.Empty;
            }

            itemFieldLocations = itemFieldLocations.Add(new IndexedWord(fieldId, locations));
            this.Matches[itemId] = itemFieldLocations;
            
        }

        private void Index(int depth, int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlyMemory<char> remainingWordText)
        {
            var indexSupportLevel = this.indexNodeFactory.GetIndexSupportLevelForDepth(depth);
            switch (indexSupportLevel)
            {
                case IndexSupportLevelKind.CharacterByCharacter:
                    this.IndexFromCharacter(depth, itemId, fieldId, locations, remainingWordText);
                    break;
                case IndexSupportLevelKind.IntraNodeText:
                    this.IndexWithIntraNodeTextSupport(depth, itemId, fieldId, locations, remainingWordText);
                    break;
                default:
                    throw new LiftiException(ExceptionMessages.UnsupportedIndexSupportLevel, indexSupportLevel);
            }

        }

        internal void Remove(int itemId)
        {
            this.Matches?.Remove(itemId);
            if (this.ChildNodes != null)
            {
                foreach (var childNode in this.ChildNodes.Values)
                {
                    childNode.Remove(itemId);
                }
            }
        }

        public override string ToString()
        {
            if (!(this.ChildNodes?.Count > 0 || this.Matches?.Count > 0))
            {
                return "<EMPTY>";
            }

            var builder = new StringBuilder();
            this.FormatNodeText(builder);
            this.FormatChildNodeText(builder, 0);

            return builder.ToString();
        }

        private void IndexWithIntraNodeTextSupport(int currentDepth, int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlyMemory<char> remainingWordText)
        {
            if (this.IntraNodeText.Length == 0)
            {
                if (this.ChildNodes == null && this.Matches == null)
                {
                    // Currently a leaf node
                    this.IntraNodeText = remainingWordText.Length == 0 ? null : remainingWordText;
                    this.AddMatchedItem(itemId, fieldId, locations);
                }
                else
                {
                    this.IndexFromCharacter(currentDepth, itemId, fieldId, locations, remainingWordText);
                }
            }
            else
            {
                if (remainingWordText.Length == 0)
                {
                    // The indexing ends before the start of the intranode text so we need to split
                    this.SplitIntraNodeText(0);
                    this.AddMatchedItem(itemId, fieldId, locations);
                    return;
                }

                var testLength = Math.Min(remainingWordText.Length, this.IntraNodeText.Length);
                var intraNodeText = this.IntraNodeText.Span;
                var wordSpan = remainingWordText.Span;
                for (var i = 0; i < testLength; i++)
                {
                    if (wordSpan[i] != intraNodeText[i])
                    {
                        this.SplitIntraNodeText(i);
                        this.ContinueIndexingAtChild(currentDepth, itemId, fieldId, locations, remainingWordText, i);
                        return;
                    }
                }

                if (this.IntraNodeText.Length > testLength)
                {
                    // This word is indexed in the middle of intra-node text. Split it and index here
                    this.SplitIntraNodeText(testLength);
                }

                this.IndexFromCharacter(currentDepth, itemId, fieldId, locations, remainingWordText, testLength);
            }
        }

        private void IndexFromCharacter(int currentDepth, int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlyMemory<char> remainingWordText, int testLength = 0)
        {
            if (remainingWordText.Length > testLength)
            {
                this.ContinueIndexingAtChild(currentDepth, itemId, fieldId, locations, remainingWordText, testLength);
            }
            else
            {
                // Remaining text == intraNodeText
                this.AddMatchedItem(itemId, fieldId, locations);
            }
        }

        private IndexNode CreateOrMutateChildNode(char indexChar)
        {
            this.EnsureChildNodesLookupCreated();
            if (this.ChildNodes.TryGetValue(indexChar, out var childNode))
            {
                if (this.mutatedChildren.Add(indexChar))
                {
                    // Clone the child node - this is the first mutation
                    childNode = this.indexNodeFactory.CloneNode(childNode);
                    this.ChildNodes[indexChar] = childNode;
                }
            }
            else
            {
                childNode = this.indexNodeFactory.CreateEmptyNode();
                this.mutatedChildren.Add(indexChar);
                this.ChildNodes.Add(indexChar, childNode);
            }

            return childNode;
        }

        private void ContinueIndexingAtChild(int currentDepth, int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlyMemory<char> remainingWordText, int remainingTextSplitPosition)
        {
            var indexChar = remainingWordText.Span[remainingTextSplitPosition];

            this.CreateOrMutateChildNode(indexChar)
                .Index(currentDepth + 1, itemId, fieldId, locations, remainingWordText.Slice(remainingTextSplitPosition + 1));
        }

        private void EnsureChildNodesLookupCreated()
        {
            if (this.ChildNodes == null)
            {
                this.ChildNodes = new Dictionary<char, IndexNode>();
            }

            if (this.mutatedChildren == null)
            {
                this.mutatedChildren = new HashSet<char>();
            }
        }

        private void SplitIntraNodeText(int splitIndex)
        {
            var splitChildNode = this.indexNodeFactory.CreateNode(
                splitIndex + 1 == this.IntraNodeText.Length ? null : this.IntraNodeText.Slice(splitIndex + 1),
                this.ChildNodes,
                this.Matches);

            splitChildNode.mutatedChildren = this.mutatedChildren;

            this.Matches = null;
            this.ChildNodes = new Dictionary<char, IndexNode>();

            var splitChar = this.IntraNodeText.Span[splitIndex];
            this.mutatedChildren = new HashSet<char> { splitChar };
            if (splitIndex == 0)
            {
                this.IntraNodeText = null;
            }
            else
            {
                this.IntraNodeText = this.IntraNodeText.Slice(0, splitIndex);
            }

            this.ChildNodes.Add(splitChar, splitChildNode);
        }

        private void ToString(StringBuilder builder, char linkChar, int currentDepth)
        {
            builder.Append(' ', currentDepth * 2)
                .Append(linkChar)
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.ChildNodes != null)
            {
                var nextDepth = currentDepth + 1;
                foreach (var item in this.ChildNodes)
                {
                    builder.AppendLine();
                    item.Value.ToString(builder, item.Key, nextDepth);
                }
            }
        }

        private void FormatNodeText(StringBuilder builder)
        {
            if (this.IntraNodeText.Length > 0)
            {
                builder.Append(this.IntraNodeText);
            }

            if (this.Matches != null)
            {
                builder.Append(" [").Append(this.Matches.Count).Append(" matche(s)]");
            }
        }
    }
}
