using KMeansClustering;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KMeansClusterLayout
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ImageList.ItemsSource = sources;
        }

        private readonly ObservableCollection<ColorClusteredBitmap> sources = new ObservableCollection<ColorClusteredBitmap>();
        private float averageAspectRatio = 1.0f;
        private int averageWidth = 0;
        private int averageHeight = 0;
        private StandardRgbBitmap sampledBitmap;

        private void LoadBatchImagesDirectory(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog originalOpenFileDialog = new CommonOpenFileDialog
            {
                Title = "Original Images",
                IsFolderPicker = true,
                InitialDirectory = Settings.Default.OriginalImagePath
            };

            if (originalOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Default.OriginalImagePath = originalOpenFileDialog.FileName;
                Settings.Default.Save();

                CommonOpenFileDialog convertedOpenFileDialog = new CommonOpenFileDialog
                {
                    Title = "Converted Images",
                    IsFolderPicker = true,
                    InitialDirectory = Settings.Default.ConvertedImagePath
                };

                if (convertedOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Settings.Default.ConvertedImagePath = convertedOpenFileDialog.FileName;
                    Settings.Default.Save();

                    sources.Clear();
                    PopulateImagesAsync(originalOpenFileDialog.FileName, convertedOpenFileDialog.FileName);
                }
            }
        }

        private string GetPrefix(string fileName)
        {
            string strippedName = Path.GetFileNameWithoutExtension(fileName);
            return strippedName.Substring(0, strippedName.LastIndexOf("_CIELAB"));
        }

        private async void PopulateImagesAsync(string originalImages, string computedImages)
        {
            string[] originalFilePaths = Directory.GetFiles(originalImages, "*.png").Concat(Directory.GetFiles(originalImages, "*.jpg")).ToArray();
            string[] computedFilesPaths = Directory.GetFiles(computedImages, "*.png");

            Tuple<string, string>[] joinedPaths = originalFilePaths.Join(computedFilesPaths, o => Path.GetFileNameWithoutExtension(o), i => GetPrefix(i), (o, i) => Tuple.Create(o, i)).ToArray();

            ColorClusteredBitmap[] bitmaps = new ColorClusteredBitmap[originalFilePaths.Length];

            int progress = 0;
            var timer = InitializeProgress(joinedPaths.Length, () => progress);

            long totalWidths = 0;
            long totalHeights = 0;

            await Task.Run(() =>
            {
                Parallel.For(0, joinedPaths.Length, i =>
                {
                    string histogramFile = Path.Combine(Path.GetDirectoryName(joinedPaths[i].Item2), "colorHistograms", $"{Path.GetFileNameWithoutExtension(joinedPaths[i].Item2)}.json");
                    if (File.Exists(histogramFile))
                    {
                        WeightedColorSet set = JsonConvert.DeserializeObject<WeightedColorSet>(File.ReadAllText(histogramFile));
                        bitmaps[i] = new ColorClusteredBitmap(joinedPaths[i].Item1, joinedPaths[i].Item2, set);

                        Interlocked.Add(ref totalWidths, bitmaps[i].Histogram.PixelWidth);
                        Interlocked.Add(ref totalHeights, bitmaps[i].Histogram.PixelHeight);
                    }

                    Interlocked.Increment(ref progress);
                });
            });

            FinishProgress(timer);

            foreach (ColorClusteredBitmap result in bitmaps.Where(b => b != null))
            {
                sources.Add(result);
            }

            averageWidth = (int)(totalWidths / sources.Count);
            averageHeight = (int)(totalHeights / sources.Count);
            averageAspectRatio = (float)totalWidths / totalHeights;
        }

        private DispatcherTimer InitializeProgress(int count, Func<int> progressEvaluator)
        {
            SharedProgress.Visibility = Visibility.Visible;
            SharedProgress.Maximum = count;
            SharedProgress.Value = 0;

            DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, (sender, e) =>
            {
                SharedProgress.Value = progressEvaluator();
            }, Dispatcher);

            return timer;
        }

        private void FinishProgress(DispatcherTimer timer)
        {
            timer.Stop();
            SharedProgress.Visibility = Visibility.Hidden;
        }

        private async void OpenMosaicImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog mosaicOpenFileDialog = new OpenFileDialog
            {
                Title = "Mosaic Image",
                Filter = "Images|*.jpg;*.png",
                InitialDirectory = Settings.Default.MosaicImagePath
            };

            if (mosaicOpenFileDialog.ShowDialog() == true)
            {
                Settings.Default.MosaicImagePath = Path.GetDirectoryName(mosaicOpenFileDialog.FileName);
                Settings.Default.Save();

                var originalImage = BitmapFrame.Create(new Uri(mosaicOpenFileDialog.FileName), BitmapCreateOptions.None, BitmapCacheOption.Default);
                sampledBitmap = await CreateSampledBitmap(originalImage.ToStandardRgbBitmap(), averageAspectRatio, sources.Count);
                var targetImage = sampledBitmap.ToBitmapSource();
                SimplifiedBitmap.Source = targetImage;
                SimplifiedBitmap.Width = targetImage.PixelWidth * 1.5;
                SimplifiedBitmap.Height = targetImage.PixelHeight;
            }
        }

        private Task<StandardRgbBitmap> CreateSampledBitmap(StandardRgbBitmap source, float volumeAspectRatio, int totalVolumeCount)
        {
            return Task.Run(() =>
            {
                // Determine the ideal width and height of each volume
                //      x * y = totalvolumeCount
                //      x * volumeAspectRatio / y = source.w / source.h
                //   => y = totalVolumeCount / x
                //   => x * x * volumeAspectRatio / totalVolumeCount = source.w / source.h
                //   => x ^ 2 = source.w / source.h * totalVolumeCount / volumeAspectRatio
                //   => y = sqrt(source.w / source.h * totalVolumeCount / volumeAspectRatio)

                double targetWidth = Math.Sqrt((double)source.Width / source.Height * totalVolumeCount / volumeAspectRatio);
                double targetHeight = totalVolumeCount / targetWidth;

                int width = (int)targetWidth;
                int height = (int)targetHeight;

                int areaWidth = source.Width / width;
                int areaHeight = source.Height / height;

                int startX = (source.Width - width * areaWidth) / 2;
                int startY = (source.Height - height * areaHeight) / 2;

                StandardRgbColor[] pixels = new StandardRgbColor[width * height];
                Parallel.For(0, width, targetX =>
                {
                    for (int targetY = 0; targetY < height; targetY++)
                    {
                        ColorAverageAccumulator accumulator = new ColorAverageAccumulator();
                        for (int sourceX = startX + targetX * areaWidth; sourceX < startX + (targetX + 1) * areaWidth; sourceX++)
                        {
                            for (int sourceY = startY + targetY * areaHeight; sourceY < startY + (targetY + 1) * areaHeight; sourceY++)
                            {
                                accumulator.AddSample((Vector3)source.Pixels[sourceY * source.Width + sourceX].ToCieLab());
                            }
                        }

                        pixels[targetY * width + targetX] = ((CieLabColor)accumulator.GetAverage()).ToStandardRgb();
                    }
                });

                return new StandardRgbBitmap(pixels, width, height, source.DpiX, source.DpiY);
            });
        }

        private class SortedTupleComparer : IComparer<Tuple<int, float>>
        {
            public int Compare(Tuple<int, float> x, Tuple<int, float> y)
            {
                int distanceComparison = x.Item2.CompareTo(y.Item2);
                if (distanceComparison != 0)
                {
                    return distanceComparison;
                }

                return x.Item1.CompareTo(y.Item1);
            }
        }

        private async void GenerateMosaic(object sender, RoutedEventArgs e)
        {
            var availableSources = sources.ToList();

            var sourcePixels = sampledBitmap.Pixels.Select(p => ColorSpaces.CieLab.ConvertFromStandardRgb(p)).ToArray();
            ColorClusteredBitmap[] targetPixelImages = new ColorClusteredBitmap[sourcePixels.Length];

            int progressIndex = 0;
            var timer = InitializeProgress(targetPixelImages.Length * 2, () => progressIndex);

            await Task.Run(() =>
            {
                for (; progressIndex < sampledBitmap.Pixels.Length; progressIndex++)
                {
                    targetPixelImages[progressIndex] = FindBestMatchAndRemove(sourcePixels[progressIndex], ColorSpaces.CieLab, availableSources);
                }
            });
            FinishProgress(timer);

            const double maxSize = 23000;
            double scaleXFit = Math.Min(1, maxSize / (sampledBitmap.Width * averageWidth));
            double scaleYFit = Math.Min(1, maxSize / (sampledBitmap.Height * averageHeight));
            double scaleFit = Math.Min(scaleXFit, scaleYFit);

            int cellWidth = (int)(averageWidth * scaleFit);
            int cellHeight = (int)(averageHeight * scaleFit);

            var output = await CreateOutputBitmap(targetPixelImages, sampledBitmap.Width, sampledBitmap.Height, (int)(averageWidth * scaleFit), (int)(averageHeight * scaleFit));

            SimplifiedBitmap.Source = output;
            SimplifiedBitmap.Width = output.PixelWidth;
            SimplifiedBitmap.Height = output.PixelHeight;
        }

        private async Task<WriteableBitmap> CreateOutputBitmap(ColorClusteredBitmap[] inputBitmaps, int cellXCount, int cellYCount, int cellPixelWidth, int cellPixelHeight)
        {
            long totalWidth = cellXCount * cellPixelWidth;
            long totalHeight = cellYCount * cellPixelHeight;

            WriteableBitmap writeableBitmap = new WriteableBitmap(cellXCount * cellPixelWidth, cellYCount * cellPixelHeight, 96, 96, PixelFormats.Bgra32, null);
            ConcurrentQueue<WriteBitmapOperation> writeableBitmapUpdates = new ConcurrentQueue<WriteBitmapOperation>();

            int progressIndex = cellXCount * cellYCount;
            var progressTimer = InitializeProgress(progressIndex * 2, () =>
            {
                while (writeableBitmapUpdates.TryDequeue(out var operation))
                {
                    writeableBitmap.WritePixels(new Int32Rect(0, 0, cellPixelWidth, cellPixelHeight), operation.Pixels, operation.Stride, cellPixelWidth * operation.X, cellPixelHeight * operation.Y);
                }

                return progressIndex;
            });

            await Task.Run(() =>
            {
                Parallel.For(0, cellYCount, y =>
                {
                    for (int x = 0; x < cellXCount; x++)
                    {
                        var source = inputBitmaps[y * cellXCount + x];
                        var sourceConverted = ConvertToTargetSize(source.OriginalImage, cellPixelWidth, cellPixelHeight).ToStandardRgbBitmap();
                        var sourceConvertedPixels = sourceConverted.ToBgra32PixelArray(out int stride);
                        writeableBitmapUpdates.Enqueue(new WriteBitmapOperation(sourceConvertedPixels, stride, x, y));
                        Interlocked.Increment(ref progressIndex);
                    }
                });
            });

            FinishProgress(progressTimer);

            return writeableBitmap;
        }

        private static ColorClusteredBitmap FindBestMatchAndRemove(Vector3 pixel, IColorSpace colorSpace, List<ColorClusteredBitmap> availableSources)
        {
            float bestDistance = float.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < availableSources.Count; i++)
            {
                var distanceSquared = FindDistanceSquared(pixel, availableSources[i].Histogram, colorSpace);
                if (distanceSquared < bestDistance)
                {
                    bestDistance = distanceSquared;
                    bestIndex = i;
                }
            }

            var result = availableSources[bestIndex];
            availableSources.RemoveAt(bestIndex);
            return result;
        }

        private static float FindDistanceSquared(Vector3 pixel, WeightedColorSet colorSet, IColorSpace colorSpace)
        {
            float distanceSquared = 0.0f;
            foreach (var weightedColor in colorSet.Colors)
            {
                distanceSquared += Vector3.DistanceSquared(pixel, colorSpace.ConvertFromStandardRgb(weightedColor.Color)) * weightedColor.PixelCount / colorSet.PixelCount;
            }

            return distanceSquared;
        }

        private static BitmapSource ConvertToTargetSize(BitmapSource source, int targetWidth, int targetHeight)
        {
            // First scale the bitmap to match either the height or the width, then crop the bitmap to center it
            double scale = Math.Max((double)targetHeight / source.PixelHeight, (double)targetWidth / source.PixelWidth);
            TransformedBitmap scaledBitmap = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            CroppedBitmap croppedBitmap = new CroppedBitmap(scaledBitmap, new Int32Rect((scaledBitmap.PixelWidth - targetWidth) / 2, (scaledBitmap.PixelHeight - targetHeight) / 2, targetWidth, targetHeight));
            return croppedBitmap;
        }

        private struct WriteBitmapOperation
        {
            public byte[] Pixels { get; }

            public int Stride { get; }

            public int X { get; }

            public int Y { get; }

            public WriteBitmapOperation(byte[] pixels, int stride, int x, int y)
            {
                Pixels = pixels;
                Stride = stride;
                X = x;
                Y = y;
            }
        }
    }
}
