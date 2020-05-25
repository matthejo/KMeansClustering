using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal static class PixelRepresentations
    {
        public static readonly StandardRgbPixelRepresentation Rgb = new StandardRgbPixelRepresentation();
        public static readonly HslPixelRepresentation Hsl = new HslPixelRepresentation();
        public static readonly CieXyzPixelRepresentation CieXyz = new CieXyzPixelRepresentation();
        public static readonly CieLabPixelRepresentation CieLab = new CieLabPixelRepresentation();
    }

    internal interface IPixelRepresentation<TPixelData>
    {
        double DistanceSquared(TPixelData a, TPixelData b);
        bool Equals(TPixelData a, TPixelData b);
        void ToPixelData(byte[] sourceRgbPixels, TPixelData[] targetPixelData, int sourcePixelIndex);
        void FromPixelData(TPixelData[] sourcePixelData, byte[] targetRgbPixels, int targetPixelIndex);
        void AddSample(ref PixelDataMeanAccumulator accumulator, TPixelData sample);
        TPixelData GetAverage(PixelDataMeanAccumulator accumulator);
    }

    internal struct PixelDataMeanAccumulator
    {
        public double XTotal;
        public double YTotal;
        public double ZTotal;
        public double WTotal;
        public int Count;

        public void AddSample(double x, double y, double z, double w = 0)
        {
            XTotal += x;
            YTotal += y;
            ZTotal += z;
            WTotal += w;
            Count++;
        }

        public void GetAverage(out double x, out double y, out double z)
        {
            x = XTotal / Count;
            y = YTotal / Count;
            z = ZTotal / Count;
        }
    }
}
