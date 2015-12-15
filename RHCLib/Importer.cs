using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Importer
    {
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
            return Importer.Import<L>(stream, nRowsToDiscard, strDelimiter, column, null, null);
        }

        public static LabeledVector<L>[] Import<L>(System.IO.StreamReader stream, int nRowsToDiscard, string strDelimiter, ClassColumn column, IPCLValueConverter<double> converterFeature, IPCLValueConverter<L> converterLabel)
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
                    rgLine = strLine.Split(new string[] { strDelimiter }, StringSplitOptions.None);
                    rgFeatures = new double[rgLine.Length - 1];
                    for (int i = (column == ClassColumn.FirstColumn ? 1 : 0); i < (column == ClassColumn.FirstColumn ? rgLine.Length : rgLine.Length - 1); i++)
                    {
                        if (converterFeature != null)
                        {
                            rgFeatures[i - (column == ClassColumn.FirstColumn ? 1 : 0)] = converterFeature.Convert(rgLine[i]);
                        }
                        else
                        {
                            rgFeatures[i - (column == ClassColumn.FirstColumn ? 1 : 0)] = (double)Convert.ChangeType(rgLine[i], typeof(double), null);
                        }
                    }

                    if (converterLabel != null)
                    {
                        label = converterLabel.Convert(rgLine[column == ClassColumn.FirstColumn ? 0 : rgLine.Length - 1]);
                    }
                    else
                    {
                        label = (L)Convert.ChangeType(rgLine[column == ClassColumn.FirstColumn ? 0 : rgLine.Length - 1], typeof(L), null);
                    }

                    lstVectors.Add(new LabeledVector<L>(label, rgFeatures));
                }
            }

            return lstVectors.ToArray();
        }
    }

    public interface IPCLValueConverter<T>
    {
        T Convert(string value);
    }
}
