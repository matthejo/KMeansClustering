using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class StandardRgbPixelRepresentation : IPixelRepresentation<StandardRgbPixelData>
    {
        public void AddSample(ref PixelDataMeanAccumulator accumulator, StandardRgbPixelData sample)
        {
            accumulator.AddSample(sample.R, sample.G, sample.B);
        }

        public StandardRgbPixelData ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return pixel;
        }

        public StandardRgbPixelData ConvertToStandardRgb(StandardRgbPixelData pixel)
        {
            return pixel;
        }

        public double DistanceSquared(StandardRgbPixelData a, StandardRgbPixelData b)
        {
            double deltaR = a.R - b.R;
            double deltaG = a.G - b.G;
            double deltaB = a.B - b.B;

            return deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
        }

        public bool Equals(StandardRgbPixelData a, StandardRgbPixelData b)
        {
            return a == b;
        }

        public StandardRgbPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double r, out double g, out double b);
            return new StandardRgbPixelData
            {
                R = (byte)Math.Round(r),
                G = (byte)Math.Round(g),
                B = (byte)Math.Round(b),
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StandardRgbPixelData
    {
        public byte R;
        public byte G;
        public byte B;

        public override bool Equals(object obj)
        {
            if (!(obj is StandardRgbPixelData other))
            {
                return false;
            }

            return this == other;
        }

        public override int GetHashCode()
        {
            return R << 16 | G << 8 | B;
        }

        public static bool operator ==(StandardRgbPixelData a, StandardRgbPixelData b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B;
        }

        public static bool operator !=(StandardRgbPixelData a, StandardRgbPixelData b)
        {
            return !(a == b);
        }
    }
}
