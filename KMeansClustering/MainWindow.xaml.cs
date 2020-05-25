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
            DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, OnTick, Dispatcher);
            timer.Start();

            StandardRgbBitmap sourceBitmap = sourceImage.ToStandardRgbBitmap();

            foreach (Image image in RGBImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }
            foreach (Image image in CIEXYZImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }
            foreach (Image image in CIELABImageGrid.Children.Cast<Image>())
            {
                image.Source = null;
            }

            await Task.WhenAll(
                UpdateGroup<StandardRgbPixelRepresentation, StandardRgbPixelData>(clusters, sourceBitmap, PixelRepresentations.Rgb, RGBImageGrid),
                UpdateGroup<CieXyzPixelRepresentation, CieXyzPixelData>(clusters, sourceBitmap, PixelRepresentations.CieXyz, CIEXYZImageGrid),
                UpdateGroup<CieLabPixelRepresentation, CieLabPixelData>(clusters, sourceBitmap, PixelRepresentations.CieLab, CIELABImageGrid)
                );

            timer.Stop();
            this.IsEnabled = true;
            ResultStatus.Content = $"Completed in {(int)((DateTime.Now - computeStarted).TotalSeconds)} seconds.";

        }

        private async Task UpdateGroup<TPixelRepresentation, TPixelData>(int clusters, StandardRgbBitmap sourceBitmap, TPixelRepresentation pixelRepresentation, Panel parent)
            where TPixelData : struct
            where TPixelRepresentation : IPixelRepresentation<TPixelData>
        {
            int currentClusterCount = Math.Max(clusters, 16);
            var targetBitmap = new BitmapCluster<TPixelRepresentation, TPixelData>(sourceBitmap.Pixels, pixelRepresentation, currentClusterCount);
            for (int targetIndex = 0; targetIndex < parent.Children.Count; targetIndex++)
            {
                await targetBitmap.ClusterAsync(currentClusterCount == clusters ? 50 : 5);
                ((Image)parent.Children[targetIndex]).Source = new StandardRgbBitmap(targetBitmap.Render(), sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();

                if (currentClusterCount == clusters)
                {
                    currentClusterCount = Math.Max(clusters, 16);
                    targetBitmap = new BitmapCluster<TPixelRepresentation, TPixelData>(sourceBitmap.Pixels, pixelRepresentation, currentClusterCount);
                }
                else
                {
                    currentClusterCount = clusters;
                    var newSeedClusters = await targetBitmap.PickDifferentiatedClusters(currentClusterCount);
                    targetBitmap = new BitmapCluster<TPixelRepresentation, TPixelData>(sourceBitmap.Pixels, pixelRepresentation, newSeedClusters);
                }
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            ResultStatus.Content = $"Computing clusters... [{DateTime.Now - computeStarted:mm\\:ss}]";
        }
    }
}
