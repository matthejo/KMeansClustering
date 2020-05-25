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
            if (PixelRep.SelectedIndex == 0)
            {
                var targetBitmap = new MemoryBitmap<StandardRgbPixelRepresentation, StandardRgbPixelData>(sourceImage, PixelRepresentations.Rgb);
                iterationCount = await targetBitmap.ClusterAsync(clusters);
                this.TransformedImage.Source = targetBitmap.Render();
            }
            else if (PixelRep.SelectedIndex == 1)
            {
                var targetBitmap = new MemoryBitmap<HslPixelRepresentation, HslPixelData>(sourceImage, PixelRepresentations.Hsl);
                iterationCount = await targetBitmap.ClusterAsync(clusters);
                this.TransformedImage.Source = targetBitmap.Render();
            }
            else if (PixelRep.SelectedIndex == 2)
            {
                var targetBitmap = new MemoryBitmap<CieXyzPixelRepresentation, CieXyzPixelData>(sourceImage, PixelRepresentations.CieXyz);
                iterationCount = await targetBitmap.ClusterAsync(clusters);
                this.TransformedImage.Source = targetBitmap.Render();
            }
            else
            {
                var targetBitmap = new MemoryBitmap<CieLabPixelRepresentation, CieLabPixelData>(sourceImage, PixelRepresentations.CieLab);
                iterationCount = await targetBitmap.ClusterAsync(clusters);
                this.TransformedImage.Source = targetBitmap.Render();
            }
            timer.Stop();
            this.IsEnabled = true;
            ResultStatus.Content = $"Completed in {iterationCount} iterations in {(int)((DateTime.Now - computeStarted).TotalSeconds)} seconds.";

        }

        private void OnTick(object sender, EventArgs e)
        {
            ResultStatus.Content = $"Computing clusters... [{DateTime.Now - computeStarted:mm\\:ss}]";
        }
    }
}
