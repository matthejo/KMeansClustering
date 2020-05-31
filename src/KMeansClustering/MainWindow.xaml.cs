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
        private BitmapBatchClusterOperation[] batchOperations;

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

        public bool IsInBatchMode
        {
            get { return (bool)GetValue(IsInBatchModeProperty); }
            set { SetValue(IsInBatchModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInBatchMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInBatchModeProperty =
            DependencyProperty.Register("IsInBatchMode", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public bool CanCompute
        {
            get { return (bool)GetValue(CanComputeProperty); }
            set { SetValue(CanComputeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanCompute.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanComputeProperty =
            DependencyProperty.Register("CanCompute", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public bool CanLoad
        {
            get { return (bool)GetValue(CanLoadProperty); }
            set { SetValue(CanLoadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanLoad.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanLoadProperty =
            DependencyProperty.Register("CanLoad", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));



        private void LoadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                IsInBatchMode = false;
                originalFileName = IOPath.GetFileNameWithoutExtension(dialog.FileName);
                sourceImage = BitmapFrame.Create(new Uri(dialog.FileName), BitmapCreateOptions.None, BitmapCacheOption.Default);
                OriginalImage.Source = sourceImage;
                CanCompute = true;
            }
        }

        private void LoadBatchImages(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.png",
                Multiselect = true,
            };
            if (dialog.ShowDialog() == true)
            {
                IsInBatchMode = true;
                batchOperations = dialog.FileNames.Select(fn => new BitmapBatchClusterOperation(fn, fn, ColorSpaces.CieLab)).ToArray();
                BatchItems.ItemsSource = batchOperations;
                CanCompute = true;
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

            this.CanLoad = false;
            this.CanCompute = false;

            if (IsInBatchMode)
            {
                await ComputeBatch(clusters);
            }
            else
            {
                await ComputeSingle(clusters);
            }

            this.CanLoad = true;
            this.CanCompute = true;
        }

        private async Task ComputeBatch(int clusters)
        {
            foreach (BitmapBatchClusterOperation operation in batchOperations)
            {
                BatchItems.ScrollIntoView(operation);
                await operation.RunAsync(clusters);
            }
        }

        private async Task ComputeSingle(int clusters)
        {
            StandardRgbBitmap sourceBitmap = sourceImage.ToStandardRgbBitmap();

            Func<Task>[] tasks =
            {
                () => rgbOperation.RunAsync(sourceBitmap, clusters, originalFileName, ShowSteps.IsChecked == true),
                () => cieLuvOperation.RunAsync(sourceBitmap, clusters, originalFileName, ShowSteps.IsChecked == true),
                () => cieLabOperation.RunAsync(sourceBitmap, clusters, originalFileName, ShowSteps.IsChecked == true)
            };

            if (ParallelExecution.IsChecked == true)
            {
                await Task.WhenAll(tasks.Select(t => t()));
            }
            else
            {
                foreach (var t in tasks)
                {
                    await t();
                }
            }
        }
    }
}
