using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    public static class WpfExtensions
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

            StandardRgbColor[] pixels = new StandardRgbColor[convertedSource.PixelWidth * convertedSource.PixelHeight];

            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                int target = i / 4;
                pixels[target].B = rawPixels[i];
                pixels[target].G = rawPixels[i + 1];
                pixels[target].R = rawPixels[i + 2];
            }

            return new StandardRgbBitmap(pixels, bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY);
        }

        public static StandardRgbColor ToStandardRgbColor(this Color color)
        {
            return new StandardRgbColor { R = color.R, G = color.G, B = color.B };
        }

        public static byte[] ToBgra32PixelArray(this StandardRgbBitmap bitmap, out int stride)
        {
            stride = bitmap.Width * sizeof(int);

            byte[] rawPixels = new byte[stride * bitmap.Height];
            for (int i = 0; i < rawPixels.Length; i += 4)
            {
                int source = i / 4;
                rawPixels[i] = bitmap.Pixels[source].B;
                rawPixels[i + 1] = bitmap.Pixels[source].G;
                rawPixels[i + 2] = bitmap.Pixels[source].R;
                rawPixels[i + 3] = 0xFF;
            }

            return rawPixels;
        }

        public static BitmapSource ToBitmapSource(this StandardRgbBitmap bitmap)
        {
            byte[] rawPixels = ToBgra32PixelArray(bitmap, out int stride);

            return BitmapSource.Create(bitmap.Width, bitmap.Height, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgra32, null, rawPixels, stride);
        }

        public static void Save(this BitmapSource bitmap, string fileName)
        {
            BitmapEncoder encoder = null;
            if (string.Compare(Path.GetExtension(fileName), ".png", true) == 0)
            {
                encoder = new PngBitmapEncoder();
            }
            else
            {
                encoder = new JpegBitmapEncoder();
            }
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fileStream);
            }
        }

        public static Color ToWindowsColor(this StandardRgbColor pixel)
        {
            return Color.FromRgb(pixel.R, pixel.G, pixel.B);
        }
    }
}
