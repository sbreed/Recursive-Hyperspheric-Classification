using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public class Sphere<L> : LabeledVector<L>
    {
        private readonly List<Sphere<L>> m_lstChildren = new List<Sphere<L>>();

        public Sphere(double fRadius, L label, IEnumerable<double> features)
            : base(label, features)
        {
            this.Radius = fRadius;
        }

        public Sphere(double fRadius, L label, params double[] features)
            : this(fRadius, label, (IEnumerable<double>)features)
        {

        }

        public Sphere(double fRadius, L label, IVector vector)
            : this(fRadius, label, vector.Features)
        {

        }

        public Sphere(double fRadius, LabeledVector<L> lvector)
            : this(fRadius, lvector.Label, lvector.Features)
        {

        }

        public void AddChild(Sphere<L> child)
        {
            this.m_lstChildren.Add(child);
            child.Parent = this;
        }

        private bool ContractRadiusToMidpoint(IEnumerable<LabeledVector<L>> groupThatSpawnedChild, IEnumerable<LabeledVector<L>> vectors, DistanceDelegate measure)
        {
            LabeledVector<L> lvectorSame = Sorter.RadialSort(groupThatSpawnedChild.Cast<IVector>(), this, measure, Sorter.SortType.GreatestDistanceFirst).First() as LabeledVector<L>;
            IEnumerable<LabeledVector<L>> vectorsDifferentLabel = this.GetVectorsWithDifferentLabel(vectors);

            if (vectorsDifferentLabel.Count() > 0)
            {
                LabeledVector<L> lvectorDifferent = (from v in vectorsDifferentLabel
                                                     where measure(this, v) > measure(this, lvectorSame)
                                                     orderby measure(this, v) ascending
                                                     select v).FirstOrDefault();
                if (lvectorDifferent == null)
                {
                    lvectorDifferent = (from v in vectorsDifferentLabel
                                        orderby measure(this, v) descending
                                        select v).First();
                }

                this.Radius = measure(this, lvectorSame) > measure(this, lvectorDifferent) ? measure(this, lvectorDifferent) + ((measure(this, lvectorSame) - measure(this, lvectorDifferent)) / 2) : measure(this, lvectorSame) + ((measure(this, lvectorDifferent) - measure(this, lvectorSame)) / 2);

                return true;
            }
            else
            {
                return false;
            }
        }

        private static Sphere<L> CreateSphereCenteredOnVectorAndMidwayDistanceBetweenClosestVector(IVector centroid, IEnumerable<IVector> vectors, L label, DistanceDelegate measure)
        {
            IVector vectorClosest = (from v in vectors
                                     where !Vector.EqualsEx(centroid, v)
                                     orderby measure(centroid, v)
                                     select v).FirstOrDefault();
            
            return vectorClosest != null ? new Sphere<L>(measure(centroid, vectorClosest) / 2, label, centroid) : null;
        }

        private Sphere<L> CreateSphereFromFurthestVector(IEnumerable<LabeledVector<L>> vectors, DistanceDelegate measure)
        {
            LabeledVector<L> childCentroid = Sorter.RadialSort(vectors.Cast<IVector>(), this, measure, Sorter.SortType.GreatestDistanceFirst).First() as LabeledVector<L>;

            return new Sphere<L>(this.Radius - measure(this, childCentroid), childCentroid);
        }

        private static Sphere<L> CreateSphereConstrainedToGroupAndClosestExcludedVector(IEnumerable<IVector> vectorsForCentroid, L label, IEnumerable<LabeledVector<L>> vectorsAll, DistanceDelegate measure)
        {
            Vector centroid = Vector.Centroid(vectorsForCentroid);
            LabeledVector<L> vectorSameLabel = Sorter.RadialSort(vectorsForCentroid.Cast<IVector>(), centroid, measure, Sorter.SortType.GreatestDistanceFirst).First() as LabeledVector<L>;
            
            IEnumerable<LabeledVector<L>> vectorsDifferentLabel = LabeledVector<L>.GetVectorsWithoutLabel(vectorsAll, label);
            IVector vectorDifferentLabel = (from v in vectorsDifferentLabel
                                            where measure(centroid, v) > measure(centroid, vectorSameLabel)
                                            orderby measure(centroid, v)
                                            select v).FirstOrDefault();
            if (vectorDifferentLabel == null)
            {
                vectorDifferentLabel = (from v in vectorsDifferentLabel
                                        orderby measure(vectorSameLabel, v)
                                        select v).FirstOrDefault();
            }

            return vectorDifferentLabel != null ? new Sphere<L>(measure(centroid, vectorDifferentLabel) > measure(centroid, vectorSameLabel) ? measure(centroid, vectorSameLabel) + ((measure(centroid, vectorDifferentLabel) - measure(centroid, vectorSameLabel)) / 2) : measure(centroid, vectorDifferentLabel) + ((measure(centroid, vectorSameLabel) - measure(centroid, vectorDifferentLabel)) / 2), label, (IVector)centroid) : null;
        }

        private static Sphere<L> CreateSphereFromVector(LabeledVector<L> lvector, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            IVector vectorClosest = (from v in vectors
                                     where !Vector.EqualsEx(lvector, v)
                                     select v).OrderBy(v => measure(lvector, v)).First() as IVector;

            return new Sphere<L>(measure(lvector, vectorClosest) / 2, lvector);
        }

        public static Sphere<L> CreateUnitSphere(DistanceDelegate measure, int nRank, L label)
        {
            Vector vecOrigin = new Vector(Enumerable.Repeat<double>(0.0, nRank));
            Vector vecCOG = new Vector(Enumerable.Repeat<double>(0.5, nRank));

            return new Sphere<L>(measure(vecOrigin, vecCOG), label, (IVector)vecCOG);
        }

        public bool DoesEncloseAll(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return Enumerable.All(vectors, v => this.DoesEncloseVector(v, measure));
        }

        public bool DoesEncloseAny(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return Enumerable.Any(vectors, v => this.DoesEncloseVector(v, measure));
        }

        public bool DoesEncloseVector(IVector vector, DistanceDelegate measure)
        {
            return this.Radius >= measure(this, vector);
        }

        public override bool Equals(object obj)
        {
            Sphere<L> sphere;
            if ((sphere = obj as Sphere<L>) != null && !this.Radius.Equals(sphere.Radius))
            {
                return base.Equals(sphere);
            }
            else
            {
                return false;
            }
        }

        public override bool EqualsWithLabelBeingInvariant(object obj)
        {
            Sphere<L> sphere;
            if ((sphere = obj as Sphere<L>) != null)
            {
                return base.EqualsWithLabelBeingInvariant(obj) && this.Radius.Equals(sphere.Radius);
            }
            else
            {
                return false;
            }
        }

        public int EnclosesHowMany(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return vectors.Count(v => this.DoesEncloseVector(v, measure));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public IEnumerable<IVector> GetVectorsInSphere(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return from v in vectors
                   where this.DoesEncloseVector(v, measure)
                   select v;
        }

        public IEnumerable<IVector> GetVectorsNotInChildren(IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            return from v in vectors
                   where this.Children.All(c => !c.DoesEncloseVector(v, measure))
                   select v;
        }

        public override void Print()
        {
            StringBuilder sb = new StringBuilder("[");

            for (int i = 0; i < this.Rank - 1; i++)
            {
                sb.Append(string.Format("{0}, ", this[i]));
            }
            sb.Append(string.Format("{0}] : {1} --- {2}", this[this.Rank - 1], this.Radius, this.Label));

            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }

        public virtual L RecognizeAsLabel(IVector vector, DistanceDelegate measure)
        {
            return this.Recognize(vector, measure).Label;
        }

        public virtual Sphere<L> Recognize(IVector vector, DistanceDelegate measure)
        {
            Sphere<L> sphereMinRadius = this.Parent == null ? this : null;

            if (this.DoesEncloseVector(vector, measure))
            {
                sphereMinRadius = this;
                Sphere<L> sphereCandidate = null;
                foreach (Sphere<L> child in this.Children)
                {
                    if ((sphereCandidate = child.Recognize(vector, measure)) != null && sphereCandidate.Radius < sphereMinRadius.Radius)
                    {
                        sphereMinRadius = sphereCandidate;
                    }
                }
            }

            return sphereMinRadius;
        }

        public virtual IEnumerable<Sphere<L>> Spawn(IEnumerable<LabeledVector<L>> vectors, DistanceDelegate measure)
        {
            List<Sphere<L>> lstSpawn = new List<Sphere<L>>();

            List<LabeledVector<L>> lstVectorsInSphere = this.GetVectorsInSphere(vectors.Cast<IVector>(), measure).Cast<LabeledVector<L>>().ToList();

            #region Recursively spawn children

            foreach (Sphere<L> child in this.Children)
            {
                lstSpawn.AddRange(child.Spawn(lstVectorsInSphere, measure));
            }

            #endregion

            List<LabeledVector<L>> lstVectorsNotInChildren = this.GetVectorsNotInChildren(lstVectorsInSphere.Cast<IVector>(), measure).Cast<LabeledVector<L>>().ToList();
            IDictionary<L, IEnumerable<LabeledVector<L>>> dictVectorsNotInChildren = LabeledVector<L>.SeparateIntoClasses(lstVectorsNotInChildren);

            foreach (KeyValuePair<L, IEnumerable<LabeledVector<L>>> kvp in dictVectorsNotInChildren)
            {
                if (!kvp.Key.Equals(this.Label))
                {
                    Vector vectorCentroid = Vector.Centroid(kvp.Value.Cast<IVector>().ToList());

                    Sphere<L> child = new Sphere<L>(this.Radius - measure(this, vectorCentroid), kvp.Key, (IVector)vectorCentroid);
                    if (child.DoesEncloseAll(lstVectorsInSphere.Cast<IVector>(), measure) || !child.DoesEncloseAny(kvp.Value.Cast<IVector>(), measure))
                    {
                        child = this.CreateSphereFromFurthestVector(kvp.Value, measure);
                        if (child.DoesEncloseAll(lstVectorsInSphere.Cast<IVector>(), measure))
                        {
                            child = Sphere<L>.CreateSphereConstrainedToGroupAndClosestExcludedVector(kvp.Value.Cast<IVector>(), kvp.Key, lstVectorsInSphere, measure);
                            if (!child.DoesEncloseAll(kvp.Value.Cast<IVector>(), measure))
                            {
                                child = Sphere<L>.CreateSphereCenteredOnVectorAndMidwayDistanceBetweenClosestVector(Sorter.RadialSort(kvp.Value.Cast<IVector>(), this, measure, Sorter.SortType.GreatestDistanceFirst).First(), lstVectorsInSphere.Cast<IVector>(), kvp.Key, measure);
                            }
                        }
                    }

                    if (child != null && !Vector.EqualsEx(this, child))
                    {
                        this.AddChild(child);
                        lstSpawn.Add(child);
                    }
                }
            }

            return lstSpawn;
        }

        public virtual IEnumerable<Sphere<L>> SpawnByExploding(IEnumerable<LabeledVector<L>> vectors, DistanceDelegate measure)
        {
            List<Sphere<L>> lstSpawn = new List<Sphere<L>>();

            List<LabeledVector<L>> lstVectorsInSphere = this.GetVectorsInSphere(vectors.Cast<IVector>(), measure).Cast<LabeledVector<L>>().ToList();

            #region Recursively spawn children

            foreach (Sphere<L> child in this.Children)
            {
                lstSpawn.AddRange(child.SpawnByExploding(lstVectorsInSphere, measure));
            }

            #endregion

            List<LabeledVector<L>> lstVectorsNotInChildren = this.GetVectorsNotInChildren(lstVectorsInSphere.Cast<IVector>(), measure).Cast<LabeledVector<L>>().ToList();
            IDictionary<L, IEnumerable<LabeledVector<L>>> dictVectorsNotInChildren = LabeledVector<L>.SeparateIntoClasses(lstVectorsNotInChildren);

            List<Sphere<L>> lstChildrenSpawnedThisIteration = new List<Sphere<L>>();

            foreach (KeyValuePair<L, IEnumerable<LabeledVector<L>>> kvp in dictVectorsNotInChildren)
            {
                if (!kvp.Key.Equals(this.Label))
                {
                    Vector vectorCentroid = Vector.Centroid(kvp.Value.Cast<IVector>().ToList());

                    Sphere<L> sphereClosest = (from s in this.Children.Concat(Enumerable.Repeat(this, 1))
                                                    orderby measure(vectorCentroid, s)
                                                    select s).First();

                    Sphere<L> child = new Sphere<L>(sphereClosest != this ? measure(vectorCentroid, sphereClosest) : this.Radius - measure(this, vectorCentroid), kvp.Key, (IVector)vectorCentroid);
                    if (child.DoesEncloseAll(lstVectorsInSphere.Cast<IVector>(), measure) || !child.DoesEncloseAny(kvp.Value.Cast<IVector>(), measure))
                    {
                        child = Sphere<L>.CreateSphereCenteredOnVectorAndMidwayDistanceBetweenClosestVector(Sorter.RadialSort(kvp.Value.Cast<IVector>(), this, measure, Sorter.SortType.GreatestDistanceFirst).First(), lstVectorsInSphere.Cast<IVector>(), kvp.Key, measure);
                    }

                    lstChildrenSpawnedThisIteration.Add(child);
                }
            }

            foreach (Sphere<L> child in lstChildrenSpawnedThisIteration)
            {
                if (!Vector.EqualsEx(this, child))
                {
                    this.AddChild(child);
                    lstSpawn.Add(child);
                }
            }

            return lstSpawn;
        }

        public virtual IEnumerable<Sphere<L>> SpawnSingular(LabeledVector<L> lvector, DistanceDelegate measure)
        {
            List<Sphere<L>> lstSpawn = new List<Sphere<L>>();

            if (this.DoesEncloseVector(lvector, measure))
            {
                foreach (Sphere<L> child in this.Children)
                {
                    lstSpawn.AddRange(child.SpawnSingular(lvector, measure));
                }

                if (!this.Label.Equals(lvector.Label) && !Vector.EqualsEx(this, lvector))
                {
                    Sphere<L> child = new Sphere<L>(this.Radius - measure(this, lvector), lvector);
                    this.AddChild(child);
                    lstSpawn.Add(child);
                }
            }

            return lstSpawn;
        }

        public virtual IEnumerable<Sphere<L>> SpawnSingularByExploding(LabeledVector<L> lvector, DistanceDelegate measure)
        {
            List<Sphere<L>> lstSpawn = new List<Sphere<L>>();

            if (this.DoesEncloseVector(lvector, measure))
            {
                foreach (Sphere<L> child in this.Children)
                {
                    lstSpawn.AddRange(child.SpawnSingularByExploding(lvector, measure));
                }

                if (!this.Label.Equals(lvector.Label) && !Vector.EqualsEx(this, lvector))
                {
                    Sphere<L> sphereClosest = (from s in this.Children.Concat(Enumerable.Repeat(this, 1))
                                               orderby measure(lvector, s)
                                               select s).First();

                    Sphere<L> child = new Sphere<L>(sphereClosest != this ? measure(lvector, sphereClosest) : this.Radius - measure(this, lvector), lvector);

                    this.AddChild(child);
                    lstSpawn.Add(child);
                }
            }

            return lstSpawn;
        }

        public IEnumerable<Sphere<L>> Children
        {
            get
            {
                return this.m_lstChildren.ToArray();
            }
        }

        public int Height
        {
            get
            {
                int nHeightMax = 0;
                int nHeight;
                foreach (Sphere<L> child in this.Children)
                {
                    nHeight = child.Height;
                    if (nHeight > nHeightMax)
                    {
                        nHeightMax = nHeight;
                    }
                }

                return nHeightMax + 1;
            }
        }

        public Sphere<L> Parent { get; private set; }

        public double Radius { get; set; }

        public int SphereCount
        {
            get
            {
                int nCount = 1;
                foreach (Sphere<L> child in this.Children)
                {
                    nCount += child.SphereCount;
                }
                return nCount;
            }
        }
    }
}