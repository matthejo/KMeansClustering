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

        public BitmapBatchClusterOperation(string originalFilePath, string targetFilePath, IColorSpace colorSpace)
        {
            OriginalFilePath = originalFilePath;
            OriginalFileName = Path.GetFileName(originalFilePath);
            ColorSpace = colorSpace;
        }

        public async Task RunAsync(int clusters)
        {
            IsComplete = false;
            IsRunning = true;
            var clusterOperation = new BitmapClusterOperation("batch", ColorSpace, "_converted");
            await clusterOperation.RunAsync(OriginalImage.ToStandardRgbBitmap(), clusters, Path.GetFileNameWithoutExtension(OriginalFilePath), false);
            ComputedImage = clusterOperation.Bitmap;
            ColorWeights = clusterOperation.ColorWeights;
            Colors = clusterOperation.Colors;
            IsRunning = false;
            IsComplete = true;
        }
    }
}
