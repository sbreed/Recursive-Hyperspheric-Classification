using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RHCLib;

namespace RHCTestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            // Let's use the SquaredEuclideanDistance as a measure learning.  Alternatively, we could use another distance measure, e.g, EuclideanDistance
            DistanceDelegate measure = Vector.SquaredEuclideanDistance;

            using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\iris.data"))
            {
                // Import the vectors from the delimited file
                LabeledVector<string>[] vectors = Importer.Import<string>(sr, 0, ",", Importer.ClassColumn.LastColumn);
                // Normalize the vectors in the range [0, 1]
                Vector.Normalize(vectors);
                
                // RANDOMLY partition the vectors into two datasets: the training dataset (80%) and the validation dataset (20%)
                IList<LabeledVector<string>> train;
                IList<LabeledVector<string>> test;
                Partitioner.RandomPartition<LabeledVector<string>>(vectors, (int)(vectors.Count() * 0.80), out train, out test);

                // Create the first sphere
                Sphere<string> sphere = Sphere<string>.CreateUnitSphere(measure, vectors[0].Rank, vectors[0].Label);

                System.Console.WriteLine("Spawning...");

                int nSpawnCount;
                do
                {
                    // Learn
                    nSpawnCount = sphere.Spawn(train, measure).Count();

                    System.Console.WriteLine(string.Format("  - {0} hypersphere(s) spawned", nSpawnCount));
                } while (nSpawnCount > 0);

                System.Console.WriteLine();
                System.Console.WriteLine("Done training!");
                System.Console.WriteLine(string.Format("Sphere Count: {0}", sphere.SphereCount));
                System.Console.WriteLine(string.Format("Sphere Height: {0}", sphere.Height));

                System.Console.WriteLine();
                System.Console.WriteLine("Validating...");
                int nCorrect, nIncorrect;
                sphere.Recognize(test, measure, out nCorrect, out nIncorrect);
                System.Console.WriteLine("Done validating.");

                System.Console.WriteLine();
                System.Console.WriteLine("Validation results:");
                System.Console.WriteLine(string.Format("{0} of {1} [{2:P2}] vectors correctly labeled!", nCorrect, test.Count, (double)nCorrect / test.Count));

                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();

                LabeledVector<int>[] rg = Importer.Import<int>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, new StringToIntegerConverter());
            }
        }
    }

    // These following two classes are needed if you want to convert a string to another type
    // In the iris.data case, you could have converted the class label into an integer if desired
    // EXA: LabeledVector<vectors>[] rg = Importer.Import<int>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, new StringToIntegerConverter());

    public class StringToDoubleConverter : IPCLValueConverter<double>
    {
        public double Convert(string value)
        {
            return System.Convert.ToDouble(value);
        }
    }

    public class StringToIntegerConverter : IPCLValueConverter<int>
    {
        public int Convert(string value)
        {
            switch (value)
            {
                case "Iris-setosa":
                    return 0;
                case "Iris-versicolor":
                    return 1;
                case "Iris-virginica":
                    return 2;
                default:
                    throw new NotImplementedException("Unknown class label.");
            }
        }
    }
}
