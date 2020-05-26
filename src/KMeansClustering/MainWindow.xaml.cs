using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using IOPath = System.IO.Path;

namespace KMeansClustering
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapClusterOperation cieLabOperation;
        private BitmapClusterOperation cieLuvOperation;
        private BitmapClusterOperation rgbOperation;
        private BitmapSource sourceImage;
        private string originalFileName;

        public MainWindow()
        {
            InitializeComponent();

            rgbOperation = new BitmapClusterOperation("sRGB", ColorSpaces.Rgb, "_sRGB");
            cieLuvOperation = new BitmapClusterOperation("CIE L*u*v*", ColorSpaces.CieLuv, "_CIELUV");
            cieLabOperation = new BitmapClusterOperation("CIE L*a*b*", ColorSpaces.CieLab, "_CIELAB");

            sRGB.Content = rgbOperation;
            CIELuv.Content = cieLuvOperation;
            CIELab.Content = cieLabOperation;
        }

        private void LoadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                originalFileName = IOPath.GetFileNameWithoutExtension(dialog.FileName);
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
            if (clusters < 1 || clusters > 100)
            {
                MessageBox.Show("Clusters must be between 1 and 100");
                return;
            }

            this.IsEnabled = false;

            StandardRgbBitmap sourceBitmap = sourceImage.ToStandardRgbBitmap();

            await Task.WhenAll(
                rgbOperation.RunAsync(sourceBitmap, clusters, originalFileName),
                cieLuvOperation.RunAsync(sourceBitmap, clusters, originalFileName),
                cieLabOperation.RunAsync(sourceBitmap, clusters, originalFileName));

            this.IsEnabled = true;
        }
    }
}
