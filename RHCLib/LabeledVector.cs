using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public class LabeledVector<L> : Vector
    {
        public LabeledVector(L label, IEnumerable<double> features) : base(features)
        {
            this.Label = label;
        }

        public LabeledVector(L label, params double[] features)
            : this(label, (IEnumerable<double>)features)
        {

        }

        public LabeledVector(L label, IVector vector)
            : this(label, vector.Features)
        {

        }

        public override bool Equals(object obj)
        {
            LabeledVector<L> lvector;
            if ((lvector = obj as LabeledVector<L>) != null && ((this.Label == null && lvector.Label == null) || (this.Label != null && this.Label.Equals(lvector.Label))))
            {
                return base.Equals(lvector);
            }
            else
            {
                return false;
            }
        }

        public virtual bool EqualsWithLabelBeingInvariant(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static IEnumerable<L> GetUniqueLabels(IEnumerable<LabeledVector<L>> vectors)
        {
            return (from v in vectors
                    select v.Label).Distinct();
        }

        public IEnumerable<LabeledVector<L>> GetVectorsWithDifferentLabel(IEnumerable<LabeledVector<L>> vectors)
        {
            return LabeledVector<L>.GetVectorsWithoutLabel(vectors, this.Label);
        }

        public static IEnumerable<LabeledVector<L>> GetVectorsWithLabel(IEnumerable<LabeledVector<L>> vectors, L label)
        {
            return from vector in vectors
                   where vector.Label.Equals(label)
                   select vector;
        }

        public static IEnumerable<LabeledVector<L>> GetVectorsWithoutLabel(IEnumerable<LabeledVector<L>> vectors, L label)
        {
            return from vector in vectors
                   where !vector.Label.Equals(label)
                   select vector;
        }

        public IEnumerable<LabeledVector<L>> GetVectorsWithSameLabel(IEnumerable<LabeledVector<L>> vectors)
        {
            return LabeledVector<L>.GetVectorsWithLabel(vectors, this.Label);
        }

        public override void Print()
        {
            StringBuilder sb = new StringBuilder("[");

            for (int i = 0; i < this.Rank - 1; i++)
            {
                sb.Append(string.Format("{0}, ", this[i]));
            }
            sb.Append(string.Format("{0}] --- {1}", this[this.Rank - 1], this.Label));

            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }

        public static IDictionary<L, IEnumerable<LabeledVector<L>>> SeparateIntoClasses(IEnumerable<LabeledVector<L>> vectors)
        {
            return (from v in vectors
                    group v by v.Label into g
                    orderby g.Key
                    select g).ToDictionary(g => g.Key, g => g.Select(vec => vec).ToList() as IEnumerable<LabeledVector<L>>);
        }

        public L Label { get; set; }
    }
}
