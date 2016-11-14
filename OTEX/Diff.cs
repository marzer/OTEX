using System;
using System.Collections.Generic;
using System.Linq;

namespace OTEX
{
    /// <summary>
    /// A diff generator based on Meyer's "An O(ND) Difference Algorithm and its Variations", 1986.
    /// This implementation is a generic, stripped-down and optimized version of Matthias Hertel's,
    /// found here: http://www.mathertel.de/diff/. The inline comments are from his original.
    /// </summary>
    public static class Diff
    {
        /// <summary>
        /// One individual calculated difference between two sets of input.
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Index of deletion range.
            /// </summary>
            public int DeleteStart { get; internal set; }

            /// <summary>
            /// Length of deletion range (0 == no deletion).
            /// </summary>
            public int DeleteLength { get; internal set; }

            /// <summary>
            /// Index of insertion range.
            /// </summary>
            public int InsertStart { get; internal set; }

            /// <summary>
            /// Length of insertion range (0 == no insertion).
            /// </summary>
            public int InsertLength { get; internal set; }
        }

        private class Data<T> where T : IEquatable<T>
        {

            /// <summary>Number of elements (lines).</summary>
            public readonly int Length;

            /// <summary>Buffer of numbers that will be compared.</summary>
            public readonly T[] data;

            /// <summary>
            /// Array of booleans that flag for modified data.
            /// This is the result of the diff.
            /// This means deletedA in the first Data or inserted in the second Data.
            /// </summary>
            public bool[] modified;

            /// <summary>
            /// Initialize the Diff-Data buffer.
            /// </summary>
            /// <param name="initData">reference to the buffer</param>
            public Data(T[] initData)
            {
                data = initData;
                Length = initData.Count();
                modified = new bool[Length + 2];
            }

        }

        /// <summary>
        /// Calculate a set of diffs for two sets of input.
        /// </summary>
        /// <param name="oldValues">Old version of the data.</param>
        /// <param name="newValues">New version of the data.</param>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <returns>An array of Items describing the differences.</returns>
        public static Item[] Calculate<T>(T[] oldValues, T[] newValues) where T : IEquatable<T>
        {
            Data<T> DataA = new Data<T>(oldValues);
            Data<T> DataB = new Data<T>(newValues);

            int MAX = DataA.Length + DataB.Length + 1;
            //vector for the (0,0) to (x,y) search
            int[] DownVector = new int[2 * MAX + 2];
            //vector for the (u,v) to (N,M) search
            int[] UpVector = new int[2 * MAX + 2];

            LongestCommonSubsequence(DataA, 0, DataA.Length, DataB, 0, DataB.Length, DownVector, UpVector);
            return CreateDiffs(DataA, DataB);
        }


        /// <summary>
        /// Shortest Middle Snake (SMS) implementation.
        /// </summary>
        /// <param name="DataA">sequence A</param>
        /// <param name="LowerA">lower bound of the actual range in DataA</param>
        /// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
        /// <param name="DataB">sequence B</param>
        /// <param name="LowerB">lower bound of the actual range in DataB</param>
        /// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
        /// <param name="DownVector">a vector for the (0,0) to (x,y) search.</param>
        /// <param name="UpVector">a vector for the (u,v) to (N,M) search.</param>
        /// <param name="retX">X value of the middle snake</param>
        /// <param name="retY">Y value of the middle snake</param>
        private static void ShortestMiddleSnake<T>(Data<T> DataA, int LowerA, int UpperA, Data<T> DataB, int LowerB, int UpperB,
          int[] DownVector, int[] UpVector, out int retX, out int retY) where T : IEquatable<T>
        {
            int MAX = DataA.Length + DataB.Length + 1;
            int DownK = LowerA - LowerB; // the k-line to start the forward search
            int UpK = UpperA - UpperB; // the k-line to start the reverse search
            int Delta = (UpperA - LowerA) - (UpperB - LowerB);
            bool oddDelta = (Delta & 1) != 0;
            int DownOffset = MAX - DownK;
            int UpOffset = MAX - UpK;
            int MaxD = ((UpperA - LowerA + UpperB - LowerB) / 2) + 1;

            // init vectors
            DownVector[DownOffset + DownK + 1] = LowerA;
            UpVector[UpOffset + UpK - 1] = UpperA;

            for (int D = 0; D <= MaxD; ++D)
            {

                // Extend the forward path.
                for (int k = DownK - D; k <= DownK + D; k += 2)
                {
                    // find the only or better starting point
                    int x, y;
                    if (k == DownK - D)
                    {
                        x = DownVector[DownOffset + k + 1]; // down
                    }
                    else
                    {
                        x = DownVector[DownOffset + k - 1] + 1; // a step to the right
                        if ((k < DownK + D) && (DownVector[DownOffset + k + 1] >= x))
                            x = DownVector[DownOffset + k + 1]; // down
                    }
                    y = x - k;

                    // find the end of the furthest reaching forward D-path in diagonal k.
                    while ((x < UpperA) && (y < UpperB) && (DataA.data[x].Equals(DataB.data[y])))
                    {
                        ++x;
                        ++y;
                    }
                    DownVector[DownOffset + k] = x;

                    // overlap ?
                    if (oddDelta && (UpK - D < k) && (k < UpK + D))
                    {
                        if (UpVector[UpOffset + k] <= DownVector[DownOffset + k])
                        {
                            retX = DownVector[DownOffset + k];
                            retY = DownVector[DownOffset + k] - k;
                            return;
                        }
                    }

                }

                // Extend the reverse path.
                for (int k = UpK - D; k <= UpK + D; k += 2)
                {
                    // find the only or better starting point
                    int x, y;
                    if (k == UpK + D)
                        x = UpVector[UpOffset + k - 1]; // up
                    else
                    {
                        x = UpVector[UpOffset + k + 1] - 1; // left
                        if ((k > UpK - D) && (UpVector[UpOffset + k - 1] < x))
                            x = UpVector[UpOffset + k - 1]; // up
                    }
                    y = x - k;

                    while ((x > LowerA) && (y > LowerB) && (DataA.data[x - 1].Equals(DataB.data[y - 1])))
                    {
                        --x;
                        --y;
                    }
                    UpVector[UpOffset + k] = x;

                    // overlap ?
                    if (!oddDelta && (DownK - D <= k) && (k <= DownK + D))
                    {
                        if (UpVector[UpOffset + k] <= DownVector[DownOffset + k])
                        {
                            retX = DownVector[DownOffset + k];
                            retY = DownVector[DownOffset + k] - k;
                            return;
                        }
                    }
                }
            }

            throw new ApplicationException("the algorithm should never come here.");
        }


        /// <summary>
        /// This is the divide-and-conquer implementation of the longes common-subsequence (LCS) 
        /// algorithm.
        /// The published algorithm passes recursively parts of the A and B sequences.
        /// To avoid copying these arrays the lower and upper bounds are passed while the sequences stay constant.
        /// </summary>
        /// <param name="DataA">sequence A</param>
        /// <param name="LowerA">lower bound of the actual range in DataA</param>
        /// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
        /// <param name="DataB">sequence B</param>
        /// <param name="LowerB">lower bound of the actual range in DataB</param>
        /// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
        /// <param name="DownVector">a vector for the (0,0) to (x,y) search. Passed as a parameter for speed reasons.</param>
        /// <param name="UpVector">a vector for the (u,v) to (N,M) search. Passed as a parameter for speed reasons.</param>
        private static void LongestCommonSubsequence<T>(Data<T> DataA, int LowerA, int UpperA, Data<T> DataB, int LowerB,
            int UpperB, int[] DownVector, int[] UpVector)
             where T : IEquatable<T>
        {
            // Fast walkthrough equal lines at the start
            while (LowerA < UpperA && LowerB < UpperB && DataA.data[LowerA].Equals(DataB.data[LowerB]))
            {
                ++LowerA;
                ++LowerB;
            }

            // Fast walkthrough equal lines at the end
            while (LowerA < UpperA && LowerB < UpperB && DataA.data[UpperA - 1].Equals(DataB.data[UpperB - 1]))
            {
                --UpperA;
                --UpperB;
            }

            if (LowerA == UpperA)
            {
                // mark as inserted lines.
                while (LowerB < UpperB)
                    DataB.modified[LowerB++] = true;

            }
            else if (LowerB == UpperB)
            {
                // mark as deleted lines.
                while (LowerA < UpperA)
                    DataA.modified[LowerA++] = true;

            }
            else
            {
                // Find the middle snakea and length of an optimal path for A and B
                int smsrdX, smsrdY;
                ShortestMiddleSnake(DataA, LowerA, UpperA, DataB, LowerB, UpperB, DownVector, UpVector, out smsrdX, out smsrdY);

                // The path is from LowerX to (x,y) and (x,y) to UpperX
                LongestCommonSubsequence(DataA, LowerA, smsrdX, DataB, LowerB, smsrdY, DownVector, UpVector);
                LongestCommonSubsequence(DataA, smsrdX, UpperA, DataB, smsrdY, UpperB, DownVector, UpVector);
            }
        }


        /// <summary>
        /// Scan the tables of which lines are inserted and deleted, producing an edit script in forward order.  
        /// </summary>
        private static Item[] CreateDiffs<T>(Data<T> DataA, Data<T> DataB)
            where T : IEquatable<T>
        {
            List<Item> items = new List<Item>();
            int StartA, StartB;
            int LineA, LineB;

            LineA = 0;
            LineB = 0;
            while (LineA < DataA.Length || LineB < DataB.Length)
            {
                if ((LineA < DataA.Length) && (!DataA.modified[LineA])
                  && (LineB < DataB.Length) && (!DataB.modified[LineB]))
                {
                    // equal lines
                    ++LineA;
                    ++LineB;
                }
                else
                {
                    // maybe deleted and/or inserted lines
                    StartA = LineA;
                    StartB = LineB;

                    while (LineA < DataA.Length && (LineB >= DataB.Length || DataA.modified[LineA]))
                        ++LineA;

                    while (LineB < DataB.Length && (LineA >= DataA.Length || DataB.modified[LineB]))
                        ++LineB;

                    if ((StartA < LineA) || (StartB < LineB))
                    {
                        // store a new difference-item
                        items.Add(new Item()
                        {
                            DeleteStart = StartA,
                            DeleteLength = LineA - StartA,
                            InsertStart = StartB,
                            InsertLength = LineB - StartB
                        });
                    }
                }
            }

            return items.ToArray();
        }
    }
}