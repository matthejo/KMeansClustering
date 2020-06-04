namespace KMeansClustering
{
    public sealed class StandardRgbBitmap
    {
        public StandardRgbColor[] Pixels { get; }

        public int Width { get; }

        public int Height { get; }

        public double DpiX { get; }

        public double DpiY { get; }

        public StandardRgbBitmap(StandardRgbColor[] pixels, int width, int height, double dpiX, double dpiY)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            DpiX = dpiX;
            DpiY = dpiY;
        }
    }
}
