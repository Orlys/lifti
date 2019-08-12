﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti
{
    public struct IndexedWordLocation
    {
        public IndexedWordLocation(byte fieldId, IReadOnlyList<Range> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public byte FieldId { get; }
        public IReadOnlyList<Range> Locations { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedWordLocation location &&
                   this.FieldId == location.FieldId &&
                   this.Locations.SequenceEqual(location.Locations);
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(this.FieldId);
            foreach (var location in this.Locations)
            {
                hashCode = HashCode.Combine(hashCode, location);
            }

            return hashCode;
        }
    }

    public class IndexNode
    {
        private Dictionary<int, List<IndexedWordLocation>> matches;
        private Dictionary<char, IndexNode> childNodes;
        private readonly IIndexNodeFactory indexNodeFactory;

        internal char[] IntraNodeText { get; private set; }
        internal IReadOnlyDictionary<char, IndexNode> ChildNodes => this.childNodes;
        internal IReadOnlyDictionary<int, List<IndexedWordLocation>> Matches => this.matches;

        public IndexNode(IIndexNodeFactory indexNodeFactory)
        {
            this.indexNodeFactory = indexNodeFactory;
        }

        public void Index(int itemId, byte fieldId, Token word)
        {
            this.Index(itemId, fieldId, word.Locations, word.Token);
        }

        private void Index(int itemId, byte fieldId, IReadOnlyList<Range> locations, ReadOnlySpan<char> remainingWordText)
        {
            if (this.IntraNodeText == null)
            {
                if (this.childNodes == null && this.matches == null)
                {
                    // Currently a leaf node
                    this.IntraNodeText = remainingWordText.Length == 0 ? null : remainingWordText.ToArray();
                    this.AddMatchedItem(itemId, fieldId, locations);
                }
                else
                {
                    if (remainingWordText.Length == 0)
                    {
                        this.AddMatchedItem(itemId, fieldId, locations);
                    }
                    else
                    {
                        this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, 0);
                    }
                }
            }
            else
            {
                if (remainingWordText.Length == 0)
                {
                    throw new InvalidOperationException("Remaining word text should not be empty at a leaf node that is not empty");
                }

                var testLength = Math.Min(remainingWordText.Length, this.IntraNodeText.Length);
                for (var i = 0; i < testLength; i++)
                {
                    if (remainingWordText[i] != this.IntraNodeText[i])
                    {
                        this.SplitIntraNodeText(i);
                        this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, i);
                        return;
                    }
                }

                // No split occurred
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
        }

        private void ContinueIndexingAtChild(int itemId, byte fieldId, IReadOnlyList<Range> locations, ReadOnlySpan<char> remainingWordText, int remainingTextSplitPosition)
        {
            this.EnsureChildNodesLookupCreated();

            var indexChar = remainingWordText[remainingTextSplitPosition];
            if (!this.childNodes.TryGetValue(indexChar, out var childNode))
            {
                childNode = this.indexNodeFactory.CreateNode();
                this.childNodes.Add(indexChar, childNode);
            }

            childNode.Index(itemId, fieldId, locations, remainingWordText.Slice(remainingTextSplitPosition + 1));
        }

        private void EnsureChildNodesLookupCreated()
        {
            if (this.childNodes == null)
            {
                this.childNodes = new Dictionary<char, IndexNode>();
            }
        }

        private void SplitIntraNodeText(int splitIndex)
        {
            var intraTextSpan = this.IntraNodeText.AsSpan();
            var splitChildNode = this.indexNodeFactory.CreateNode();
            splitChildNode.IntraNodeText = splitIndex + 1 == intraTextSpan.Length ? null : intraTextSpan.Slice(splitIndex + 1).ToArray();
            splitChildNode.childNodes = this.childNodes;
            splitChildNode.matches = this.matches;
            this.matches = null;
            this.childNodes = new Dictionary<char, IndexNode>();

            if (splitIndex == 0)
            {
                this.IntraNodeText = null;
            }
            else
            {
                this.IntraNodeText = intraTextSpan.Slice(0, splitIndex).ToArray();
            }

            this.childNodes.Add(intraTextSpan[splitIndex], splitChildNode);
        }

        private void AddMatchedItem(int itemId, byte fieldId, IReadOnlyList<Range> locations)
        {
            if (this.matches == null)
            {
                this.matches = new Dictionary<int, List<IndexedWordLocation>>();
            }

            if (!this.matches.TryGetValue(itemId, out var itemFieldLocations))
            {
                itemFieldLocations = new List<IndexedWordLocation>(); // TODO Constrain list to size == number of fields for index?
                this.matches[itemId] = itemFieldLocations;
            }

            itemFieldLocations.Add(new IndexedWordLocation(fieldId, locations));
        }

        public override string ToString()
        {
            if (this.childNodes == null && this.matches == null)
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
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.childNodes != null)
            {
                var nextDepth = currentDepth + 1;
                foreach (var item in this.childNodes)
                {
                    builder.AppendLine();
                    item.Value.ToString(builder, item.Key, nextDepth);
                }
            }
        }

        private void FormatNodeText(StringBuilder builder)
        {
            if (this.IntraNodeText != null)
            {
                builder.Append(this.IntraNodeText);
            }

            if (this.matches != null)
            {
                builder.Append(" [").Append(this.matches.Count).Append(" matche(s)]");
            }
        }
    }
}
