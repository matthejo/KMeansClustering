using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    internal static class WpfExtensions
    {
        public static StandardRgbBitmap ToStandardRgbBitmap(this BitmapSource bitmap)
        {
            BitmapSource convertedSource = bitmap;
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                convertedSource = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 1.0);
            }

            int stride = convertedSource.PixelWidth * sizeof(int);
            byte[] rawPixels = new byte[stride * convertedSource.PixelHeight];
            convertedSource.CopyPixels(rawPixels, stride, offset: 0);

            StandardRgbPixelData[] pixelData = new StandardRgbPixelData[convertedSource.PixelWidth * convertedSource.PixelHeight];

            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                int target = i / 4;
                pixelData[target].B = rawPixels[i];
                pixelData[target].G = rawPixels[i + 1];
                pixelData[target].R = rawPixels[i + 2];
            }

            return new StandardRgbBitmap(pixelData, bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY);
        }

        public static BitmapSource ToBitmapSource(this StandardRgbBitmap bitmap)
        {
            int stride = bitmap.Width * sizeof(int);

            byte[] rawPixels = new byte[stride * bitmap.Height];
            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                int source = i / 4;
                rawPixels[i] = bitmap.Pixels[source].B;
                rawPixels[i + 1] = bitmap.Pixels[source].G;
                rawPixels[i + 2] = bitmap.Pixels[source].R;
                rawPixels[i + 3] = 0xFF;
            }

            return BitmapSource.Create(bitmap.Width, bitmap.Height, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgra32, null, rawPixels, stride);
        }

        public static Color ToWindowsColor(this StandardRgbPixelData pixel)
        {
            return Color.FromRgb(pixel.R, pixel.G, pixel.B);
        }
    }
}
