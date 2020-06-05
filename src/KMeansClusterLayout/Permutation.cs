using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClusterLayout
{
    public static class Permutation
    {
        private static readonly ConcurrentDictionary<int, List<int[]>> permutationMap = new ConcurrentDictionary<int, List<int[]>>();

        public static IEnumerable<IReadOnlyList<int>> GetPermutations(int count)
        {
            return permutationMap.GetOrAdd(count, c =>
            {
                var permutations = new List<int[]>(count);
                var original = CreateIndexList(count);

                foreach (var permutation in Permutate(original, count))
                {
                    permutations.Add(permutation.ToArray());
                }

                return permutations;
            });
        }

        private static List<int> CreateIndexList(int count)
        {
            List<int> result = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(i);
            }
            return result;
        }

        private static void RotateRight<T>(IList<T> sequence, int count)
        {
            T tmp = sequence[count - 1];
            sequence.RemoveAt(count - 1);
            sequence.Insert(0, tmp);
        }

        private static IEnumerable<IList<T>> Permutate<T>(IList<T> sequence, int count)
        {
            if (count == 1) yield return sequence;
            else
            {
                for (int i = 0; i < count; i++)
                {
                    foreach (var perm in Permutate(sequence, count - 1))
                        yield return perm;
                    RotateRight(sequence, count);
                }
            }
        }
    }
}
