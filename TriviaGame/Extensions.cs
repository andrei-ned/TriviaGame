using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TriviaGame
{
    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rnd = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                T x = list[i];
                list[i] = list[j];
                list[j] = x;
            }
        }
    }
}
