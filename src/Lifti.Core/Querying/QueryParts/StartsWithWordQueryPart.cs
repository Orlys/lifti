﻿using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part that matches items that are indexed starting with given text.
    /// </summary>
    public class StartsWithWordQueryPart : WordQueryPart
    {
        public StartsWithWordQueryPart(string word)
            : base(word)
        {
        }

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using (var navigator = navigatorCreator())
            {
                navigator.Process(this.Word.AsSpan());
                return queryContext.ApplyTo(navigator.GetExactAndChildMatches());
            }
        }

        public override string ToString()
        {
            return $"{this.Word}*";
        }
    }
}
