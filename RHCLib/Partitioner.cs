using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Partitioner
    {
        private static readonly Random s_rand = new Random(Guid.NewGuid().GetHashCode());

        public static void Partition<T>(IList<T> lstSource, int nFirstPartitionCount, out IList<T> lstFirstPartition, out IList<T> lstSecondPartition)
        {
            Partitioner.Partition<T>(lstSource, 0, nFirstPartitionCount, out lstFirstPartition, out lstSecondPartition);
        }

        public static void Partition<T>(IList<T> lstSource, int nStartAt, int nFirstPartitionCount, out IList<T> lstFirstPartition, out IList<T> lstSecondPartition)
        {
            if (lstSource != null)
            {
                if (nFirstPartitionCount <= lstSource.Count)
                {
                    lstFirstPartition = new List<T>();
                    lstSecondPartition = new List<T>();

                    nStartAt = nStartAt % lstSource.Count;
                    while (lstFirstPartition.Count < nFirstPartitionCount)
                    {
                        lstFirstPartition.Add(lstSource[nStartAt]);
                        nStartAt = (nStartAt + 1) >= lstSource.Count ? 0 : (nStartAt + 1);
                    }

                    while (lstSecondPartition.Count < lstSource.Count - nFirstPartitionCount)
                    {
                        lstSecondPartition.Add(lstSource[nStartAt]);
                        nStartAt = (nStartAt + 1) >= lstSource.Count ? 0 : (nStartAt + 1);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public static IList<T[]> EqualPartition<T>(IList<T> lstSource, int nGroups)
        {
            if (lstSource != null)
            {
                if (nGroups <= lstSource.Count)
                {
                    IList<T[]> lstGroups = new List<T[]>();

                    int nIndex = 0;
                    int nCount = nGroups;
                    int nLength = lstSource.Count;
                    for (int x = 0; x < nGroups; x++)
                    {
                        lstGroups.Add(new T[lstSource.Count / nGroups + (nLength % nCount > 0 ? 1 : 0)]);
                        for (int y = nIndex, z = 0; z < lstGroups[x].Length; y++, z++)
                        {
                            lstGroups[x][z] = lstSource[y];
                        }

                        nIndex += lstGroups[x].Length;
                        nCount--;
                        nLength -= lstGroups[x].Length;
                    }

                    return lstGroups;
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public static void RandomPartition<T>(IList<T> lstSource, int nFirstPartitionCount, out IList<T> lstFirstPartition, out IList<T> lstSecondPartition)
        {
            if (lstSource != null)
            {
                if (nFirstPartitionCount <= lstSource.Count)
                {
                    lstFirstPartition = new List<T>();
                    lstSecondPartition = new List<T>();

                    List<T> lstDupSource = new List<T>(lstSource);
                    int nIndex;

                    while (lstFirstPartition.Count < nFirstPartitionCount)
                    {
                        lstFirstPartition.Add(lstDupSource[(nIndex = Partitioner.s_rand.Next(0, lstDupSource.Count))]);
                        lstDupSource.RemoveAt(nIndex);
                    }

                    foreach (T element in lstDupSource)
                    {
                        lstSecondPartition.Add(element);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                throw new ArgumentNullException();
            }
        }
    }
}