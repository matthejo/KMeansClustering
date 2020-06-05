using KMeansClustering;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

        private void LoadBatchImagesDirectory(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog originalOpenFileDialog = new CommonOpenFileDialog
            {
                Title = "Original Images",
                IsFolderPicker = true,
                DefaultDirectory = Settings.Default.OriginalImagePath
            };

            if (originalOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Default.OriginalImagePath = originalOpenFileDialog.FileName;
                Settings.Default.Save();

                CommonOpenFileDialog convertedOpenFileDialog = new CommonOpenFileDialog
                {
                    Title = "Converted Images",
                    IsFolderPicker = true,
                    DefaultDirectory = Settings.Default.ConvertedImagePath
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

            await Task.Run(() =>
            {
                Parallel.For(0, joinedPaths.Length, i =>
                {
                    string histogramFile = Path.Combine(Path.GetDirectoryName(joinedPaths[i].Item2), "colorHistograms", $"{Path.GetFileNameWithoutExtension(joinedPaths[i].Item2)}.json");
                    if (File.Exists(histogramFile))
                    {
                        WeightedColorSet set = JsonConvert.DeserializeObject<WeightedColorSet>(File.ReadAllText(histogramFile));
                        bitmaps[i] = new ColorClusteredBitmap(joinedPaths[i].Item1, joinedPaths[i].Item2, set);
                    }

                    Interlocked.Increment(ref progress);
                });
            });

            FinishProgress(timer);

            foreach (ColorClusteredBitmap result in bitmaps.Where(b => b != null))
            {
                sources.Add(result);
            }
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

        private async void FindClosestImages(object sender, RoutedEventArgs e)
        {
            ColorClusteredBitmap first = (ColorClusteredBitmap)ImageList.SelectedItem;
            ClosestList.ItemsSource = null;

            if (first != null)
            {
                int progress = 0;
                var timer = InitializeProgress(sources.Count, () => progress);

                float[] distances = new float[sources.Count];
                bool usePermutations = UsePermutationForDistance.IsChecked == true;

                await Task.Run(() =>
                {
                    Parallel.For(0, sources.Count, i =>
                    {
                        distances[i] = first.DistanceTo(sources[i], usePermutations);
                        Interlocked.Increment(ref progress);
                    });
                });

                FinishProgress(timer);

                SortedList<Tuple<int, float>, ColorClusteredBitmap> distanceList = new SortedList<Tuple<int, float>, ColorClusteredBitmap>(new SortedTupleComparer());
                for (int i = 0; i < distances.Length; i++)
                {
                    distanceList.Add(Tuple.Create(i, distances[i]), sources[i]);
                }

                ClosestList.ItemsSource = distanceList.Values.Take(100).ToArray();
            }
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
