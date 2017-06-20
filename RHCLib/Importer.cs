using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Importer
    {
        public delegate T ValueConverterHandler<T>(string value, int column);
        public delegate void LinePreprocessorHandler(LinePreprocessorEventArgs e);

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream)
        {
            return Importer.Import<L>(stream, 0);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, ",");
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, ClassColumn.LastColumn);
        }

        public enum ClassColumn
        {
            FirstColumn,
            LastColumn
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, column, null);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column, int[] columnsToDiscard)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, column, columnsToDiscard, null);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column, int[] columnsToDiscard, ValueConverterHandler<double> converterFeature)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, column, columnsToDiscard, converterFeature, null);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column, int[] columnsToDiscard, ValueConverterHandler<double> converterFeature, ValueConverterHandler<L> converterLabel)
        {
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, column, columnsToDiscard, converterFeature, converterLabel, null);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column, int[] columnsToDiscard, ValueConverterHandler<double> converterFeature, ValueConverterHandler<L> converterLabel, LinePreprocessorHandler preprocessor)
        {
            List<LabeledVector<L>> lstVectors = new List<LabeledVector<L>>();

            string strLine;
            string[] rgLine;
            double[] rgFeatures;
            L label;

            #region Discard Rows

            for (int i = 0; i < nRowsToDiscard; i++)
            {
                stream.ReadLine();
            }

            #endregion

            while ((strLine = stream.ReadLine()) != null)
            {
                if (strLine.Trim().Length > 0)
                {
                    LinePreprocessorEventArgs e = null;
                    if (preprocessor != null)
                    {
                        preprocessor((e = new LinePreprocessorEventArgs(strLine)));
                        strLine = e.Line;
                    }

                    if (e == null || !e.SkipLine)
                    {
                        rgLine = strLine.Split(new string[] { strDelimiter }, StringSplitOptions.None);
                        rgFeatures = new double[rgLine.Length - 1 - (columnsToDiscard != null ? columnsToDiscard.Length : 0)];

                        int currentIndex = 0;
                        for (int i = (column == ClassColumn.FirstColumn ? 1 : 0); i < (column == ClassColumn.FirstColumn ? rgLine.Length : rgLine.Length - 1); i++)
                        {
                            if (columnsToDiscard == null || !columnsToDiscard.Contains(i))
                            {
                                if (converterFeature != null)
                                {
                                    rgFeatures[currentIndex] = converterFeature(rgLine[i], i);
                                }
                                else
                                {
                                    rgFeatures[currentIndex] = (double)Convert.ChangeType(rgLine[i], typeof(double), null);
                                }

                                currentIndex++;
                            }
                        }

                        if (converterLabel != null)
                        {
                            label = converterLabel(rgLine[column == ClassColumn.FirstColumn ? 0 : rgLine.Length - 1], -1);
                        }
                        else
                        {
                            label = (L)Convert.ChangeType(rgLine[column == ClassColumn.FirstColumn ? 0 : rgLine.Length - 1], typeof(L), null);
                        }

                        lstVectors.Add(new LabeledVector<L>(label, rgFeatures));
                    }
                }
            }

            return lstVectors.ToArray();
        }
    }

    public class LinePreprocessorEventArgs : EventArgs
    {
        public LinePreprocessorEventArgs(string line) : base()
        {
            this.Line = line;
            this.SkipLine = false;
        }

        public string Line { get; set; }

        public bool SkipLine { get; set; }
    }
}
