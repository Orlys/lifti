﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Lifti
{
    internal class IndexNodeMutation
    {
        private readonly int depth;
        private readonly IIndexNodeFactory indexNodeFactory;
        private IndexNode? original;

        private IndexNodeMutation(IndexNodeMutation parent)
            : this(parent.depth + 1, parent.indexNodeFactory)
        {
        }

        private IndexNodeMutation(int depth, IIndexNodeFactory indexNodeFactory)
        {
            this.depth = depth;
            this.indexNodeFactory = indexNodeFactory;
        }

        public IndexNodeMutation(int depth, IndexNode node, IIndexNodeFactory indexNodeFactory)
            : this(depth, indexNodeFactory)
        {
            this.original = node;
            this.IntraNodeText = node.IntraNodeText;

            this.HasMatches = node.HasMatches;
            this.HasChildNodes = node.HasChildNodes;
        }

        public bool IsEmpty => !this.HasChildNodes && !this.HasMatches;
        public bool HasChildNodes { get; private set; }
        public bool HasMatches { get; private set; }
        public ReadOnlyMemory<char> IntraNodeText { get; private set; }
        public Dictionary<char, IndexNodeMutation>? MutatedChildNodes { get; private set; }

        public IEnumerable<KeyValuePair<char, IndexNode>> UnmutatedChildNodes
        {
            get
            {
                if (this.original == null)
                {
                    return Array.Empty<KeyValuePair<char, IndexNode>>();
                }

                if (this.MutatedChildNodes == null)
                {
                    return this.original.ChildNodes;
                }

                return this.original.ChildNodes.Where(n => !this.MutatedChildNodes.ContainsKey(n.Key));
            }
        }

        public Dictionary<int, ImmutableList<IndexedWord>>? MutatedMatches { get; private set; }

        internal void Index(
            int itemId,
            byte fieldId,
            IReadOnlyList<WordLocation> locations,
            ReadOnlyMemory<char> remainingWordText)
        {
            var indexSupportLevel = this.indexNodeFactory.GetIndexSupportLevelForDepth(this.depth);
            switch (indexSupportLevel)
            {
                case IndexSupportLevelKind.CharacterByCharacter:
                    this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText);
                    break;
                case IndexSupportLevelKind.IntraNodeText:
                    this.IndexWithIntraNodeTextSupport(itemId, fieldId, locations, remainingWordText);
                    break;
                default:
                    throw new LiftiException(ExceptionMessages.UnsupportedIndexSupportLevel, indexSupportLevel);
            }
        }

        internal IndexNode Apply()
        {
            ImmutableDictionary<char, IndexNode> childNodes;
            ImmutableDictionary<int, ImmutableList<IndexedWord>> matches;

            IEnumerable<KeyValuePair<char, IndexNode>> mapNodeMutations()
            {
                return this.MutatedChildNodes.Select(p => new KeyValuePair<char, IndexNode>(p.Key, p.Value.Apply()));
            }

            if (this.original == null)
            {
                childNodes = this.MutatedChildNodes == null ? ImmutableDictionary<char, IndexNode>.Empty : mapNodeMutations().ToImmutableDictionary();
                matches = this.MutatedMatches == null ? ImmutableDictionary<int, ImmutableList<IndexedWord>>.Empty : this.MutatedMatches.ToImmutableDictionary();
            }
            else
            {
                childNodes = this.original.ChildNodes;
                if (this.MutatedChildNodes?.Count > 0)
                {
                    childNodes = childNodes.SetItems(mapNodeMutations());
                }

                if (this.MutatedMatches == null)
                {
                    matches = this.original.Matches;
                }
                else
                {
                    matches = this.MutatedMatches.ToImmutableDictionary();
                }
            }

            return this.indexNodeFactory.CreateNode(this.IntraNodeText, childNodes, matches);
        }

        internal void Remove(int itemId)
        {
            if (this.HasChildNodes)
            {
                // First look through any already mutated child nodes
                if (this.MutatedChildNodes != null)
                {
                    foreach (var child in this.MutatedChildNodes)
                    {
                        child.Value.Remove(itemId);
                    }
                }

                // Then any unmutated children
                foreach (var child in this.UnmutatedChildNodes)
                {
                    if (this.TryRemove(child.Value, itemId, this.depth + 1, out var mutatedChild))
                    {
                        this.EnsureMutatedChildNodesCreated();
                        this.MutatedChildNodes!.Add(child.Key, mutatedChild);
                    }
                }
            }

            if (this.HasMatches)
            {
                if (this.MutatedMatches != null)
                {
                    this.MutatedMatches.Remove(itemId);
                }
                else
                {
                    if (this.original != null && this.original.Matches.ContainsKey(itemId))
                    {
                        // Mutate and remove
                        this.EnsureMutatedMatchesCreated();
                        this.MutatedMatches!.Remove(itemId);
                    }
                }
            }
        }

        private bool TryRemove(IndexNode node, int itemId, int nodeDepth, [NotNullWhen(true)]out IndexNodeMutation? mutatedNode)
        {
            mutatedNode = null;

            if (node.HasChildNodes)
            {
                // Work through the child nodes and recursively determine whether removals are needed from 
                // them. If they are, then this instance will also become mutated.
                foreach (var child in node.ChildNodes)
                {
                    if (this.TryRemove(child.Value, itemId, nodeDepth + 1, out var mutatedChild))
                    {
                        if (mutatedNode == null)
                        {
                            mutatedNode = new IndexNodeMutation(nodeDepth, node, this.indexNodeFactory);
                            mutatedNode.EnsureMutatedChildNodesCreated();
                        }

                        mutatedNode.MutatedChildNodes!.Add(child.Key, mutatedChild);
                    }
                }
            }

            if (node.HasMatches)
            {
                // Removing an item from the nodes current matches will return the same dictionary
                // if the item didn't exist - this removes the need for an extra Exists check
                var mutatedMatches = node.Matches.Remove(itemId);
                if (mutatedMatches != node.Matches)
                {
                    if (mutatedNode == null)
                    {
                        mutatedNode = new IndexNodeMutation(nodeDepth, node, this.indexNodeFactory);
                    }

                    mutatedNode.EnsureMutatedMatchesCreated();
                    mutatedNode.MutatedMatches!.Remove(itemId);
                }
            }

            return mutatedNode != null;
        }

        private void IndexFromCharacter(
            int itemId,
            byte fieldId,
            IReadOnlyList<WordLocation> locations,
            ReadOnlyMemory<char> remainingWordText,
            int testLength = 0)
        {
            if (remainingWordText.Length > testLength)
            {
                this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, testLength);
            }
            else
            {
                // Remaining text == intraNodeText
                this.AddMatchedItem(itemId, fieldId, locations);
            }
        }

        private void ContinueIndexingAtChild(
            int itemId,
            byte fieldId,
            IReadOnlyList<WordLocation> locations,
            ReadOnlyMemory<char> remainingWordText,
            int remainingTextSplitPosition)
        {
            var indexChar = remainingWordText.Span[remainingTextSplitPosition];

            this.EnsureMutatedChildNodesCreated();
            if (!this.MutatedChildNodes!.TryGetValue(indexChar, out var childNode))
            {
                if (this.original != null && this.original.ChildNodes.TryGetValue(indexChar, out var originalChildNode))
                {
                    // the original had an unmutated child node that matched the index character - mutate it now
                    childNode = new IndexNodeMutation(this.depth + 1, originalChildNode, this.indexNodeFactory);
                }
                else
                {
                    // This is a novel branch in the index
                    childNode = new IndexNodeMutation(this);
                }

                // Track the mutated node
                this.MutatedChildNodes.Add(indexChar, childNode);
            }

            childNode.Index(itemId, fieldId, locations, remainingWordText.Slice(remainingTextSplitPosition + 1));
        }

        private void EnsureMutatedChildNodesCreated()
        {
            if (this.MutatedChildNodes == null)
            {
                this.HasChildNodes = true;
                this.MutatedChildNodes = new Dictionary<char, IndexNodeMutation>();
            }
        }

        private void IndexWithIntraNodeTextSupport(
            int itemId,
            byte fieldId,
            IReadOnlyList<WordLocation> locations,
            ReadOnlyMemory<char> remainingWordText)
        {
            if (this.IntraNodeText.Length == 0)
            {
                if (this.IsEmpty)
                {
                    // Currently a leaf node
                    this.IntraNodeText = remainingWordText.Length == 0 ? null : remainingWordText;
                    this.AddMatchedItem(itemId, fieldId, locations);
                }
                else
                {
                    this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText);
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
                var intraNodeSpan = this.IntraNodeText.Span;
                var wordSpan = remainingWordText.Span;
                for (var i = 0; i < testLength; i++)
                {
                    if (wordSpan[i] != intraNodeSpan[i])
                    {
                        this.SplitIntraNodeText(i);
                        this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, i);
                        return;
                    }
                }

                if (this.IntraNodeText.Length > testLength)
                {
                    // This word is indexed in the middle of intra-node text. Split it and index here
                    this.SplitIntraNodeText(testLength);
                }

                this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText, testLength);
            }
        }

        private void AddMatchedItem(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations)
        {
            this.EnsureMutatedMatchesCreated();

            var indexedWord = new IndexedWord(fieldId, locations);
            if (this.MutatedMatches!.TryGetValue(itemId, out var itemFieldLocations))
            {
                this.MutatedMatches[itemId] = itemFieldLocations.Add(new IndexedWord(fieldId, locations));
            }
            else
            {
                if (this.MutatedMatches.TryGetValue(itemId, out var originalItemFieldLocations))
                {
                    this.MutatedMatches[itemId] = originalItemFieldLocations.Add(indexedWord);
                }
                else
                {
                    // This item has not been indexed at this location previously
                    var builder = ImmutableList.CreateBuilder<IndexedWord>();
                    builder.Add(indexedWord);
                    this.MutatedMatches.Add(itemId, builder.ToImmutable());
                }
            }
        }

        private void EnsureMutatedMatchesCreated()
        {
            if (this.MutatedMatches == null)
            {
                this.HasMatches = true;

                if (this.original?.HasMatches ?? false)
                {
                    // Once we're mutating matches, copy everything across
                    this.MutatedMatches = new Dictionary<int, ImmutableList<IndexedWord>>(
                        this.original.Matches);
                }
                else
                {
                    this.MutatedMatches = new Dictionary<int, ImmutableList<IndexedWord>>();
                }
            }
        }

        private void SplitIntraNodeText(int splitIndex)
        {
            var splitChildNode = new IndexNodeMutation(this)
            {
                MutatedChildNodes = this.MutatedChildNodes,
                MutatedMatches = this.MutatedMatches,
                IntraNodeText = splitIndex + 1 == this.IntraNodeText.Length ? null : this.IntraNodeText.Slice(splitIndex + 1),

                // Pass the original down to the child node - the only state that matters there is any unmutated child nodes/matches
                original = this.original
            };

            this.original = null;

            var splitChar = this.IntraNodeText.Span[splitIndex];

            // Reset the matches at this node
            this.MutatedMatches = null;
            this.HasMatches = false;

            // Replace any remaining intra node text
            this.IntraNodeText = splitIndex == 0 ? null : this.IntraNodeText.Slice(0, splitIndex);

            this.HasChildNodes = true;
            this.MutatedChildNodes = new Dictionary<char, IndexNodeMutation>
            {
                { splitChar, splitChildNode }
            };
        }

        [Pure]
        public override string ToString()
        {
            if (this.IsEmpty)
            {
                return "<EMPTY>";
            }

            var builder = new StringBuilder();
            this.FormatNodeText(builder);
            this.FormatChildNodeText(builder, 0);

            return builder.ToString();
        }

        private void ToString(StringBuilder builder, char linkChar, int currentDepth)
        {
            builder.Append(' ', currentDepth * 2)
                .Append(linkChar)
                .Append('*')
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.HasChildNodes)
            {
                var nextDepth = currentDepth + 1;
                if (this.original != null)
                {
                    foreach (var item in this.original.ChildNodes.Where(e => this.MutatedChildNodes == null || !this.MutatedChildNodes.ContainsKey(e.Key)))
                    {
                        builder.AppendLine();
                        item.Value.ToString(builder, item.Key, nextDepth);
                    }
                }

                if (this.MutatedChildNodes != null)
                {
                    foreach (var item in this.MutatedChildNodes)
                    {
                        builder.AppendLine();
                        item.Value.ToString(builder, item.Key, nextDepth);
                    }
                }
            }
        }

        private void FormatNodeText(StringBuilder builder)
        {
            if (this.IntraNodeText.Length > 0)
            {
                builder.Append(this.IntraNodeText);
            }

            if (this.HasMatches)
            {
                builder.Append($" [{this.original?.Matches.Count ?? 0} original matche(s) - {this.MutatedMatches?.Count ?? 0} mutated]");
            }
        }
    }
}
