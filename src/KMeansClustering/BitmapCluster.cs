using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class BitmapCluster
    {
        private const double Epsilon = 0.00001;

        private readonly IColorSpace colorSpace;

        private readonly StandardRgbColor[] originalPixels;
        private readonly int[] pixelClusters;
        private readonly int[] clusterWeights;
        private Vector3[] clusterMeans;
        private Vector3[] pixels;

        public int[] ClusterWeights => clusterWeights;
        public StandardRgbColor[] ClusterMeans => clusterMeans.Select(p => colorSpace.ConvertToStandardRgb(p)).ToArray();

        public BitmapCluster(StandardRgbColor[] pixels, IColorSpace colorSpace, Vector3[] initialClusterSeeds)
        {
            this.originalPixels = pixels;
            this.colorSpace = colorSpace;
            this.pixelClusters = new int[pixels.Length];
            this.clusterMeans = initialClusterSeeds;
            if (this.clusterMeans != null)
            {
                this.clusterWeights = new int[clusterMeans.Length];
            }
        }

        public BitmapCluster(StandardRgbColor[] pixels, IColorSpace colorSpace, int clusterCount)
            : this(pixels, colorSpace, null)
        {
            this.clusterWeights = new int[clusterCount];
        }

        public StandardRgbColor[] Render()
        {
            return pixelClusters.Select(i => colorSpace.ConvertToStandardRgb(clusterMeans[i])).ToArray();
        }

        private static Vector3[] ConvertToColorSpace(StandardRgbColor[] pixels, IColorSpace colorSpace)
        {
            return pixels.Select(p => colorSpace.ConvertFromStandardRgb(p)).ToArray();
        }

        public Task<Vector3[]> ChooseDifferentiatedClusters(int subsetCount)
        {
            return Task.Run(() =>
            {
                Vector3[] differentiatedClusters = new Vector3[subsetCount];
                differentiatedClusters[0] = this.clusterMeans[this.clusterWeights.Select((w, i) => new { Index = i, Weight = w }).OrderByDescending(t => t.Weight).Select(t => t.Index).First()];

                for (int differentClusterIndex = 1; differentClusterIndex < differentiatedClusters.Length; differentClusterIndex++)
                {
                    double highestDistance = 0;
                    int bestCluster = -1;

                    for (int clusterIndex = 0; clusterIndex < this.clusterMeans.Length; clusterIndex++)
                    {
                        double minDistance = double.MaxValue;
                        for (int previousDifferentClusterIndex = 0; previousDifferentClusterIndex < differentClusterIndex; previousDifferentClusterIndex++)
                        {
                            minDistance = Math.Min(minDistance, Vector3.DistanceSquared(clusterMeans[clusterIndex], differentiatedClusters[previousDifferentClusterIndex]));
                        }

                        if (minDistance > highestDistance)
                        {
                            bestCluster = clusterIndex;
                            highestDistance = minDistance;
                        }
                    }

                    differentiatedClusters[differentClusterIndex] = clusterMeans[bestCluster];
                }

                return differentiatedClusters;
            });
        }

        public Task<int> ClusterAsync(int maxIterations = 50)
        {
            return Task.Run(() =>
            {
                int iterationCount = 0;

                this.pixels = ConvertToColorSpace(this.originalPixels, colorSpace);

                if (clusterMeans == null)
                {
                    clusterMeans = CreateRandomSeeding(pixels, clusterWeights.Length);
                }

                while (!IterateNextCluster(clusterMeans, pixelClusters))
                {
                    iterationCount++;
                    if (iterationCount >= maxIterations)
                    {
                        break;
                    }
                }
                AssignPixelsFromClusters(clusterMeans, pixelClusters);

                return iterationCount;
            });
        }

        private bool IterateNextCluster(Vector3[] clusterMeans, int[] clusterAssigments)
        {
            ColorAverageAccumulator[] accumulatedSamples = new ColorAverageAccumulator[clusterMeans.Length];
            for (int i = 0; i < clusterWeights.Length; i++)
            {
                clusterWeights[i] = 0;
            }

            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
            {
                int bestCluster = -1;
                double bestDistance = double.MaxValue;

                for (int clusterIndex = 0; clusterIndex < clusterMeans.Length; clusterIndex++)
                {
                    double distance = Vector3.DistanceSquared(clusterMeans[clusterIndex], pixels[pixelIndex]);
                    if (distance < bestDistance)
                    {
                        bestCluster = clusterIndex;
                        bestDistance = distance;
                    }
                }

                clusterAssigments[pixelIndex] = bestCluster;
                clusterWeights[bestCluster]++;
                accumulatedSamples[bestCluster].AddSample(pixels[pixelIndex]);
            }

            bool isComplete = true;
            for (int i = 0; i < clusterMeans.Length; i++)
            {
                Vector3 newValue = accumulatedSamples[i].GetAverage();
                isComplete = isComplete && AreNearlyEqual(newValue, clusterMeans[i]);
                clusterMeans[i] = newValue;
            }

            return isComplete;
        }

        private void AssignPixelsFromClusters(Vector3[] clusterMeans, int[] clusterAssigments)
        {
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
            {
                pixels[pixelIndex] = clusterMeans[clusterAssigments[pixelIndex]];
            }
        }

        private static bool AreNearlyEqual(Vector3 a, Vector3 b)
        {
            Vector3 delta = Vector3.Abs(a - b);
            return delta.X < Epsilon &&
                delta.Y < Epsilon &&
                delta.Z < Epsilon;
        }

        private static Vector3[] CreateRandomSeeding(Vector3[] pixels, int clusterCount)
        {
            Random random = new Random();
            Vector3[] clusterMeans = new Vector3[clusterCount];
            // Randomly choose a first cluster point
            clusterMeans[0] = pixels[random.Next(pixels.Length)];

            double[] weightedProbabilities = new double[pixels.Length];

            for (int clusterIndex = 1; clusterIndex < clusterMeans.Length; clusterIndex++)
            {
                // Choose a cluster point based on k-means++, where the weighted probability of the point being chosen is associated with its
                // squared distance from the nearest existing point.

                double totalDistances = 0;
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    double closestDistance = double.MaxValue;
                    for (int previousClusterIndex = 0; previousClusterIndex < clusterIndex; previousClusterIndex++)
                    {
                        closestDistance = Math.Min(closestDistance, Vector3.DistanceSquared(clusterMeans[previousClusterIndex], pixels[pixelIndex]));
                    }
                    weightedProbabilities[pixelIndex] = closestDistance;
                    totalDistances += closestDistance;
                }

                double weightedRandom = random.NextDouble() * totalDistances;
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    weightedRandom -= weightedProbabilities[pixelIndex];
                    if (weightedRandom <= 0 || pixelIndex == pixels.Length - 1)
                    {
                        clusterMeans[clusterIndex] = pixels[pixelIndex];
                        break;
                    }
                }
            }

            return clusterMeans;
        }

        private struct ColorAverageAccumulator
        {
            private Vector3 Total;
            private int Count;

            public void AddSample(Vector3 color)
            {
                Total += color;
                Count++;
            }

            public Vector3 GetAverage()
            {
                return Total / Count;
            }
        }
    }
}
