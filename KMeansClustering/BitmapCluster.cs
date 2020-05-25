using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    internal sealed class BitmapCluster<TPixelRepresentation, TPixelData>
        where TPixelData : struct
        where TPixelRepresentation : IPixelRepresentation<TPixelData>
    {
        private readonly StandardRgbPixelData[] pixels;
        private readonly TPixelRepresentation pixelRepresentation;
        private readonly TPixelData[] pixelData;

        private readonly int[] pixelClusters;
        private TPixelData[] clusterMeans;
        private readonly int[] clusterWeights;

        public BitmapCluster(StandardRgbPixelData[] pixels, TPixelRepresentation pixelRepresentation, TPixelData[] initialClusterSeeds)
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

        public BitmapCluster(StandardRgbPixelData[] pixels, TPixelRepresentation pixelRepresentation, int clusterCount)
            : this(pixels, pixelRepresentation, null)
        {
            this.clusterWeights = new int[clusterCount];
        }

        public StandardRgbPixelData[] Render()
        {
            return pixelClusters.Select(i => pixelRepresentation.ConvertToStandardRgb(clusterMeans[i])).ToArray();
        }

        private static TPixelData[] ConvertToPixelData(StandardRgbPixelData[] pixels, TPixelRepresentation pixelRepresentation)
        {
            return pixels.Select(p => pixelRepresentation.ConvertFromStandardRgb(p)).ToArray();
        }

        public Task<TPixelData[]> PickDifferentiatedClusters(int subsetCount)
        {
            return Task.Run(() =>
            {
                TPixelData[] differentiatedClusters = new TPixelData[subsetCount];
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
                            minDistance = Math.Min(minDistance, pixelRepresentation.DistanceSquared(clusterMeans[clusterIndex], differentiatedClusters[previousDifferentClusterIndex]));
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

        private bool IterateNextCluster(TPixelData[] clusterMeans, int[] clusterAssigments)
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
                    double distance = pixelRepresentation.DistanceSquared(clusterMeans[clusterIndex], pixelData[pixelIndex]);
                    if (distance < bestDistance)
                    {
                        bestCluster = clusterIndex;
                        bestDistance = distance;
                    }
                }

                clusterAssigments[pixelIndex] = bestCluster;
                clusterWeights[bestCluster]++;
                pixelRepresentation.AddSample(ref accumulatedSamples[bestCluster], pixelData[pixelIndex]);
            }

            bool isComplete = true;
            for (int i = 0; i < clusterMeans.Length; i++)
            {
                TPixelData newValue = pixelRepresentation.GetAverage(accumulatedSamples[i]);
                isComplete = isComplete && pixelRepresentation.Equals(newValue, clusterMeans[i]);
                clusterMeans[i] = newValue;
            }

            return isComplete;
        }

        private void AssignPixelsFromClusters(TPixelData[] clusterMeans, int[] clusterAssigments)
        {
            for (int pixelIndex = 0; pixelIndex < pixelData.Length; pixelIndex++)
            {
                pixelData[pixelIndex] = clusterMeans[clusterAssigments[pixelIndex]];
            }
        }

        private static TPixelData[] CreateRandomSeeding(TPixelData[] pixelData, TPixelRepresentation pixelRepresentation, int clusterCount)
        {
            Random random = new Random();
            TPixelData[] clusterMeans = new TPixelData[clusterCount];
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
                        closestDistance = Math.Min(closestDistance, pixelRepresentation.DistanceSquared(clusterMeans[previousClusterIndex], pixelData[pixelIndex]));
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
    }
}
