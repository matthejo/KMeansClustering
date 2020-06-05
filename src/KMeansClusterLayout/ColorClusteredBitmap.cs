using KMeansClustering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClusterLayout
{
    public class ColorClusteredBitmap : NotifyPropertyChanged
    {
        public string OriginalImagePath { get; }
        public string ConvertedImagePath { get; }
        public WeightedColorSet Histogram { get; }
        public IList<int> ColorWeights { get; }
        public IList<Color> Colors { get; }

        private WeakReference<BitmapSource> originalBitmap = new WeakReference<BitmapSource>(null);
        private WeakReference<BitmapSource> convertedBitmap = new WeakReference<BitmapSource>(null);

        public ColorClusteredBitmap(string originalImagePath, string convertedImagePath, WeightedColorSet histogram)
        {
            OriginalImagePath = originalImagePath;
            ConvertedImagePath = convertedImagePath;
            Histogram = histogram;
            Colors = histogram.Colors.Select(c => c.Color.ToWindowsColor()).ToArray();
            ColorWeights = histogram.Colors.Select(c => c.PixelCount).ToArray();
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
                weakReference.SetTarget(value);
                return value;
            }
        }

        private void SetImage(WeakReference<BitmapSource> weakReference, BitmapSource value, [CallerMemberName] string propertyName = null)
        {
            BitmapSourceStrongReferenceCache.RefreshReference(value);
            weakReference.SetTarget(value);
            OnPropertyChanged(propertyName);
        }

        public BitmapSource ConvertedImage
        {
            get
            {
                return GetOrLoadImage(convertedBitmap, () => BitmapFrame.Create(new Uri(ConvertedImagePath), BitmapCreateOptions.None, BitmapCacheOption.Default));
            }
            set
            {
                SetImage(convertedBitmap, value);
            }
        }

        public BitmapSource OriginalImage
        {
            get
            {
                return GetOrLoadImage(originalBitmap, () => BitmapFrame.Create(new Uri(OriginalImagePath), BitmapCreateOptions.None, BitmapCacheOption.Default));
            }
            set
            {
                SetImage(originalBitmap, value);
            }
        }

        public float DistanceTo(ColorClusteredBitmap other, bool usePermutations)
        {
            float distance = float.MaxValue;

            if (usePermutations)
            {
                foreach (var permutation in Permutation.GetPermutations(this.Histogram.Colors.Count))
                {
                    float newDistance = DistanceTo(other, permutation);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                    }
                }
            }
            else
            {
                distance = DistanceTo(other, Permutation.GetPermutations(this.Histogram.Colors.Count).First());
            }

            return distance;
        }

        private float DistanceTo(ColorClusteredBitmap other, IReadOnlyList<int> indexLookup)
        {
            float distance = 0;
            for (int i = 0; i < this.Histogram.Colors.Count; i++)
            {
                float thisWeight = (float)this.Histogram.Colors[i].PixelCount / this.Histogram.PixelCount;
                float otherWeight = (float)other.Histogram.Colors[indexLookup[i]].PixelCount / other.Histogram.PixelCount;

                float distanceSquared = Vector3.DistanceSquared((Vector3)this.Histogram.Colors[i].Color.ToCieLab(), (Vector3)other.Histogram.Colors[indexLookup[i]].Color.ToCieLab());

                distance += distanceSquared * (thisWeight + otherWeight) / 2;
            }

            return distance;
        }
    }
}
