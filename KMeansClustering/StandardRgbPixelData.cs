using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KMeansClustering
{
    internal sealed class StandardRgbPixelRepresentation : IPixelRepresentation
    {
        public Vector3 ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return (Vector3)pixel;
        }

        public StandardRgbPixelData ConvertToStandardRgb(Vector3 pixel)
        {
            return (StandardRgbPixelData)pixel;
        }
    }

    internal struct StandardRgbPixelData
    {
        public byte R;
        public byte G;
        public byte B;

        public static explicit operator Vector3(StandardRgbPixelData source)
        {
            return new Vector3(source.R, source.G, source.B);
        }

        public static explicit operator StandardRgbPixelData(Vector3 source)
        {
            return new StandardRgbPixelData
            {
                R = (byte)Math.Round(source.X),
                G = (byte)Math.Round(source.Y),
                B = (byte)Math.Round(source.Z)
            };
        }
    }
}
