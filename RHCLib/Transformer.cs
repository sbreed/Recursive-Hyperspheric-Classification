using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Transformer
    {
        public static void TransformWithOneLabel<L>(IList<LabeledVector<L>> vectors, out double[][] features, out double[][] labels, out Dictionary<L, int> map)
        {
            features = vectors.Select(v => v.Features).ToArray();

            List<L> labelSet = vectors.Select(v => v.Label).Distinct().ToList();
            labels = new double[vectors.Count][];
            for (int i = 0; i < vectors.Count; i++)
            {
                labels[i] = new double[] { labelSet.IndexOf(vectors[i].Label) };
            }

            map = new Dictionary<L, int>();
            for (int i = 0; i < labelSet.Count; i++)
            {
                map.Add(labelSet[i], i);
            }
        }

        /// <summary>
        /// Transforms an IList of LabeledVectors into two separate double[][]
        /// </summary>
        /// <typeparam name="L"></typeparam>
        /// <param name="vectors"></param>
        /// <param name="features"></param>
        /// <param name="labels"></param>
        /// <param name="map">Tells the mapping of the label to the respective index in the variable labels.</param>
        public static void Transform<L>(IList<LabeledVector<L>> vectors, out double[][] features, out double[][] labels, out Dictionary<L, int> map)
        {
            features = vectors.Select(v => v.Features).ToArray();

            L[] labelSet = vectors.Select(v => v.Label).Distinct().ToArray();
            labels = new double[vectors.Count][];
            for (int i = 0; i < vectors.Count; i++)
            {
                labels[i] = new double[labelSet.Length];
                for (int j = 0; j < labels[i].Length; j++)
                {
                    labels[i][j] = vectors[i].Label.Equals(labelSet[j]) ? 1.0 : 0.0;
                }
            }

            map = new Dictionary<L, int>();
            for (int i = 0; i < labelSet.Length; i++)
            {
                map.Add(labelSet[i], i);
            }
        }
    }
}
