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
            int iterationCount = 0;

            StandardRgbBitmap sourceBitmap = sourceImage.ToStandardRgbBitmap();

            RGBImage.Source = null;
            CIEXYZImage.Source = null;
            CIELABImage.Source = null;
            
            iterationCount = await UpdateRGB(clusters, iterationCount, sourceBitmap);
            
            iterationCount = await UpdateCIEXYZ(clusters, iterationCount, sourceBitmap);
            
            iterationCount = await UpdateCIELAB(clusters, iterationCount, sourceBitmap);
            
            timer.Stop();
            this.IsEnabled = true;
            ResultStatus.Content = $"Completed in {(int)((DateTime.Now - computeStarted).TotalSeconds)} seconds.";

        }

        private async Task<int> UpdateCIELAB(int clusters, int iterationCount, StandardRgbBitmap sourceBitmap)
        {
            var targetBitmap = new BitmapCluster<CieLabPixelRepresentation, CieLabPixelData>(sourceBitmap.Pixels, PixelRepresentations.CieLab, clusters);
            iterationCount = await targetBitmap.ClusterAsync(clusters);
            this.CIELABImage.Source = new StandardRgbBitmap(targetBitmap.Render(), sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();
            return iterationCount;
        }

        private async Task<int> UpdateCIEXYZ(int clusters, int iterationCount, StandardRgbBitmap sourceBitmap)
        {
            var targetBitmap = new BitmapCluster<CieXyzPixelRepresentation, CieXyzPixelData>(sourceBitmap.Pixels, PixelRepresentations.CieXyz, clusters);
            iterationCount = await targetBitmap.ClusterAsync(clusters);
            this.CIEXYZImage.Source = new StandardRgbBitmap(targetBitmap.Render(), sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();
            return iterationCount;
        }

        private async Task<int> UpdateRGB(int clusters, int iterationCount, StandardRgbBitmap sourceBitmap)
        {
            var targetBitmap = new BitmapCluster<StandardRgbPixelRepresentation, StandardRgbPixelData>(sourceBitmap.Pixels, PixelRepresentations.Rgb, clusters);
            iterationCount = await targetBitmap.ClusterAsync(clusters);
            this.RGBImage.Source = new StandardRgbBitmap(targetBitmap.Render(), sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();
            return iterationCount;
        }

        private void OnTick(object sender, EventArgs e)
        {
            ResultStatus.Content = $"Computing clusters... [{DateTime.Now - computeStarted:mm\\:ss}]";
        }
    }
}
