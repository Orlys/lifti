﻿using System;
using System.Globalization;

namespace Lifti.Querying.QueryParts
{
    public class NearQueryOperator : BinaryQueryOperator
    {
        public NearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)
            : base(left, right)
        {
            this.Tolerance = tolerance;
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.Positional;

        public int Tolerance { get; }

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Left.Evaluate(navigatorCreator).PositionalIntersectAndCombine(this.Right.Evaluate(navigatorCreator), this.Tolerance, this.Tolerance);
        }

        public override string ToString()
        {
            var toleranceText = this.Tolerance == 5 ? string.Empty : this.Tolerance.ToString(CultureInfo.InvariantCulture);
            return $"{this.Left} ~{toleranceText} {this.Right}";
        }
    }
}
