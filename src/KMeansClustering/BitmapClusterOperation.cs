using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KMeansClustering
{
    internal class BitmapClusterOperation : INotifyPropertyChanged
    {
        private readonly IColorSpace colorSpace;
        private readonly string fileSuffix;
        private string status;
        private bool isRunning;
        private bool isComplete;
        private BitmapSource bitmap;
        private IList<int> colorWeights;
        private IList<Color> colors;

        private string originalFileName;

        public event PropertyChangedEventHandler PropertyChanged;

        public string OperationName { get; }

        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set { SetProperty(ref isRunning, value); }
        }

        public bool IsComplete
        {
            get { return isComplete; }
            set { SetProperty(ref isComplete, value); }
        }

        public BitmapSource Bitmap
        {
            get { return bitmap; }
            set { SetProperty(ref bitmap, value); }
        }

        public IList<int> ColorWeights
        {
            get { return colorWeights; }
            set { SetProperty(ref colorWeights, value); }
        }

        public IList<Color> Colors
        {
            get { return colors; }
            set { SetProperty(ref colors, value); }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand(OnSave);
            }
        }

        public BitmapClusterOperation(string operationName, IColorSpace colorSpace, string fileSuffix)
        {
            OperationName = operationName;
            this.colorSpace = colorSpace;
            this.fileSuffix = fileSuffix;
        }

        public async Task RunAsync(StandardRgbBitmap sourceBitmap, int clusters, string originalFileName, bool showAllSteps)
        {
            this.originalFileName = originalFileName;
            this.Bitmap = null;
            IsRunning = true;
            IsComplete = false;
            ColorWeights = null;
            Colors = null;
            DateTime startTime = DateTime.Now;

            string currentStatus = "Computing clusters...";
            Lazy<BitmapSource> currentBitmap = new Lazy<BitmapSource>(() => null);

            EventHandler onTick = (sender, e) =>
            {
                Status = $"{currentStatus} [{DateTime.Now - startTime:mm\\:ss}]";
                Bitmap = currentBitmap.Value;
            };

            DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, onTick, Dispatcher.CurrentDispatcher);
            timer.Start();

            BitmapCluster targetBitmap = null;
            if (clusters < 16)
            {
                currentStatus = "Creating initial 16-cluster seed... (iteration 0)";
                targetBitmap = new BitmapCluster(sourceBitmap.Pixels, colorSpace, 16);
                await targetBitmap.ClusterAsync(async i =>
                {
                    currentStatus = $"Creating initial 16-cluster seed... (iteration {i})";
                    if (showAllSteps)
                    {
                        var currentBitmapContent = await targetBitmap.RenderAsync();
                        currentBitmap = CreateLazyBitmap(sourceBitmap, currentBitmapContent, targetBitmap.GetClusterWeights(), targetBitmap.GetClusterMeans());
                    }
                }, 3);

                currentStatus = $"Rendering 16-cluster image...";
                var intermediateBitmapContent = await targetBitmap.RenderAsync();
                currentBitmap = CreateLazyBitmap(sourceBitmap, intermediateBitmapContent, targetBitmap.GetClusterWeights(), targetBitmap.GetClusterMeans());

                currentStatus = $"Choosing refined seed colors...";
                var newSeedClusters = await targetBitmap.ChooseDifferentiatedClusters(clusters);
                targetBitmap = new BitmapCluster(sourceBitmap.Pixels, colorSpace, newSeedClusters);
            }
            else
            {
                targetBitmap = new BitmapCluster(sourceBitmap.Pixels, colorSpace, clusters);
            }

            currentStatus = $"Computing {clusters}-cluster image... (iteration 0)";
            await targetBitmap.ClusterAsync(async i =>
            {
                currentStatus = $"Computing {clusters}-cluster image... (iteration {i})";
                if (showAllSteps)
                {
                    var currentBitmapContent = await targetBitmap.RenderAsync();
                    currentBitmap = CreateLazyBitmap(sourceBitmap, currentBitmapContent, targetBitmap.GetClusterWeights(), targetBitmap.GetClusterMeans());
                }
            }, 200);

            currentStatus = $"Rendering {clusters}-cluster image...";
            var finalBitmapContent = await targetBitmap.RenderAsync();
            currentBitmap = CreateLazyBitmap(sourceBitmap, finalBitmapContent, targetBitmap.GetClusterWeights(), targetBitmap.GetClusterMeans());
            Bitmap = currentBitmap.Value;

            timer.Stop();
            Status = null;
            IsRunning = false;
            IsComplete = true;
        }

        private Lazy<BitmapSource> CreateLazyBitmap(StandardRgbBitmap sourceBitmap, StandardRgbColor[] colors, int[] clusterWeights, StandardRgbColor[] clusterColors)
        {
            return new Lazy<BitmapSource>(() =>
            {
                UpdateColorHistogram(clusterWeights, clusterColors);
                return new StandardRgbBitmap(colors, sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.DpiX, sourceBitmap.DpiY).ToBitmapSource();
            });
        }

        private void UpdateColorHistogram(int[] weights, StandardRgbColor[] colors)
        {
            var sortedByWeight = weights.Zip(colors, (w, c) => new { Weight = w, Color = c }).OrderByDescending(t => t.Weight).ToArray();

            ColorWeights = sortedByWeight.Select(t => t.Weight).ToArray();
            Colors = sortedByWeight.Select(t => t.Color.ToWindowsColor()).ToArray();
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!object.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnSave()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                FileName = $"{originalFileName}{fileSuffix}_#{ColorWeights.Count}.png",
                Filter = "PNG images|*.png|JPG images|*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = null;
                if (string.Compare(Path.GetExtension(dialog.FileName), ".png", true) == 0)
                {
                    encoder = new PngBitmapEncoder();
                }
                else
                {
                    encoder = new JpegBitmapEncoder();
                }
                encoder.Frames.Add(BitmapFrame.Create(Bitmap));

                using (FileStream fileStream = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fileStream);
                }
            }
        }
    }
}
