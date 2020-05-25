using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class BitmapCluster
    {
        private const double Epsilon = 0.00001;

        private readonly StandardRgbPixelData[] pixels;
        private readonly IPixelRepresentation pixelRepresentation;
        private readonly Vector3[] pixelData;

        private readonly int[] pixelClusters;
        private Vector3[] clusterMeans;
        private readonly int[] clusterWeights;

        public int[] ClusterWeights => clusterWeights;
        public StandardRgbPixelData[] ClusterMeans => clusterMeans.Select(p => pixelRepresentation.ConvertToStandardRgb(p)).ToArray();

        public BitmapCluster(StandardRgbPixelData[] pixels, IPixelRepresentation pixelRepresentation, Vector3[] initialClusterSeeds)
        {
            this.pixels = pixels;
            this.pixelRepresentation = pixelRepresentation;
            this.pixelClusters = new int[pixels.Length];
            this.pixelData = ConvertToPixelData(pixels, pixelRepresentation);
            this.clusterMeans = initialClusterSeeds;
            if (this.clusterMeans != null)
            {
                this.clusterWeights = new int[clusterMeans.Length];
            }
        }

        public BitmapCluster(StandardRgbPixelData[] pixels, IPixelRepresentation pixelRepresentation, int clusterCount)
            : this(pixels, pixelRepresentation, null)
        {
            this.clusterWeights = new int[clusterCount];
        }

        public StandardRgbPixelData[] Render()
        {
            return pixelClusters.Select(i => pixelRepresentation.ConvertToStandardRgb(clusterMeans[i])).ToArray();
        }

        private static Vector3[] ConvertToPixelData(StandardRgbPixelData[] pixels, IPixelRepresentation pixelRepresentation)
        {
            return pixels.Select(p => pixelRepresentation.ConvertFromStandardRgb(p)).ToArray();
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

                if (clusterMeans == null)
                {
                    clusterMeans = CreateRandomSeeding(pixelData, pixelRepresentation, clusterWeights.Length);
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
            PixelDataMeanAccumulator[] accumulatedSamples = new PixelDataMeanAccumulator[clusterMeans.Length];
            for (int i = 0; i < clusterWeights.Length; i++)
            {
                clusterWeights[i] = 0;
            }

            for (int pixelIndex = 0; pixelIndex < pixelData.Length; pixelIndex++)
            {
                int bestCluster = -1;
                double bestDistance = double.MaxValue;

                for (int clusterIndex = 0; clusterIndex < clusterMeans.Length; clusterIndex++)
                {
                    double distance = Vector3.DistanceSquared(clusterMeans[clusterIndex], pixelData[pixelIndex]);
                    if (distance < bestDistance)
                    {
                        bestCluster = clusterIndex;
                        bestDistance = distance;
                    }
                }

                clusterAssigments[pixelIndex] = bestCluster;
                clusterWeights[bestCluster]++;
                accumulatedSamples[bestCluster].AddSample(pixelData[pixelIndex]);
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
            for (int pixelIndex = 0; pixelIndex < pixelData.Length; pixelIndex++)
            {
                pixelData[pixelIndex] = clusterMeans[clusterAssigments[pixelIndex]];
            }
        }

        private static bool AreNearlyEqual(Vector3 a, Vector3 b)
        {
            Vector3 delta = Vector3.Abs(a - b);
            return delta.X < Epsilon &&
                delta.Y < Epsilon &&
                delta.Z < Epsilon;
        }

        private static Vector3[] CreateRandomSeeding(Vector3[] pixelData, IPixelRepresentation pixelRepresentation, int clusterCount)
        {
            Random random = new Random();
            Vector3[] clusterMeans = new Vector3[clusterCount];
            // Randomly choose a first cluster point
            clusterMeans[0] = pixelData[random.Next(pixelData.Length)];

            double[] weightedProbabilities = new double[pixelData.Length];

            for (int clusterIndex = 1; clusterIndex < clusterMeans.Length; clusterIndex++)
            {
                // Choose a cluster point based on k-means++, where the weighted probability of the point being chosen is associated with its
                // squared distance from the nearest existing point.

                double totalDistances = 0;
                for (int pixelIndex = 0; pixelIndex < pixelData.Length; pixelIndex++)
                {
                    double closestDistance = double.MaxValue;
                    for (int previousClusterIndex = 0; previousClusterIndex < clusterIndex; previousClusterIndex++)
                    {
                        closestDistance = Math.Min(closestDistance, Vector3.DistanceSquared(clusterMeans[previousClusterIndex], pixelData[pixelIndex]));
                    }
                    weightedProbabilities[pixelIndex] = closestDistance;
                    totalDistances += closestDistance;
                }

                double weightedRandom = random.NextDouble() * totalDistances;
                for (int pixelIndex = 0; pixelIndex < pixelData.Length; pixelIndex++)
                {
                    weightedRandom -= weightedProbabilities[pixelIndex];
                    if (weightedRandom <= 0 || pixelIndex == pixelData.Length - 1)
                    {
                        clusterMeans[clusterIndex] = pixelData[pixelIndex];
                        break;
                    }
                }
            }

            return clusterMeans;
        }

        private struct PixelDataMeanAccumulator
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
