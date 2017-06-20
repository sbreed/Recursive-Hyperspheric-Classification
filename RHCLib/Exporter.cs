using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Exporter
    {
        public static void Serialize<L>(IEnumerable<LabeledVector<L>> vectors, System.IO.StreamWriter stream)
        {
            Exporter.Serialize<L>(vectors, stream, ",");
        }

        public static void Serialize<L>(IEnumerable<LabeledVector<L>> vectors, System.IO.StreamWriter stream, string separator)
        {
            foreach (LabeledVector<L> vector in vectors)
            {
                stream.WriteLine(string.Format("{0}{1}{2}", string.Join(separator, vector.Select(f => f.ToString())), separator, vector.Label));
            }
        }
    }
}
