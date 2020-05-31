using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KMeansClustering
{
    internal sealed class StandardRgbColorSpace : IColorSpace
    {
        public Vector3 ConvertFromStandardRgb(StandardRgbColor pixel)
        {
            return (Vector3)pixel;
        }

        public StandardRgbColor ConvertToStandardRgb(Vector3 pixel)
        {
            return (StandardRgbColor)pixel;
        }
    }

    internal struct StandardRgbColor
    {
        public byte R;
        public byte G;
        public byte B;

        public static explicit operator Vector3(StandardRgbColor source)
        {
            return new Vector3(source.R, source.G, source.B);
        }

        public static explicit operator StandardRgbColor(Vector3 source)
        {
            return new StandardRgbColor
            {
                R = (byte)Math.Max(0, Math.Min(255, Math.Round(source.X))),
                G = (byte)Math.Max(0, Math.Min(255, Math.Round(source.Y))),
                B = (byte)Math.Max(0, Math.Min(255, Math.Round(source.Z)))
            };
        }
    }
}
