using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    internal sealed class StandardRgbBitmap
    {
        public StandardRgbPixelData[] Pixels { get; }

        public int Width { get; }

        public int Height { get; }

        public double DpiX { get; }

        public double DpiY { get; }

        public StandardRgbBitmap(StandardRgbPixelData[] pixels, int width, int height, double dpiX, double dpiY)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            DpiX = dpiX;
            DpiY = dpiY;
        }
    }
}
