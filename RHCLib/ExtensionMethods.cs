using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class ExtensionMethods
    {
        public static void Print(this IEnumerable<IVector> vectors)
        {
            foreach (IVector vector in vectors)
            {
                System.Reflection.MethodInfo mi = vector.GetType().GetMethod("Print");
                if (mi != null)
                {
                    mi.Invoke(vector, null);
                }
            }
        }

        public static void PrintBubbleUp<L>(this Sphere<L> sphere)
        {
            while (sphere != null)
            {
                sphere.Print();
                sphere = sphere.Parent;
            }
        }

        public static void PrintHierarchy<L>(this Sphere<L> sphere)
        {
            ExtensionMethods.PrintHierarchy<L>(sphere, 0, null, null);
        }

        public static void PrintHierarchy<L>(this Sphere<L> sphere, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            ExtensionMethods.PrintHierarchy<L>(sphere, 0, vectors, measure);
        }

        private static void PrintHierarchy<L>(this Sphere<L> sphere, int nLevel, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0}[", new string(' ', nLevel)));
            for (int i = 0; i < sphere.Rank - 1; i++)
            {
                sb.Append(string.Format("{0}, ", sphere[i]));
            }
            sb.Append(string.Format("{0}] - {1} - {2}", sphere[sphere.Rank - 1], sphere.Radius, sphere.Label));

            if (vectors != null)
            {
                sb.Append(string.Format(" ::: {0}", sphere.EnclosesHowMany(vectors, measure)));
            }

            System.Diagnostics.Debug.WriteLine(sb.ToString());

            nLevel++;

            foreach (Sphere<L> child in sphere.Children)
            {
                ExtensionMethods.PrintHierarchy<L>(child, nLevel, vectors, measure);
            }
        }

        public static void PrintHierarchy<L>(this Sphere<L> sphere, System.IO.StreamWriter sw)
        {
            ExtensionMethods.PrintHierarchy<L>(sphere, sw, 0, null, null);
        }

        public static void PrintHierarchy<L>(this Sphere<L> sphere, System.IO.StreamWriter sw, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            ExtensionMethods.PrintHierarchy<L>(sphere, sw, 0, vectors, measure);
        }

        private static void PrintHierarchy<L>(this Sphere<L> sphere, System.IO.StreamWriter sw, int nLevel, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0}[", new string(' ', nLevel)));
            for (int i = 0; i < sphere.Rank - 1; i++)
            {
                sb.Append(string.Format("{0}, ", sphere[i]));
            }
            sb.Append(string.Format("{0}] - {1} - {2}", sphere[sphere.Rank - 1], sphere.Radius, sphere.Label));

            if (vectors != null)
            {
                sb.Append(string.Format(" ::: {0}", sphere.EnclosesHowMany(vectors, measure)));
            }

            sw.WriteLine(sb.ToString());

            nLevel++;

            foreach (Sphere<L> child in sphere.Children)
            {
                ExtensionMethods.PrintHierarchy<L>(child, sw, nLevel, vectors, measure);
            }
        }

        public static void Recognize<L>(this Sphere<L> sphere, IEnumerable<IVector> vectors, DistanceDelegate measure)
        {
            int nCorrect, nIncorrect;
            ExtensionMethods.Recognize(sphere, vectors, measure, out nCorrect, out nIncorrect);
        }

        public static void Recognize<L>(this Sphere<L> sphere, IEnumerable<IVector> vectors, DistanceDelegate measure, out int nCorrect, out int nIncorrect)
        {
            nCorrect = 0;
            nIncorrect = 0;

            L label;
            int nCurrentVector = 0;
            int nVectorCount = vectors.Count();

            foreach (IVector vector in vectors)
            {
                label = sphere.RecognizeAsLabel(vector, measure);

                System.Diagnostics.Debug.WriteLine(string.Format("Vector {0}:: Recognized Label: {1}{2}", nCurrentVector, label, vector is LabeledVector<L> ? string.Format(" | Actual Label: {0}", ((LabeledVector<L>)vector).Label) : string.Empty));

                if (vector is LabeledVector<L>)
                {
                    nCorrect += ((LabeledVector<L>)vector).Label.Equals(label) ? 1 : 0;
                    nIncorrect += ((LabeledVector<L>)vector).Label.Equals(label) ? 0 : 1;
                }

                nCurrentVector++;
            }

            System.Diagnostics.Debug.WriteLine(string.Empty);

            System.Diagnostics.Debug.WriteLine("If Labeled Vectors:");
            System.Diagnostics.Debug.WriteLine(string.Format("  - {0} of {1} were correctly identified [{2:P2}]", nCorrect, nVectorCount, (double)nCorrect / nVectorCount));
        }
    }
}
