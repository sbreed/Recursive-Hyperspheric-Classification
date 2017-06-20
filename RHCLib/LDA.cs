using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public class LDA
    {
        public static int Prediction(double[][] data, double[] x, double[][] w)
        {
            // item to predict, x, is a normal array
            // convert array to 1-col matrix then call regular Prediction()
            double[][] xm = MatrixFromVector(x); // x to col matrix
            return Prediction(data, xm, w);
        }

        public static int Prediction(double[][] data, double[][] x, double[][] w)
        {
            // item to predict, x, is a 1-col matrix (column vector)
            int dim = data[0].Length - 1; // at least 2
            double[][] wT = MatrixTranspose(w); // 1 x d
            double[][] m0 = Mean(data, 0); // d x 1
            double[][] m1 = Mean(data, 1); // d x 1
            double[][] m = MatrixAdd(m0, m1); // d x 1
            m = MatrixProduct(m, 0.5); // d x 1 

            // threshold constant c (distance from origin)
            double[][] tc = MatrixProduct(wT, m); // ((1xd)(dx1) = 1 x 1
                                                  // distance from origin
            double[][] wTx = MatrixProduct(wT, x); // (1xd)(dx1) = 1 x 1

            double adjust = 0.0;

            // optional weighting by probabilities:
            //int n0 = 0; int n1 = 0; // number each class
            //for (int i = 0; i < data.Length; ++i)
            //{
            //  int label = (int)data[i][dim];
            //  if (label == 0)
            //    ++n0;
            //  else
            //    ++n1;
            //}
            //double p0 = (n0 * 1.0) / data.Length;
            //double p1 = (n1 * 1.0) / data.Length;
            //double adjust = Math.Log(p0 / p1);

            if (wTx[0][0] - adjust > tc[0][0]) // a bit tricky
                return 0;
            else
                return 1;
        }

        public static bool IsCompletelySeparatedWithDiscriminant(double[][] data, double[][] w)
        {
            // item to predict, x, is a 1-col matrix (column vector)
            int dim = data[0].Length - 1; // at least 2
            double[][] wT = MatrixTranspose(w); // 1 x d
            double[][] m0 = Mean(data, 0); // d x 1
            double[][] m1 = Mean(data, 1); // d x 1
            double[][] m = MatrixAdd(m0, m1); // d x 1
            m = MatrixProduct(m, 0.5); // d x 1 

            // threshold constant c (distance from origin)
            double[][] tc = MatrixProduct(wT, m); // ((1xd)(dx1) = 1 x 1
                                                  // distance from origin

            double c1Min = double.MaxValue;
            double c1Max = double.MinValue;
            double c2Min = double.MaxValue;
            double c2Max = double.MinValue;

            double c1 = data[0][dim];

            foreach (double[] datum in data)
            {
                double[] v = new double[dim];   // Vector without class label
                Array.Copy(datum, v, v.Length);

                double[][] wTdatum = MatrixProduct(wT, MatrixFromVector(v));

                if (c1 == datum[dim])
                {
                    #region Class 1

                    if (wTdatum[0][0] > c1Max)
                    {
                        c1Max = wTdatum[0][0];
                    }
                    if (wTdatum[0][0] < c1Min)
                    {
                        c1Min = wTdatum[0][0];
                    }

                    #endregion
                }
                else
                {
                    #region Class 2

                    if (wTdatum[0][0] > c2Max)
                    {
                        c2Max = wTdatum[0][0];
                    }
                    if (wTdatum[0][0] < c2Min)
                    {
                        c2Min = wTdatum[0][0];
                    }

                    #endregion
                }
            }

            return (c1Min <= c2Min && c1Max <= c2Min) ||
                (c2Min <= c1Min && c2Max <= c1Min);
        }

        public static bool IsCompletelySeparatedWithDiscriminant<L>(IEnumerable<LabeledVector<L>> ds1, IEnumerable<LabeledVector<L>> ds2, Sphere<L> sphere, out Discriminant<L> discriminant)
        {
            // Concats the vectors into a matrix WITH the included classes (0.0 = ds1 class, 1.0 = ds2 class)
            double[][] dataConcat = ds1.Select(v => v.Features.Concat(Enumerable.Repeat<double>(0.0, 1)).ToArray()).Concat(ds2.Select(v => v.Features.Concat(Enumerable.Repeat<double>(1.0, 1)).ToArray())).ToArray();
            double[][] w = LDA.Discriminate(dataConcat, true);

            if (LDA.IsCompletelySeparatedWithDiscriminant(dataConcat, w))
            {
                #region Find the midpoint

                // We could also set the midpoint to be the midpoint between the two closest points (that are conflicting) on the projection

                double[][] wT = MatrixTranspose(w); // 1 x d
                double[][] m0 = Mean(dataConcat, 0); // d x 1
                double[][] m1 = Mean(dataConcat, 1); // d x 1
                double[][] m = MatrixAdd(m0, m1); // d x 1
                m = MatrixProduct(m, 0.5); // d x 1 

                double midpoint = MatrixProduct(wT, m)[0][0];

                #endregion

                #region Projected Means

                // If m0's projected mean is less than midpoint, than left of the midpoint is class0.  The converse is true.
                double m0Proj = MatrixProduct(wT, m0)[0][0];

                #endregion

                discriminant = new Discriminant<L>(w, midpoint, m0Proj <= midpoint ? ds1.First().Label : ds2.First().Label, m0Proj <= midpoint ? ds2.First().Label : ds1.First().Label, sphere);
                return true;
            }
            else
            {
                discriminant = null;
                return false;
            }
        }

        public static double[][] Mean(double[][] data, int c)
        {
            // return mean of class c (0 or 1) as column vector
            int dim = data[0].Length - 1;
            double[][] result = MatrixCreate(dim, 1);
            int ct = 0;

            for (int i = 0; i < data.Length; ++i)
            {
                int label = (int)data[i][dim];
                if (label == c)
                {
                    for (int j = 0; j < dim; ++j)
                        result[j][0] += data[i][j];
                    ++ct;
                }
            } // i

            for (int i = 0; i < dim; ++i)
                result[i][0] /= ct;

            return result;
        } // Mean

        public static double[][] Discriminate(double[][] data, bool unitize)
        {
            // returns the w vector as a column 
            // calls helper ScatterWithin which calls Scatter
            double[][] mean0 = Mean(data, 0);
            double[][] mean1 = Mean(data, 1);
            //System.Diagnostics.Debug.WriteLine("\nClass means: \n");
            ShowMatrix(mean0, 2, true);
            ShowMatrix(mean1, 2, true);

            double[][] Sw = ScatterWithin(data); // sum of S0 and S1
            //System.Diagnostics.Debug.WriteLine("The within-class combined scatter matrix Sw: \n");
            ShowMatrix(Sw, 4, true);

            double[][] SwInv = MatrixInverse(Sw);

            double[][] diff = MatrixSubtract(mean0, mean1);
            double[][] w = MatrixProduct(SwInv, diff);

            if (unitize == true)
                return Unitize(w);
            else
                return w;
        }

        public static double[][] ScatterWithin(double[][] data)
        {
            // Sw = within class scatter = S0 + S1
            double[][] S0 = Scatter(data, 0);
            double[][] S1 = Scatter(data, 1);
            //System.Diagnostics.Debug.WriteLine("Scatter matrices S0 S1: \n");
            ShowMatrix(S0, 4, true);
            ShowMatrix(S1, 4, true);
            double[][] Sw = MatrixAdd(S0, S1);
            return Sw;
        }

        public static double[][] Scatter(double[][] data, int c)
        {
            // scatter matrix of class c
            // [Sw (within class) is sum of class scatters]
            int dim = data[0].Length - 1;
            double[][] mean = Mean(data, c); // mean as col vector
            double[][] result = MatrixCreate(dim, dim); // d x d
            for (int i = 0; i < data.Length; ++i)
            {
                int label = (int)data[i][dim]; // 0 or 1
                if (label == c)
                {
                    double[][] x = MatrixCreate(dim, 1); // d x 1
                    for (int j = 0; j < dim; ++j)
                        x[j][0] = data[i][j];
                    double[][] diff = MatrixSubtract(x, mean); // d x 1
                    double[][] diffT = MatrixTranspose(diff);  // 1 x d
                    double[][] prod = MatrixProduct(diff, diffT); // d x d

                    result = MatrixAdd(result, prod); // accumulate
                }
            } // i
            return result;
        } // Scatter

        public static double[][] Unitize(double[][] vector)
        {
            // return column vector scaled to unit length
            if (vector[0].Length != 1)
                throw new Exception("Not column vector");

            int len = vector.Length;
            double[][] result = MatrixCreate(len, 1);
            double sum = 0.0;
            for (int i = 0; i < len; ++i)
                sum += vector[i][0] * vector[i][0];
            sum = Math.Sqrt(sum); // check if 0
            for (int i = 0; i < len; ++i)
                result[i][0] = vector[i][0] / sum;
            return result;
        }

        // ============================================

        //static double[][] Covariance(double[][] data, int c, bool sample)
        //{
        //  // covariance is essentially Scatter divided by number items
        //  int n = 0;
        //  int dim = data[0].Length - 1;
        //  for (int i = 0; i < data.Length; ++i)
        //  {
        //    int label = (int)data[i][dim];
        //    if (label == c)
        //      ++n;
        //  }
        //  double[][] s = Scatter(data, c);
        //  double[][] result = MatrixDuplicate(s);
        //  for (int i = 0; i < dim; ++i)
        //  {
        //    for (int j = 0; j < dim; ++j)
        //    {
        //      if (sample == true)
        //        result[i][j] /= n - 1;
        //      else
        //        result[i][j] /= n;
        //    }
        //  }
        //  return result;
        //} // Covariance

        //static double[][] Pooled(double[][] data, bool sample)
        //{
        //  // pooled covariance: (n1*C1 + n2*C2) / (n1 + n2)
        //  int n0 = 0; int n1 = 0;
        //  int dim = data[0].Length - 1;
        //  for (int i = 0; i < data.Length; ++i)
        //  {
        //    int label = (int)data[i][dim]; // 0 or 1
        //    if (label == 0) ++n0; else ++n1;
        //  }

        //  double[][] C0 = Covariance(data, 0, sample);
        //  double[][] C1 = Covariance(data, 1, sample);

        //  double[][] result = MatrixCreate(dim, dim);
        //  for (int i = 0; i < dim; ++i)
        //    for (int j = 0; j < dim; ++j)
        //      result[i][j] = (n0 * C0[i][j] + n1 * C1[i][j]) / (n0 + n1);
        //  return result;
        //} // Pooled

        // -------------------------------------------------------------
        // Matrix methods below
        // -------------------------------------------------------------

        public static double[][] MatrixInverse(double[][] matrix)
        {
            int n = matrix.Length;
            double[][] result = MatrixDuplicate(matrix);

            int[] perm;
            int toggle;
            double[][] lum = MatrixDecompose(matrix, out perm, out toggle);
            if (lum == null)
                throw new Exception("Unable to compute inverse");

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }

                double[] x = HelperSolve(lum, b); // use decomposition

                for (int j = 0; j < n; ++j)
                    result[j][i] = x[j];
            }
            return result;
        }

        // -------------------------------------------------------------

        public static double[] HelperSolve(double[][] luMatrix, double[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.Length;
            double[] x = new double[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1][n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum / luMatrix[i][i];
            }

            return x;
        }

        // -------------------------------------------------------------

        public static double[][] MatrixDuplicate(double[][] matrix)
        {
            // allocates/creates a duplicate of a matrix
            double[][] result = MatrixCreate(matrix.Length, matrix[0].Length);
            for (int i = 0; i < matrix.Length; ++i) // copy the values
                for (int j = 0; j < matrix[i].Length; ++j)
                    result[i][j] = matrix[i][j];
            return result;
        }

        // -------------------------------------------------------------

        public static double[][] MatrixDecompose(double[][] matrix, out int[] perm,
          out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // returns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            if (rows != cols)
                throw new Exception("Non-square mattrix");

            int n = rows; // convenience

            double[][] result = MatrixDuplicate(matrix); // 

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps

            for (int j = 0; j < n - 1; ++j) // each column
            {
                double colMax = Math.Abs(result[j][j]);
                int pRow = j;
                //for (int i = j + 1; i < n; ++i) // deprecated
                //{
                //  if (result[i][j] > colMax)
                //  {
                //    colMax = result[i][j];
                //    pRow = i;
                //  }
                //}

                for (int i = j + 1; i < n; ++i) // reader Matt V needed this:
                {
                    if (Math.Abs(result[i][j]) > colMax)
                    {
                        colMax = Math.Abs(result[i][j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    double[] rowPtr = result[pRow];
                    result[pRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // -------------------------------------------------------------
                // This part added later (not in original code) 
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row 
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j

                if (result[j][j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row][j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        throw new Exception("Cannot use Doolittle's method");

                    // swap rows so 0.0 no longer on diagonal
                    double[] rowPtr = result[goodRow];
                    result[goodRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // -------------------------------------------------------------

                //if (Math.Abs(result[j][j]) < 1.0E-20) // deprecated
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i][j] /= result[j][j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i][k] -= result[i][j] * result[j][k];
                    }
                }

            } // main j column loop

            return result;
        } // MatrixDecompose

        // -------------------------------------------------------------

        public static double[][] MatrixProduct(double[][] matrixA, double[][] matrixB)
        {
            int aRows = matrixA.Length; int aCols = matrixA[0].Length;
            int bRows = matrixB.Length; int bCols = matrixB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");

            double[][] result = MatrixCreate(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use k < bRows
                        result[i][j] += matrixA[i][k] * matrixB[k][j];

            //Parallel.For(0, aRows, i =>
            //  {
            //    for (int j = 0; j < bCols; ++j) // each col of B
            //      for (int k = 0; k < aCols; ++k) // could use k < bRows
            //        result[i][j] += matrixA[i][k] * matrixB[k][j];
            //  }
            //);

            return result;
        }

        public static double[][] MatrixProduct(double[][] m, double x)
        {
            // multiple all cells in m by scalar x
            double[][] result = MatrixDuplicate(m); // copy
            for (int i = 0; i < result.Length; ++i)
                for (int j = 0; j < result[i].Length; ++j)
                    result[i][j] *= x;
            return result;
        }

        public static double[][] MatrixAdd(double[][] a, double[][] b)
        {
            // return a-b
            int rows = a.Length; int cols = a[0].Length;
            double[][] result = MatrixCreate(rows, cols);
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    result[i][j] = a[i][j] + b[i][j];
            return result;
        }

        public static double[][] MatrixSubtract(double[][] a, double[][] b)
        {
            // return a-b
            int rows = a.Length; int cols = a[0].Length;
            double[][] result = MatrixCreate(rows, cols);
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    result[i][j] = a[i][j] - b[i][j];
            return result;
        }

        public static double[][] MatrixTranspose(double[][] matrix)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            double[][] result = MatrixCreate(cols, rows); // note indexing
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; ++j)
                {
                    result[j][i] = matrix[i][j];
                }
            }
            return result;
        } // TransposeMatrix

        public static double[][] MatrixFromVector(double[] vector)
        {
            // return a column vector-matrix
            int len = vector.Length;
            double[][] result = MatrixCreate(len, 1); // 1 colum
            for (int i = 0; i < len; ++i)
                result[i][0] = vector[i];
            return result;
        }

        public static double[][] MatrixCreate(int rows, int cols)
        {
            // allocates/creates a matrix initialized to all 0.0
            // do error checking here
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        public static void ShowMatrix(double[][] m, int dec, bool newl)
        {
            for (int i = 0; i < m.Length; ++i)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < m[i].Length; ++j)
                {
                    if (m[i][j] >= 0.0) sb.Append(" "); // '+'
                    sb.Append(m[i][j].ToString("F" + dec) + "  ");
                }
                //System.Diagnostics.Debug.WriteLine(sb.ToString());
                //System.Diagnostics.Debug.WriteLine("");
            }
            //if (newl) System.Diagnostics.Debug.WriteLine("");
        }

        public static void ShowVector(double[] v, int dec)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < v.Length; ++i)
                sb.Append(v[i].ToString("F" + dec) + "  ");

            //System.Diagnostics.Debug.WriteLine(sb.ToString());
            //System.Diagnostics.Debug.WriteLine("");
        }
    }
}
