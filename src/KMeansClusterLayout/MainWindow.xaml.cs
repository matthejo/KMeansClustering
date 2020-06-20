using KMeansClustering;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            OpenFileDialog originalOpenFileDialog = new OpenFileDialog
            {
                Title = "Mosaic Image",
                Filter = "Images|*.jpg;*.png",
                InitialDirectory = Settings.Default.OriginalImagePath
            };

            if (originalOpenFileDialog.ShowDialog() == true)
            {
                var originalImage = BitmapFrame.Create(new Uri(originalOpenFileDialog.FileName), BitmapCreateOptions.None, BitmapCacheOption.Default);
                var targetImage = (await CreateSampledBitmap(originalImage.ToStandardRgbBitmap(), averageAspectRatio, sources.Count)).ToBitmapSource();
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
    }
}
