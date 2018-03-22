using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RHCLib;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RHCTestBed
{
    class Program
    {
        private const int NUMBER_OF_TESTS = 30;

        private const int RHC_MAX_SPHERE_COUNT = 500000;

        private const double TRAIN_SET_PERCENTAGE = 0.80;

        private const int MAX_NN_ITERATIONS = 5000;
        private const double NN_LEARN_RATE = 0.03;
        private const double NN_MOMENTUM = 0.3;
        private const double MIN_NN_ERROR = 0.001;
        private const int NN_HIDDEN_NODE_COUNT = 20;

        private const int MAX_SVM_ITERATIONS = 5000;
        private const double MIN_SVM_ERROR = 0.001;

        private const int RBF_BASE_COUNT = 2;

        private const int NEAT_POPULATION_COUNT = 150;

        static void Main(string[] args)
        {
            DataSet dataSet;
            ParallelStrategy parallelStrategy = ParallelStrategy.SingleThreadSpawn;

            string strInput;
            int nInput;
            do
            {
                Console.WriteLine("What data set would you like to run?");
                Console.WriteLine();

                foreach (DataSet set in Enum.GetValues(typeof(DataSet)))
                {
                    Console.WriteLine("{0}: {1}", (int)set, set);
                }
                strInput = Console.ReadLine();
            } while (!int.TryParse(strInput, out nInput) || !Enum.GetValues(typeof(DataSet)).OfType<DataSet>().Select(ds => (int)ds).Contains(nInput));
            dataSet = (DataSet)nInput;

            Dictionary<BenchmarkAlgorithm, List<RunStatistics>> dictResults = new Dictionary<BenchmarkAlgorithm, List<RunStatistics>>();
            foreach (BenchmarkAlgorithm algorithm in Program.AcquireAlgorithmBenchmarkSet())
            {
                dictResults.Add(algorithm, new List<RunStatistics>());
            }

            if (dictResults.Any(ba => ba.Key.HasAttributeOfType<IsRHCSphereBenchmarkAttribute>()))
            {
                do
                {
                    Console.WriteLine("At least one algorithm is RHC based, would you like to try to expedite the spawning process by offloading the spawning over many cores (Note: For small datasets or hierarchies, this MAY impede training time because of the overhead with threads)? (Y or N)");
                    strInput = Console.ReadLine().ToUpper();
                } while (strInput != "Y" && strInput != "N");
                parallelStrategy = strInput == "Y" ? ParallelStrategy.MultithreadedSpawn : ParallelStrategy.SingleThreadSpawn;
            }

            do
            {
                Console.WriteLine("Would you like to (L)oad or run a (N)ew set?");
                strInput = Console.ReadLine().ToUpper();
            } while (strInput != "L" && strInput != "N");

            LabeledVector<double>[] rgEntireDataset;
            List<IList<LabeledVector<double>>> lstAllTrain = new List<IList<LabeledVector<double>>>();
            List<IList<LabeledVector<double>>> lstAllTest = new List<IList<LabeledVector<double>>>();

            if (strInput == "L")
            {
                #region Load from CSV

                Dictionary<int, string> dictKeys = Program.GetSerializedVectorsKeys(dataSet).Select((k, i) => new { Key = k, Index = i }).ToDictionary(p => p.Index, p => p.Key);

                int index;
                bool invalid = true;
                do
                {
                    foreach (KeyValuePair<int, string> kvp in dictKeys)
                    {
                        Console.WriteLine(string.Format("{0} : {1}", kvp.Key, kvp.Value));
                    }
                    Console.WriteLine();
                    Console.Write("Enter the number (index) of the keyed data set to load: ");

                    if (int.TryParse(Console.ReadLine(), out index) && index < dictKeys.Count && index >= 0)
                    {
                        invalid = false;
                    }
                } while (invalid);

                Program.DeserializeVectors<double>(dataSet, dictKeys[index], out lstAllTrain, out lstAllTest);

                #endregion
            }
            else
            {
                #region New Dataset

                switch (dataSet)
                {
                    case DataSet.IrisDataSet:
                        #region Iris

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\iris.data")) // Change this for another dataset
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, null, new Importer.ValueConverterHandler<double>((l, c) =>
                            {
                                switch (l.Trim().ToUpper())
                                {
                                    case "IRIS-SETOSA":
                                        return 0.0;
                                    case "IRIS-VERSICOLOR":
                                        return 1.0;
                                    case "IRIS-VIRGINICA":
                                        return 2.0;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.WineDataSet:
                        #region Wine

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\wine.data")) // Change this for another dataset
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.FirstColumn);
                        }

                        #endregion
                        break;
                    case DataSet.WisconsinBreastCancerDataSet:
                        #region Wisconsin Breast Cancer

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\breast-cancer-wisconsin.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, null, null, new Importer.LinePreprocessorHandler(e =>
                            {
                                e.SkipLine = e.Line.Contains("?");
                                if (!e.SkipLine)
                                {
                                    // Filter out the ID; it's irrelevant.  I could also filter using the columnsToDiscard parameter
                                    e.Line = e.Line.Substring(e.Line.IndexOf(',') + 1);
                                }
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.NewThyroid:
                        #region New Thyroid

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\new-thyroid.data")) // Change this for another dataset
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.FirstColumn);
                        }

                        #endregion
                        break;
                    case DataSet.Glass:
                        #region Glass

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\glass.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, new int[] { 0 });
                        }

                        #endregion
                        break;
                    case DataSet.BalanceScale:
                        #region Balance Scale

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\balance-scale.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.FirstColumn, null, null, new Importer.ValueConverterHandler<double>((l, c) =>
                            {
                                switch (l.Trim().ToUpper())
                                {
                                    case "L":
                                        return 0.0;
                                    case "R":
                                        return 1.0;
                                    case "B":
                                        return 2.0;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.Credit:
                        #region Credit

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\crx.data"))
                        {
                            // Only NON continuous columns should be converted
                            Dictionary<int, List<string>> dict = new int[] { 0, 3, 4, 5, 6, 8, 9, 11, 12 }.ToDictionary(k => k, k => new List<string>());
                            List<string> labels;

                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null,
                                new Importer.ValueConverterHandler<double>((s, i) =>
                                {
                                    if (dict.TryGetValue(i, out labels))
                                    {
                                        s = s.Trim().ToUpper();

                                        int index = labels.IndexOf(s);
                                        if (index == -1)
                                        {
                                            labels.Add(s);
                                            index = labels.Count - 1;
                                        }

                                        return index;
                                    }
                                    else
                                    {
                                        return Convert.ToDouble(s);
                                    }
                                }),
                                new Importer.ValueConverterHandler<double>((s, i) =>
                                {
                                    switch (s.Trim().ToUpper())
                                    {
                                        case "+":
                                            return 0.0;
                                        case "-":
                                            return 1.0;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }),
                                new Importer.LinePreprocessorHandler(e =>
                                {
                                    e.SkipLine = e.Line.Contains("?");
                                }));
                        }

                        #endregion
                        break;
                    case DataSet.Dermatology:
                        #region Dermatology

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\dermatology.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, null, null, new Importer.LinePreprocessorHandler(e =>
                            {
                                e.SkipLine = e.Line.Contains("?");
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.PimaDiabetes:
                        #region PimaDiabetes

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\pima-indians-diabetes.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn);
                        }

                        #endregion
                        break;
                    case DataSet.EColi:
                        #region E. Coli

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\ecoli.data")) // Change this for another dataset
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, new int[] { 0 }, null, new Importer.ValueConverterHandler<double>((s, i) =>
                            {
                                switch (s.Trim().ToUpper())
                                {
                                    case "CP":
                                        return 0.0;
                                    case "IM":
                                        return 1.0;
                                    case "IMS":
                                        return 2.0;
                                    case "IML":
                                        return 3.0;
                                    case "IMU":
                                        return 4.0;
                                    case "OM":
                                        return 5.0;
                                    case "OML":
                                        return 6.0;
                                    case "PP":
                                        return 7.0;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }), new Importer.LinePreprocessorHandler(e =>
                            {
                                e.Line = Regex.Replace(e.Line, @"\s+", " ").Replace(' ', ',');
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.ClevelandHeartDisease:
                        #region ClevelandHeartDisease

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\processed.cleveland.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, null, null, new Importer.LinePreprocessorHandler(e =>
                            {
                                e.SkipLine = e.Line.Contains("?");
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.Urban:
                        #region Urban

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\urban_training.csv"))
                        {
                            List<string> labels = new List<string>();

                            rgEntireDataset = Importer.Import<double>(sr, 1, ",", Importer.ClassColumn.FirstColumn, null, null, new Importer.ValueConverterHandler<double>((l, i) =>
                            {
                                string label = l.Trim().ToUpper();
                                int index = labels.IndexOf(label);
                                if (index == -1)
                                {
                                    labels.Add(label);
                                    index = labels.Count - 1;
                                }

                                return index;
                            }));
                        }

                        #endregion
                        break;
                    case DataSet.Zoo:
                        #region Zoo

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\zoo.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, new int[] { 0 }, null, null);
                        }

                        #endregion
                        break;
                    case DataSet.Ionosphere:
                        #region Ionosphere

                        using (System.IO.StreamReader sr = new System.IO.StreamReader(@".\Datasets\ionosphere.data"))
                        {
                            rgEntireDataset = Importer.Import<double>(sr, 0, ",", Importer.ClassColumn.LastColumn, null, null, new Importer.ValueConverterHandler<double>((s, i) =>
                            {
                                switch (s.Trim().ToUpper())
                                {
                                    case "G":
                                        return 0.0;
                                    case "B":
                                        return 1.0;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }));
                        }

                        #endregion
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Vector.Normalize(rgEntireDataset);

                IList<LabeledVector<double>> lstTrain;
                IList<LabeledVector<double>> lstTest;
                for (int i = 0; i < Program.NUMBER_OF_TESTS; i++)
                {
                    Partitioner.RandomPartition<LabeledVector<double>>(rgEntireDataset, (int)(rgEntireDataset.Length * TRAIN_SET_PERCENTAGE), out lstTrain, out lstTest);

                    lstAllTrain.Add(lstTrain);
                    lstAllTest.Add(lstTest);
                }

                #endregion
            }

            Debug.Assert(lstAllTrain.Count == lstAllTest.Count);

            for (int i = 0; i < lstAllTrain.Count; i++)
            {
                Console.WriteLine("ITERATION: {0}", i);

                IList<LabeledVector<double>> lstTrain = lstAllTrain[i];
                IList<LabeledVector<double>> lstTest = lstAllTest[i];

                foreach (BenchmarkAlgorithm algorithm in dictResults.Keys)
                {
                    Console.WriteLine("  - {0}", algorithm);

                    dictResults[algorithm].Add(Program.Benchmark(lstTrain, lstTest, algorithm, parallelStrategy));
                }
            }

            #region Display Results

            foreach (KeyValuePair<BenchmarkAlgorithm, List<RunStatistics>> kvp in dictResults)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    Console.WriteLine(kvp.Key.ToString().ToUpper());
                    Console.WriteLine();

                    Console.WriteLine("Correct Average: {0:P2}", kvp.Value.Select(r => r.CorrectPercentage).Average());
                    Console.WriteLine("Correct Std. Dev.: {0:P2}", kvp.Value.Select(r => r.CorrectPercentage).StandardDeviation());
                    if (kvp.Key.HasAttributeOfType<IsRHCSphereBenchmarkAttribute>())
                    {
                        Console.WriteLine("Average Tree Height: {0:F2}", kvp.Value.Select(r => r.TreeHeight.Value).Average());
                        Console.WriteLine("Tree Height Std. Dev.: {0:F2}", kvp.Value.Select(r => r.TreeHeight.Value).StandardDeviation());
                        Console.WriteLine("Average Sphere Count: {0:F2}", kvp.Value.Select(r => r.SphereCount.Value).Average());
                        Console.WriteLine("Sphere Count Std. Dev.: {0:F2}", kvp.Value.Select(r => r.SphereCount.Value).StandardDeviation());
                    }
                    Console.WriteLine("Average Training Time: {0:F4} ms", kvp.Value.Select(r => r.TrainTime).Average());
                    Console.WriteLine("Training Time Std. Dev.: {0:F4} ms", kvp.Value.Select(r => r.TrainTime).StandardDeviation());
                    Console.WriteLine("Average Test Time: {0:F4} ms", kvp.Value.SelectMany(r => r.TestTimes.Select(tt => tt)).Average());
                    Console.WriteLine("Test Time Std. Dev.: {0:F4} ms", kvp.Value.SelectMany(r => r.TestTimes.Select(tt => tt)).StandardDeviation());

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            #endregion

            #region Serialize the Train / Test Datasets

            if (strInput != "L")
            {
                do
                {
                    Console.WriteLine();
                    Console.Write("Would you like to serialize the train and test sets (Y / N)? ");
                    strInput = Console.ReadKey().KeyChar.ToString().ToUpper();
                } while (strInput != "Y" && strInput != "N");

                if (strInput == "Y")
                {
                    Program.SerializeVectors(dataSet, lstAllTrain, lstAllTest);
                }
            }

            #endregion

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static List<BenchmarkAlgorithm> AcquireAlgorithmBenchmarkSet()
        {
            List<BenchmarkAlgorithm> lstAlgorithms = new List<BenchmarkAlgorithm>();

            bool shouldContinue = true;
            do
            {
                Console.WriteLine("What algorithms would you like to benchmark?");
                Console.WriteLine();

                foreach (BenchmarkAlgorithm algorithm in Enum.GetValues(typeof(BenchmarkAlgorithm)))
                {
                    if (!lstAlgorithms.Contains(algorithm) && !algorithm.HasAttributeOfType<ObsoleteAttribute>())
                    {
                        Console.WriteLine("{0}: {1}", (int)algorithm, algorithm);
                    }
                }
                Console.WriteLine("A: All");
                Console.WriteLine();
                Console.WriteLine("D: Done");

                string input = Console.ReadLine().ToUpper().Trim();
                int inputAsInt;
                if (input == "A")
                {
                    foreach (BenchmarkAlgorithm algorithm in Enum.GetValues(typeof(BenchmarkAlgorithm)))
                    {
                        if (!lstAlgorithms.Contains(algorithm) && !algorithm.HasAttributeOfType<ObsoleteAttribute>())
                        {
                            lstAlgorithms.Add(algorithm);
                        }
                    }

                    shouldContinue = false;
                }
                else if (input == "D")
                {
                    shouldContinue = false;
                }
                else if (int.TryParse(input, out inputAsInt) && Enum.IsDefined(typeof(BenchmarkAlgorithm), inputAsInt) && !lstAlgorithms.Contains((BenchmarkAlgorithm)inputAsInt) && !((BenchmarkAlgorithm)inputAsInt).HasAttributeOfType<ObsoleteAttribute>())
                {
                    lstAlgorithms.Add((BenchmarkAlgorithm)inputAsInt);
                }
                else
                {
                    Console.WriteLine("Invalid selection.  Try again.");
                }
            } while (shouldContinue);

            return lstAlgorithms;
        }

        public static RunStatistics Benchmark<L>(IList<LabeledVector<L>> lstTrain, IList<LabeledVector<L>> lstTest, BenchmarkAlgorithm algorithm, ParallelStrategy parallelStrategy)
        {
            IsRHCSphereBenchmarkAttribute attribute = algorithm.GetAttributeOfType<IsRHCSphereBenchmarkAttribute>();
            if (attribute != null)
            {
                #region RHC

                DistanceDelegate measure = attribute.UseSquaredEuclidean ? Vector.SquaredEuclideanDistance : Vector.EuclideanDistance;

                Sphere<L> sphere = Sphere<L>.CreateUnitSphere(measure, lstTrain[0].Rank, lstTrain[0].Label);

                int spawnCount;

                Stopwatch watch = Stopwatch.StartNew();
                do
                {
                    switch (algorithm)
                    {
                        case BenchmarkAlgorithm.RHCEuclidean:
                        case BenchmarkAlgorithm.RHCSquaredEuclidean:
                            spawnCount = sphere.Spawn(lstTrain, measure, ChildDoesNotEncloseAnyStrategy.FurthestVectorSpawns, parallelStrategy);
                            break;
                        case BenchmarkAlgorithm.RHCLDASquaredEuclidean:
                        case BenchmarkAlgorithm.RHCLDAEuclidean:
                            spawnCount = sphere.SpawnWithLDA(lstTrain, measure, ChildDoesNotEncloseAnyStrategy.FurthestVectorSpawns, LDAStrategy.OnlyApplyLDAIfNoChildren, parallelStrategy);
                            break;
                        case BenchmarkAlgorithm.RHCDisjointMidpoint:
                            spawnCount = sphere.SpawnMinimally(lstTrain, measure, ChildDoesNotEncloseAnyStrategy.FurthestVectorSpawns, parallelStrategy);
                            break;
                        case BenchmarkAlgorithm.RHCMaxMargin:
                            spawnCount = sphere.SpawnMinimallyUsingDifferentLabel(lstTrain, measure, ChildDoesNotEncloseAnyStrategy.FurthestVectorSpawns, parallelStrategy);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    System.Diagnostics.Debug.WriteLine("Spawn Count: {0}", spawnCount);
                } while (spawnCount > 0 && sphere.SphereCount < Program.RHC_MAX_SPHERE_COUNT);
                watch.Stop();

                double trainTime = ((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000;

                List<double> lstTestTimes = new List<double>();

                int nCorrect = 0;
                foreach (LabeledVector<L> vector in lstTest)
                {
                    watch = Stopwatch.StartNew();
                    L label = sphere.RecognizeAsLabel(vector, measure);
                    watch.Stop();

                    nCorrect += vector.Label.Equals(label) ? 1 : 0;

                    lstTestTimes.Add(((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000);
                }

                return new RunStatistics((double)nCorrect / lstTest.Count, trainTime, lstTestTimes, sphere.Height, sphere.SphereCount);

                #endregion
            }
            else
            {
                switch (algorithm)
                {
                    case BenchmarkAlgorithm.BPNN:
                        #region BPNN
                        {
                            double[][] trainInput;
                            double[][] trainOutput;
                            double[][] testInput;
                            double[][] testOutput;
                            Dictionary<double, int> map;

                            Program.MergeAndTransform<double>(lstTrain as IList<LabeledVector<double>>, lstTest as IList<LabeledVector<double>>, out trainInput, out trainOutput, out testInput, out testOutput, out map);

                            Encog.Neural.NeuralData.INeuralDataSet trainingSet = new Encog.Neural.Data.Basic.BasicNeuralDataSet(trainInput, trainOutput);
                            Encog.Neural.Networks.BasicNetwork network = new Encog.Neural.Networks.BasicNetwork();
                            network.AddLayer(new Encog.Neural.Networks.Layers.BasicLayer(null, true, lstTrain[0].Rank));
                            network.AddLayer(new Encog.Neural.Networks.Layers.BasicLayer(new Encog.Engine.Network.Activation.ActivationSigmoid(), true, Program.NN_HIDDEN_NODE_COUNT));
                            network.AddLayer(new Encog.Neural.Networks.Layers.BasicLayer(new Encog.Engine.Network.Activation.ActivationSigmoid(), false, map.Count));

                            network.Structure.FinalizeStructure();
                            network.Reset();

                            Encog.Neural.Networks.Training.ITrain train = new Encog.Neural.Networks.Training.Propagation.Back.Backpropagation(network, trainingSet, Program.NN_LEARN_RATE, Program.NN_MOMENTUM);

                            int epoch = 0;
                            Stopwatch watch = Stopwatch.StartNew();
                            do
                            {
                                train.Iteration();
                                System.Diagnostics.Debug.WriteLine("Epoch #" + epoch + " Error:" + train.Error);
                                epoch++;
                            } while ((epoch < Program.MAX_NN_ITERATIONS) && (train.Error > Program.MIN_NN_ERROR));
                            watch.Stop();

                            double trainTime = ((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000;

                            Encog.Neural.NeuralData.INeuralDataSet testingSet = new Encog.Neural.Data.Basic.BasicNeuralDataSet(testInput, testOutput);

                            List<double> lstTestTimes = new List<double>();

                            int nCorrect = 0;
                            foreach (Encog.ML.Data.IMLDataPair pair in testingSet)
                            {
                                watch = Stopwatch.StartNew();
                                Encog.ML.Data.IMLData result = network.Compute(pair.Input);
                                watch.Stop();

                                #region Result

                                int maxIndex = -1;
                                double max = double.MinValue;
                                for (int i = 0; i < result.Count; i++)
                                {
                                    if (result[i] > max)
                                    {
                                        maxIndex = i;
                                        max = result[i];
                                    }
                                }

                                #endregion

                                #region Ideal

                                int maxIdealIndex = -1;
                                max = double.MinValue;
                                for (int i = 0; i < pair.Ideal.Count; i++)
                                {
                                    if (pair.Ideal[i] > max)
                                    {
                                        maxIdealIndex = i;
                                        max = result[i];
                                    }
                                }

                                #endregion

                                nCorrect += (maxIndex == maxIdealIndex ? 1 : 0);

                                lstTestTimes.Add(((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000);
                            }

                            return new RunStatistics((double)nCorrect / lstTest.Count, trainTime, lstTestTimes);
                        }

                    #endregion
                    case BenchmarkAlgorithm.SVM:
                        #region SVM
                        {
                            double[][] trainInput;
                            double[][] trainOutput;
                            double[][] testInput;
                            double[][] testOutput;
                            Dictionary<double, int> map;

                            Program.MergeAndTransformWithOneLabel<double>(lstTrain as IList<LabeledVector<double>>, lstTest as IList<LabeledVector<double>>, out trainInput, out trainOutput, out testInput, out testOutput, out map);

                            Encog.ML.SVM.SupportVectorMachine svm = new Encog.ML.SVM.SupportVectorMachine(trainInput[0].Length, false);

                            Encog.ML.Data.IMLDataSet trainingSet = new Encog.ML.Data.Basic.BasicMLDataSet(trainInput, trainOutput);

                            Encog.ML.Train.IMLTrain train = new Encog.ML.SVM.Training.SVMSearchTrain(svm, trainingSet);

                            int epoch = 0;
                            Stopwatch watch = Stopwatch.StartNew();
                            do
                            {
                                train.Iteration();
                                System.Diagnostics.Debug.WriteLine("Epoch #" + epoch + " Error:" + train.Error);
                                epoch++;
                            } while ((epoch < Program.MAX_SVM_ITERATIONS) && (train.Error > Program.MIN_SVM_ERROR));
                            watch.Stop();

                            double trainTime = ((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000;

                            Encog.ML.Data.IMLDataSet testingSet = new Encog.ML.Data.Basic.BasicMLDataSet(testInput, testOutput);

                            List<double> lstTestTimes = new List<double>();

                            int nCorrect = 0;
                            foreach (Encog.ML.Data.IMLDataPair pair in testingSet)
                            {
                                watch = Stopwatch.StartNew();
                                Encog.ML.Data.IMLData result = svm.Compute(pair.Input);
                                watch.Stop();

                                nCorrect += (result[0] == pair.Ideal[0] ? 1 : 0);

                                lstTestTimes.Add(((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000);
                            }

                            return new RunStatistics((double)nCorrect / lstTest.Count, trainTime, lstTestTimes);
                        }

                    #endregion
                    case BenchmarkAlgorithm.RBF:
                        #region RBF
                        {
                            double[][] trainInput;
                            double[][] trainOutput;
                            double[][] testInput;
                            double[][] testOutput;
                            Dictionary<double, int> map;

                            Program.MergeAndTransformWithOneLabel<double>(lstTrain as IList<LabeledVector<double>>, lstTest as IList<LabeledVector<double>>, out trainInput, out trainOutput, out testInput, out testOutput, out map);

                            Encog.Neural.RBF.RBFNetwork rbf = new Encog.Neural.RBF.RBFNetwork(trainInput[0].Length, (int)Math.Pow(Program.RBF_BASE_COUNT, trainInput[0].Length), 1, Encog.MathUtil.RBF.RBFEnum.Gaussian);
                            rbf.SetRBFCentersAndWidthsEqualSpacing(0.0, 1.0, Encog.MathUtil.RBF.RBFEnum.Gaussian, 2.0 / Math.Pow(Program.RBF_BASE_COUNT, trainInput[0].Length), true);    // Standard for hidden count = 2.0 / Math.Pow(Program.RBF_BASE_COUNT, trainInput[0].Length)

                            Encog.ML.Data.IMLDataSet trainingSet = new Encog.Neural.Data.Basic.BasicNeuralDataSet(trainInput, trainOutput);
                            Encog.Neural.Rbf.Training.SVDTraining train = new Encog.Neural.Rbf.Training.SVDTraining(rbf, trainingSet);

                            Stopwatch watch = Stopwatch.StartNew();
                            train.Iteration();  // For SVDTraining, you only need one iteration to train
                            watch.Stop();

                            double trainTime = ((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000;

                            Encog.Neural.NeuralData.INeuralDataSet testingSet = new Encog.Neural.Data.Basic.BasicNeuralDataSet(testInput, testOutput);

                            List<double> lstTestTimes = new List<double>();

                            int nCorrect = 0;
                            foreach (Encog.ML.Data.IMLDataPair pair in testingSet)
                            {
                                watch = Stopwatch.StartNew();
                                Encog.ML.Data.IMLData result = rbf.Compute(pair.Input);
                                watch.Stop();

                                nCorrect += (Math.Round(result[0], MidpointRounding.AwayFromZero) == pair.Ideal[0] ? 1 : 0);

                                lstTestTimes.Add(((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000);
                            }

                            return new RunStatistics((double)nCorrect / lstTest.Count, trainTime, lstTestTimes);
                        }

                    #endregion
                    case BenchmarkAlgorithm.NEAT:
                        #region NEAT
                        {
                            double[][] trainInput;
                            double[][] trainOutput;
                            double[][] testInput;
                            double[][] testOutput;
                            Dictionary<double, int> map;

                            Program.MergeAndTransform<double>(lstTrain as IList<LabeledVector<double>>, lstTest as IList<LabeledVector<double>>, out trainInput, out trainOutput, out testInput, out testOutput, out map);

                            Encog.ML.Data.IMLDataSet trainingSet = new Encog.ML.Data.Basic.BasicMLDataSet(trainInput, trainOutput);
                            Encog.Neural.NEAT.NEATPopulation population = new Encog.Neural.NEAT.NEATPopulation(trainInput[0].Length, trainOutput[0].Length, Program.NEAT_POPULATION_COUNT);

                            population.InitialConnectionDensity = 1.0;
                            population.Reset();

                            Encog.Neural.Networks.Training.ICalculateScore score = new Encog.Neural.Networks.Training.TrainingSetScore(trainingSet);

                            Encog.ML.EA.Train.IEvolutionaryAlgorithm train = Encog.Neural.NEAT.NEATUtil.ConstructNEATTrainer(population, score);

                            int epoch = 0;
                            Stopwatch watch = Stopwatch.StartNew();
                            do
                            {
                                train.Iteration();
                                System.Diagnostics.Debug.WriteLine("Epoch #" + epoch + " Error:" + train.Error);
                                epoch++;
                            } while ((epoch < Program.MAX_NN_ITERATIONS) && (train.Error > Program.MIN_NN_ERROR));
                            watch.Stop();

                            double trainTime = ((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000;

                            Encog.Neural.NEAT.NEATNetwork network = (Encog.Neural.NEAT.NEATNetwork)train.CODEC.Decode(train.BestGenome);

                            Encog.Neural.NeuralData.INeuralDataSet testingSet = new Encog.Neural.Data.Basic.BasicNeuralDataSet(testInput, testOutput);

                            List<double> lstTestTimes = new List<double>();

                            int nCorrect = 0;
                            foreach (Encog.ML.Data.IMLDataPair pair in testingSet)
                            {
                                watch = Stopwatch.StartNew();
                                Encog.ML.Data.IMLData result = network.Compute(pair.Input);
                                watch.Stop();

                                #region Result

                                int maxIndex = -1;
                                double max = double.MinValue;
                                for (int i = 0; i < result.Count; i++)
                                {
                                    if (result[i] > max)
                                    {
                                        maxIndex = i;
                                        max = result[i];
                                    }
                                }

                                #endregion

                                #region Ideal

                                int maxIdealIndex = -1;
                                max = double.MinValue;
                                for (int i = 0; i < pair.Ideal.Count; i++)
                                {
                                    if (pair.Ideal[i] > max)
                                    {
                                        maxIdealIndex = i;
                                        max = result[i];
                                    }
                                }

                                #endregion

                                nCorrect += (maxIndex == maxIdealIndex ? 1 : 0);

                                lstTestTimes.Add(((double)watch.ElapsedTicks / Stopwatch.Frequency) * 1000);
                            }

                            return new RunStatistics((double)nCorrect / lstTest.Count, trainTime, lstTestTimes);
                        }

                    #endregion
                    default:
                        throw new NotImplementedException();
                }

                throw new NotImplementedException();
            }
        }

        public static void SerializeVectors<L>(DataSet dataSet, IList<IList<LabeledVector<L>>> train, IList<IList<LabeledVector<L>>> test)
        {
            DateTime dtNow = DateTime.Now;

            if (!System.IO.Directory.Exists(string.Format(@".\SavedBenchmarkedDataSets\{0}", dataSet)))
            {
                System.IO.Directory.CreateDirectory(string.Format(@".\SavedBenchmarkedDataSets\{0}", dataSet));
            }

            for (int i = 0; i < train.Count; i++)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(string.Format(@".\SavedBenchmarkedDataSets\{0}\train{1:s}_{2}.csv", dataSet, dtNow, i).Replace(":", "")))
                {
                    Exporter.Serialize<L>(train[i], sw);
                }
            }

            for (int i = 0; i < test.Count; i++)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(string.Format(@".\SavedBenchmarkedDataSets\{0}\test{1:s}_{2}.csv", dataSet, dtNow, i).Replace(":", "")))
                {
                    Exporter.Serialize<L>(test[i], sw);
                }
            }
        }

        public static void DeserializeVectors<L>(DataSet dataSet, string key, out List<IList<LabeledVector<L>>> train, out List<IList<LabeledVector<L>>> test)
        {
            // This is a BLIND, naive import

            train = new List<IList<LabeledVector<L>>>();
            foreach (string file in System.IO.Directory.GetFiles(string.Format(@".\SavedBenchmarkedDataSets\{0}", dataSet), string.Format("train{0}_*.csv", key), System.IO.SearchOption.TopDirectoryOnly))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(file))
                {
                    train.Add(Importer.Import<L>(sr).ToList());
                }
            }

            test = new List<IList<LabeledVector<L>>>();
            foreach (string file in System.IO.Directory.GetFiles(string.Format(@".\SavedBenchmarkedDataSets\{0}", dataSet), string.Format("test{0}_*.csv", key), System.IO.SearchOption.TopDirectoryOnly))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(file))
                {
                    test.Add(Importer.Import<L>(sr).ToList());
                }
            }
        }

        public static List<string> GetSerializedVectorsKeys(DataSet dataSet)
        {
            HashSet<string> keys = new HashSet<string>();

            foreach (string file in System.IO.Directory.GetFiles(string.Format(@".\SavedBenchmarkedDataSets\{0}", dataSet), "*.csv", System.IO.SearchOption.TopDirectoryOnly))
            {
                Match match = Regex.Match(file, @"train(.+?)_");
                if (match.Length > 0)
                {
                    keys.Add(match.Groups[1].Value);
                }
            }

            return keys.ToList();
        }

        public static void MergeAndTransform<L>(IList<LabeledVector<L>> train, IList<LabeledVector<L>> test, out double[][] trainFeatures, out double[][] trainLabels, out double[][] testFeatures, out double[][] testLabels, out Dictionary<L, int> map)
        {
            IList<LabeledVector<L>> combined = train.Concat(test).ToList();

            double[][] features;
            double[][] labels;
            Transformer.Transform<L>(combined, out features, out labels, out map);

            trainFeatures = new double[train.Count][];
            trainLabels = new double[train.Count][];
            testFeatures = new double[test.Count][];
            testLabels = new double[test.Count][];

            Array.Copy(features, 0, trainFeatures, 0, train.Count);
            Array.Copy(labels, 0, trainLabels, 0, train.Count);
            Array.Copy(features, train.Count, testFeatures, 0, test.Count);
            Array.Copy(labels, train.Count, testLabels, 0, test.Count);
        }

        public static void MergeAndTransformWithOneLabel<L>(IList<LabeledVector<L>> train, IList<LabeledVector<L>> test, out double[][] trainFeatures, out double[][] trainLabels, out double[][] testFeatures, out double[][] testLabels, out Dictionary<L, int> map)
        {
            IList<LabeledVector<L>> combined = train.Concat(test).ToList();

            double[][] features;
            double[][] labels;
            Transformer.TransformWithOneLabel<L>(combined, out features, out labels, out map);

            trainFeatures = new double[train.Count][];
            trainLabels = new double[train.Count][];
            testFeatures = new double[test.Count][];
            testLabels = new double[test.Count][];

            Array.Copy(features, 0, trainFeatures, 0, train.Count);
            Array.Copy(labels, 0, trainLabels, 0, train.Count);
            Array.Copy(features, train.Count, testFeatures, 0, test.Count);
            Array.Copy(labels, train.Count, testLabels, 0, test.Count);
        }
    }

    public enum BenchmarkAlgorithm
    {
        [IsRHCSphereBenchmark(false)]
        RHCEuclidean,
        [IsRHCSphereBenchmark]
        RHCSquaredEuclidean,
        [IsRHCSphereBenchmark]
        RHCLDASquaredEuclidean,
        [IsRHCSphereBenchmark(false)]
        RHCLDAEuclidean,
        [IsRHCSphereBenchmark]
        RHCDisjointMidpoint,
        [IsRHCSphereBenchmark]
        RHCMaxMargin,
        BPNN,
        SVM,
        [Obsolete]
        RBF,    // Doesn't really work
        NEAT
    }

    // Can't use horse colic data set (368 exemplars) because A LOT of missing values
    public enum DataSet
    {
        IrisDataSet,
        WisconsinBreastCancerDataSet,
        WineDataSet,
        NewThyroid,
        Glass,
        BalanceScale,
        Credit,
        Dermatology,
        PimaDiabetes,
        EColi,
        ClevelandHeartDisease,
        Urban,
        Zoo,
        Ionosphere
    }

    public static class EnumHelper
    {
        public static bool HasAttributeOfType<T>(this Enum value) where T : System.Attribute
        {
            return EnumHelper.GetAttributeOfType<T>(value) != null;
        }

        public static T GetAttributeOfType<T>(this Enum value) where T : System.Attribute
        {
            Type type = value.GetType();
            System.Reflection.MemberInfo[] info = type.GetMember(value.ToString());
            object[] attributes = info[0].GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? (T)attributes[0] : null;
        }
    }

    public class IsRHCSphereBenchmarkAttribute : Attribute
    {
        public IsRHCSphereBenchmarkAttribute(bool useSquaredEuclidean = true) : base()
        {
            this.UseSquaredEuclidean = useSquaredEuclidean;
        }

        public bool UseSquaredEuclidean { get; private set; }
    }

    public static class ExtensionMethods
    {
        public static double StandardDeviation(this IEnumerable<double> values)
        {
            double fStdDev = 0.0;
            if (values.Any())
            {
                double fAverage = values.Average();
                double fSum = values.Sum(v => Math.Pow(v - fAverage, 2));

                fStdDev = Math.Sqrt((fSum) / (values.Count() - 1));
            }

            return fStdDev;
        }

        public static double StandardDeviation(this IEnumerable<int> values)
        {
            return ExtensionMethods.StandardDeviation(values.Select(v => (double)v));
        }
    }

    public class RunStatistics
    {
        /// <summary>
        /// The statistics of a run.
        /// </summary>
        /// <param name="correctPercentage">The correct percentage.</param>
        /// <param name="trainTime">The train time in milliseconds.</param>
        /// <param name="testTimes">A list of test times in milliseconds.</param>
        public RunStatistics(double correctPercentage, double trainTime, IEnumerable<double> testTimes) : this(correctPercentage, trainTime, testTimes, null, null)
        {

        }

        /// <summary>
        /// The statistics of a run.
        /// </summary>
        /// <param name="correctPercentage">The correct percentage.</param>
        /// <param name="trainTime">The train time in milliseconds.</param>
        /// <param name="testTimes">A list of test times in milliseconds.</param>
        /// <param name="treeHeight">The tree height of the RHC structure.</param>
        /// <param name="sphereCount">The sphere count of the RHC structure.</param>
        public RunStatistics(double correctPercentage, double trainTime, IEnumerable<double> testTimes, int? treeHeight, int? sphereCount)
        {
            this.CorrectPercentage = correctPercentage;
            this.TrainTime = trainTime;
            this.TestTimes = testTimes;
            this.TreeHeight = treeHeight;
            this.SphereCount = sphereCount;
        }

        public double CorrectPercentage { get; set; }

        public int? SphereCount { get; set; }

        /// <summary>
        /// Test times (in milliseconds)
        /// </summary>
        public IEnumerable<double> TestTimes { get; set; }

        /// <summary>
        /// Train time (in milliseconds)
        /// </summary>
        public double TrainTime { get; set; }

        public int? TreeHeight { get; set; }
    }
}
