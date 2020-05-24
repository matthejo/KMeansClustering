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
    internal enum PixelRepresentation
    {
        RGB,
        HSL
    }

    internal sealed class MemoryBitmap<TPixelRepresentation, TPixelData>
        where TPixelData: struct
        where TPixelRepresentation : IPixelRepresentation<TPixelData>
    {
        private readonly BitmapSource source;
        private readonly TPixelRepresentation pixelRepresentation;
        private readonly TPixelData[] pixelData;

        public MemoryBitmap(BitmapSource source, TPixelRepresentation pixelRepresentation)
        {
            this.source = source;
            this.pixelRepresentation = pixelRepresentation;
            this.pixelData = ConvertToPixelData(source, pixelRepresentation);
        }

        public BitmapSource Render()
        {
            int stride = source.PixelWidth * sizeof(int);

            byte[] rawPixels = new byte[stride * source.PixelHeight];
            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                pixelRepresentation.FromPixelData(pixelData, rawPixels, i);
            }

            return BitmapSource.Create(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgra32, null, rawPixels, stride);
        }

        private static TPixelData[] ConvertToPixelData(BitmapSource source, TPixelRepresentation pixelRepresentation)
        {
            BitmapSource convertedSource = source;
            if (source.Format != PixelFormats.Bgra32)
            {
                convertedSource = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 1.0);
            }

            int stride = convertedSource.PixelWidth * sizeof(int);
            byte[] rawPixels = new byte[stride * convertedSource.PixelHeight];
            convertedSource.CopyPixels(rawPixels, stride, offset: 0);

            TPixelData[] pixelData = new TPixelData[convertedSource.PixelWidth * convertedSource.PixelHeight];

            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                pixelRepresentation.ToPixelData(rawPixels, pixelData, i);
            }

            return pixelData;
        }

        public Task<int> ClusterAsync(int clusterCount, int maxIterations = 50)
        {
            return Task.Run(() =>
            {
                Random random = new Random();
                TPixelData[] clusterMeans = new TPixelData[clusterCount];
                int[] clusterAssignments = new int[source.PixelWidth * source.PixelHeight];

                InitializeClusterSeeds(clusterMeans, random);
                int iterationCount = 0;
                while (!IterateNextCluster(clusterMeans, clusterAssignments))
                {
                    iterationCount++;
                    if (iterationCount >= maxIterations)
                    {
                        break;
                    }
                }
                AssignPixelsFromClusters(clusterMeans, clusterAssignments);

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
