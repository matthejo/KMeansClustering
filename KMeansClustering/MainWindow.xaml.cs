using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KMeansClustering
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private BitmapSource sourceImage;
        private DateTime computeStarted;

        private void LoadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                sourceImage = BitmapFrame.Create(new Uri(dialog.FileName), BitmapCreateOptions.None, BitmapCacheOption.Default);
                OriginalImage.Source = sourceImage;
                ComputeOptions.IsEnabled = true;
            }
        }

        private async void Compute(object sender, RoutedEventArgs e)
        {

            int clusters;
            if (!int.TryParse(ClusterCount.Text, out clusters))
            {
                MessageBox.Show("Could not parse the cluster count");
                return;
            }

            this.IsEnabled = false;
            computeStarted = DateTime.Now;

            StandardRgbBitmap sourceBitmap = sourceImage.ToStandardRgbBitmap();

            foreach (Image image in RGBImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }
            foreach (Image image in CIELUVImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }
            foreach (Image image in CIELABImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }
            RGBColorSlices.Children.Clear();
            CIELUVColorSlices.Children.Clear();
            CIELabColorSlices.Children.Clear();

            await Task.WhenAll(
                UpdateGroup<StandardRgbPixelRepresentation, StandardRgbPixelData>(clusters, sourceBitmap, PixelRepresentations.Rgb, RGBImageGrid, RGBColorSlices, RGBStatus),
                UpdateGroup<CieLuvPixelRepresentation, CieLuvPixelData>(clusters, sourceBitmap, PixelRepresentations.CieLuv, CIELUVImageGrid, CIELUVColorSlices, CIELuvStatus),
                UpdateGroup<CieLabPixelRepresentation, CieLabPixelData>(clusters, sourceBitmap, PixelRepresentations.CieLab, CIELABImageGrid, CIELabColorSlices, CIELabStatus)
                );

            this.IsEnabled = true;
        }

        private async Task UpdateGroup<TPixelRepresentation, TPixelData>(int clusters, StandardRgbBitmap sourceBitmap, TPixelRepresentation pixelRepresentation, Panel parent, Grid colorSlices, Label label)
            where TPixelData : struct
            where TPixelRepresentation : IPixelRepresentation<TPixelData>
        {
            EventHandler onTick = (sender, e) =>
            {
                label.Content = $"Computing clusters... [{DateTime.Now - computeStarted:mm\\:ss}]";
            };
            DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, onTick, Dispatcher);
            timer.Start();

            for (int targetIndex = 0; targetIndex < parent.Children.Count; targetIndex++)
            {
                int currentClusterCount = Math.Max(clusters, 16);
                var targetBitmap = new BitmapCluster<TPixelRepresentation, TPixelData>(sourceBitmap.Pixels, pixelRepresentation, currentClusterCount);
                await targetBitmap.ClusterAsync(5);
                currentClusterCount = clusters;
                var newSeedClusters = await targetBitmap.PickDifferentiatedClusters(currentClusterCount);
                targetBitmap = new BitmapCluster<TPixelRepresentation, TPixelData>(sourceBitmap.Pixels, pixelRepresentation, newSeedClusters);
                await targetBitmap.ClusterAsync(50);
                ((Image)parent.Children[targetIndex]).Source = new StandardRgbBitmap(targetBitmap.Render(), sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();

                if (colorSlices != null)
                {
                    var weights = targetBitmap.ClusterWeights;
                    var colors = targetBitmap.ClusterMeans;

                    var sortedByWeight = weights.Zip(colors, (w, c) => new { Weight = w, Color = c }).OrderByDescending(t => t.Weight).ToArray();

                    colorSlices.Children.Clear();
                    colorSlices.ColumnDefinitions.Clear();
                    for (int i = 0; i < clusters; i++)
                    {
                        colorSlices.ColumnDefinitions.Add(new ColumnDefinition
                        {
                            Width = new GridLength(sortedByWeight[i].Weight, GridUnitType.Star)
                        });
                        var rectangle = new Rectangle
                        {
                            Fill = new SolidColorBrush(sortedByWeight[i].Color.ToWindowsColor()),
                            Margin = new Thickness(2, 0, 0, 0)
                        };
                        Grid.SetColumn(rectangle, i);
                        colorSlices.Children.Add(rectangle);
                    }
                }
            }

            timer.Stop();
            label.Content = null;
        }
    }
}
