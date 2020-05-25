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
        private readonly TPixelData[] clusterMeans;

        public BitmapCluster(StandardRgbPixelData[] pixels, TPixelRepresentation pixelRepresentation, int clusterCount)
        {
            this.pixels = pixels;
            this.pixelRepresentation = pixelRepresentation;
            this.pixelClusters = new int[pixels.Length];
            this.pixelData = ConvertToPixelData(pixels, pixelRepresentation);
            this.clusterMeans = new TPixelData[clusterCount];
        }

        public StandardRgbPixelData[] Render()
        {
            return pixelClusters.Select(i => pixelRepresentation.ConvertToStandardRgb(clusterMeans[i])).ToArray();
        }

        private static TPixelData[] ConvertToPixelData(StandardRgbPixelData[] pixels, TPixelRepresentation pixelRepresentation)
        {
            return pixels.Select(p => pixelRepresentation.ConvertFromStandardRgb(p)).ToArray();
        }

        public Task<int> ClusterAsync(int maxIterations = 200)
        {
            return Task.Run(() =>
            {
                Random random = new Random();

                InitializeClusterSeeds(clusterMeans, random);
                int iterationCount = 0;
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

        private void InitializeClusterSeeds(TPixelData[] clusterMeans, Random random)
        {
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
        }
    }
}
