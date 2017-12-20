﻿
using System;
using System.Text;
using System.Threading;

namespace BusinessFunction
{
    public class cpu
    {
        private int timetorun;
        public cpu(string timetorun)
        {
            this.timetorun = Convert.ToInt16(timetorun);
        }
        public  void Fun(object state)
        {
            DateTime currentTime = new DateTime();
            currentTime = System.DateTime.Now;
            DateTime finishTime = currentTime.AddSeconds(timetorun);
            Console.WriteLine( "CPU Start." + Convert.ToString(currentTime));
            int i = 0;
            while (System.DateTime.Now.CompareTo(finishTime) < 0)
            {
                string comparestring1 = StringDistance.GenerateRandomString(1000);
                i++;
                if (i == 50)
                {
                    Thread.Sleep(10); // Change the wait time here.
                    i = 0;
                }
                //  string comparestring2 = StringDistance.GenerateRandomString(1000);
                // StringDistance.LevenshteinDistance(comparestring1, comparestring2);
            }
            Console.WriteLine("CPU Done." + Convert.ToString(System.DateTime.Now));
        }
    }
    internal class StringDistance
    {
        #region Public Methods

        public static string GenerateRandomString(int length)
        {
            var r = new Random((int)DateTime.Now.Ticks);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int c = r.Next(97, 123);
                sb.Append(Char.ConvertFromUtf32(c));
            }
            return sb.ToString();
        }
        public static int LevenshteinDistance(string str1, string str2)
        {
            var scratchDistanceMatrix = new int[str1.Length + 1, str2.Length + 1];
            // distance matrix contains one extra row and column for the seed values         
            for (int i = 0; i <= str1.Length; i++)
            {
                scratchDistanceMatrix[i, 0] = i;
            }
            for (int j = 0; j <= str2.Length; j++)
            {
                scratchDistanceMatrix[0, j] = j;
            }
            for (int i = 1; i <= str1.Length; i++)
            {
                int str1Index = i - 1;
                for (int j = 1; j <= str2.Length; j++)
                {
                    int str2Index = j - 1;
                    int cost = (str1[str1Index] == str2[str2Index]) ? 0 : 1;
                    int deletion = (i == 0) ? 1 : scratchDistanceMatrix[i - 1, j] + 1;
                    int insertion = (j == 0) ? 1 : scratchDistanceMatrix[i, j - 1] + 1;
                    int substitution = (i == 0 || j == 0) ? cost : scratchDistanceMatrix[i - 1, j - 1] + cost;
                    scratchDistanceMatrix[i, j] = Math.Min(Math.Min(deletion, insertion), substitution);
                    // Check for Transposition  
                    if (i > 1 && j > 1 && (str1[str1Index] == str2[str2Index - 1]) &&
                        (str1[str1Index - 1] == str2[str2Index]))
                    {
                        scratchDistanceMatrix[i, j] = Math.Min(
                            scratchDistanceMatrix[i, j], scratchDistanceMatrix[i - 2, j - 2] + cost);
                    }
                }
            }
            // Levenshtein distance is the bottom right element       
            return scratchDistanceMatrix[str1.Length, str2.Length];
        }
        #endregion
    }
}
