using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Minkowski
    {
        public static DistanceDelegate CreateMinkowskiDistanceDelegate(double fOrder)
        {
            if (fOrder > 0.0)
            {
                return new DistanceDelegate((a, b) =>
                    {
                        if (a != null && b != null)
                        {
                            if (a.Rank.Equals(b.Rank))
                            {
                                double fDistance = 0.0;
                                for (int i = 0; i < a.Rank; i++)
                                {
                                    fDistance += Math.Pow(Math.Abs(a.GetFeature(i) - b.GetFeature(i)), fOrder);
                                }

                                return Math.Pow(fDistance, (1.0 / fOrder));
                            }
                            else
                            {
                                throw new RankException();
                            }
                        }
                        else
                        {
                            throw new ArgumentNullException();
                        }
                    });
            }
            else
            {
                throw new ArgumentOutOfRangeException("The order of the Minkowski distance must be greater than zero.");
            }
        }
    }
}
