using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public delegate double DistanceDelegate(IVector a, IVector b);
    public delegate Vector CentroidDelegate(IEnumerable<IVector> vectors);

    public class Vector : IVector, IEnumerable<double>
    {
        private static DistanceDelegate s_distEuclidean = null;
        private static DistanceDelegate s_distSqEuclidean = null;
        private static CentroidDelegate s_centroid = null;

        public Vector(IEnumerable<double> features)
        {
            this.Features = (double[])Array.CreateInstance(typeof(double), features.Count());
            Array.Copy(features.ToArray(), this.Features, this.Rank);
        }

        public Vector(params double[] features)
            : this((IEnumerable<double>)features)
        {

        }

        public Vector(IVector vector)
            : this(vector.Features)
        {

        }

        public override bool Equals(object obj)
        {
            IVector vector;
            if ((vector = obj as IVector) != null && this.Rank == vector.Rank)
            {
                for (int i = 0; i < this.Rank; i++)
                {
                    if (!this.GetFeature(i).Equals(vector.GetFeature(i)))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool EqualsEx(IVector a, IVector b)
        {
            if (a != null && b != null)
            {
                if (a.Rank == b.Rank)
                {
                    for (int i = 0; i < a.Rank; i++)
                    {
                        if (a.GetFeature(i) != b.GetFeature(i))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return a == null && b == null;
            }
        }

        public IVector GetClosestVector(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return (from v in vectors
                    orderby measure(this, v)
                    select v).FirstOrDefault();
        }

        public IEnumerator<double> GetEnumerator()
        {
            foreach (double feature in this.Features)
            {
                yield return feature;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.Features.GetEnumerator();
        }

        public double GetFeature(int nIndex)
        {
            return this.Features[nIndex];
        }

        public IVector GetFurthestVector(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return (from v in vectors
                    orderby measure(this, v) descending
                    select v).FirstOrDefault();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static void Normalize(IEnumerable<IVector> vectors)
        {
            if (vectors != null && vectors.Any())
            {
                IVector vectorFirst = vectors.ElementAt(0);

                double[] rgMin = new double[vectorFirst.Rank];
                double[] rgMax = new double[vectorFirst.Rank];
                double fFeature;

                bool bFirstIteration = true;
                foreach (Vector vector in vectors)
                {
                    if (vector.Rank.Equals(rgMin.Length))
                    {
                        for (int j = 0; j < vector.Rank; j++)
                        {
                            if (bFirstIteration)
                            {
                                rgMin[j] = vector.GetFeature(j);
                                rgMax[j] = vector.GetFeature(j);

                                bFirstIteration = false;
                            }
                            else
                            {
                                fFeature = vector.GetFeature(j);
                                if (fFeature < rgMin[j])
                                {
                                    rgMin[j] = fFeature;
                                }
                                if (fFeature > rgMax[j])
                                {
                                    rgMax[j] = fFeature;
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new RankException();
                    }
                }

                foreach (IVector v in vectors)
                {
                    for (int i = 0; i < v.Rank; i++)
                    {
                        v.SetFeature(i, (v.GetFeature(i) - rgMin[i]) / (rgMax[i] - rgMin[i]));
                    }
                }
            }
        }

        public virtual void Print()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < this.Rank - 1; i++)
            {
                sb.Append(string.Format("{0}, ", this[i]));
            }
            sb.Append(string.Format("{0}]", this[this.Rank - 1]));

            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }

        public void SetFeature(int nIndex, double value)
        {
            this.Features[nIndex] = value;
        }

        public double[] Features { get; private set; }

        public static CentroidDelegate Centroid
        {
            get
            {
                if (Vector.s_centroid == null)
                {
                    Vector.s_centroid = vectors =>
                        {
                            Vector centroid = null;

                            if (vectors != null && vectors.Any())
                            {
                                double[] rgFeatures = new double[vectors.ElementAt(0).Rank];
                                foreach (Vector v in vectors)
                                {
                                    if (rgFeatures.Length == v.Rank)
                                    {
                                        for (int j = 0; j < v.Rank; j++)
                                        {
                                            rgFeatures[j] += v.GetFeature(j);
                                        }
                                    }
                                    else
                                    {
                                        throw new RankException();
                                    }
                                }

                                int nCount = vectors.Count();
                                for (int i = 0; i < rgFeatures.Length; i++)
                                {
                                    rgFeatures[i] /= nCount;
                                }

                                centroid = new Vector(rgFeatures);
                            }

                            return centroid;
                        };
                }

                return Vector.s_centroid;
            }
        }

        public static DistanceDelegate EuclideanDistance
        {
            get
            {
                if (Vector.s_distEuclidean == null)
                {
                    Vector.s_distEuclidean = (a, b) =>
                        {
                            return Math.Sqrt(Vector.SquaredEuclideanDistance(a, b));
                        };
                }

                return Vector.s_distEuclidean;
            }
        }

        public int Rank
        {
            get
            {
                return this.Features.Length;
            }
        }

        public static DistanceDelegate SquaredEuclideanDistance
        {
            get
            {
                if (Vector.s_distSqEuclidean == null)
                {
                    Vector.s_distSqEuclidean = (a, b) =>
                        {
                            if (a != null && b != null)
                            {
                                if (a.Rank.Equals(b.Rank))
                                {
                                    double fDistance = 0.0;
                                    for (int i = 0; i < a.Rank; i++)
                                    {
                                        fDistance += Math.Pow(a.GetFeature(i) - b.GetFeature(i), 2.0);
                                    }

                                    return fDistance;
                                }
                                else
                                {
                                    throw new RankException();
                                }
                            }
                            else
                            {
                                throw new ArgumentNullException();
                            }
                        };
                }

                return Vector.s_distSqEuclidean;
            }
        }

        public double this[int nIndex]
        {
            get
            {
                return this.GetFeature(nIndex);
            }
            set
            {
                this.SetFeature(nIndex, value);
            }
        }
    }
}
