using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    internal sealed class BitmapBatchClusterOperation : NotifyPropertyChanged
    {
        private readonly WeakReference<BitmapSource> originalImage = new WeakReference<BitmapSource>(null);
        private readonly WeakReference<BitmapSource> computedImage = new WeakReference<BitmapSource>(null);
        private IList<int> colorWeights;
        private IList<Color> colors;
        private bool isRunning;
        private bool isComplete;

        public string OriginalFilePath { get; }

        public string OriginalFileName { get; }

        public IColorSpace ColorSpace { get; }

        public IList<int> ColorWeights
        {
            get { return colorWeights; }
            set { SetProperty(ref colorWeights, value); }
        }

        public IList<Color> Colors
        {
            get { return colors; }
            set { SetProperty(ref colors, value); }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set { SetProperty(ref isRunning, value); }
        }

        public bool IsComplete
        {
            get { return isComplete; }
            set { SetProperty(ref isComplete, value); }
        }

        private BitmapSource GetOrLoadImage(WeakReference<BitmapSource> weakReference, Func<BitmapSource> reload)
        {
            if (weakReference.TryGetTarget(out BitmapSource strongReference))
            {
                return strongReference;
            }
            else
            {
                var value = reload();
                BitmapSourceStrongReferenceCache.RefreshReference(value);
                originalImage.SetTarget(value);
                return value;
            }
        }

        private void SetImage(WeakReference<BitmapSource> weakReference, BitmapSource value, [CallerMemberName] string propertyName = null)
        {
            BitmapSourceStrongReferenceCache.RefreshReference(value);
            weakReference.SetTarget(value);
            OnPropertyChanged(propertyName);
        }

        public BitmapSource OriginalImage
        {
            get
            {
                return GetOrLoadImage(originalImage, () => BitmapFrame.Create(new Uri(OriginalFilePath), BitmapCreateOptions.None, BitmapCacheOption.Default));
            }
            set
            {
                SetImage(originalImage, value);
            }
        }

        public BitmapSource ComputedImage
        {
            get
            {
                return GetOrLoadImage(computedImage, () => null);
            }
            set
            {
                SetImage(computedImage, value);
            }
        }

        public BitmapBatchClusterOperation(string originalFilePath)
        {
            OriginalFilePath = originalFilePath;
            OriginalFileName = Path.GetFileName(originalFilePath);
        }

        public async Task RunAsync(IColorSpace colorSpace, int clusters, string outputDirectory, bool saveColorHistogram)
        {
            IsComplete = false;
            IsRunning = true;
            var clusterOperation = new BitmapClusterOperation("batch", colorSpace, "_converted");
            await clusterOperation.RunAsync(OriginalImage.ToStandardRgbBitmap(), clusters, Path.GetFileNameWithoutExtension(OriginalFilePath), false);

            clusterOperation.Bitmap.Save(GetOutputFileName(OriginalFilePath, outputDirectory, colorSpace, clusters, ".png"));

            if (saveColorHistogram)
            {
                string histogramOutputDirectory = Path.Combine(outputDirectory, "colorHistograms");
                Directory.CreateDirectory(histogramOutputDirectory);
                SaveWeightedColorsToJson(clusterOperation, histogramOutputDirectory, colorSpace, clusters);
            }

            ComputedImage = clusterOperation.Bitmap;
            ColorWeights = clusterOperation.ColorWeights;
            Colors = clusterOperation.Colors;
            IsRunning = false;
            IsComplete = true;
        }

        private void SaveWeightedColorsToJson(BitmapClusterOperation clusterOperation, string outputDirectory, IColorSpace colorSpace, int clusters)
        {
            List<WeightedColor> colors = clusterOperation.ColorWeights.Zip(clusterOperation.Colors, (w, c) => new WeightedColor(w, c.ToStandardRgbColor())).OrderByDescending(wc => wc.PixelCount).ToList();
            WeightedColorSet set = new WeightedColorSet(colors.Sum(c => c.PixelCount), colors);
            File.WriteAllText(GetOutputFileName(OriginalFilePath, outputDirectory, colorSpace, clusters, ".json"), JsonConvert.SerializeObject(set));
        }

        private string GetOutputFileName(string inputFilePath, string outputDirectory, IColorSpace colorSpace, int clusterCount, string fileExtension)
        {
            return Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{colorSpace.Name}@{clusterCount}{fileExtension}");
        }
    }
}
