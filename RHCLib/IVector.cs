using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public interface IVector
    {
        double GetFeature(int nIndex);
        void SetFeature(int nIndex, double value);

        double[] Features { get; }
        int Rank { get; }
    }
}
