using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class CieXyzPixelRepresentation : IPixelRepresentation<CieXyzPixelData>
    {
        private readonly double Epsilon = 0.00001;

        public void AddSample(ref PixelDataMeanAccumulator accumulator, CieXyzPixelData sample)
        {
            accumulator.AddSample(sample.X, sample.Y, sample.Z);
        }

        public double DistanceSquared(CieXyzPixelData a, CieXyzPixelData b)
        {
            double xDelta = a.X - b.X;
            double yDelta = a.Y - b.Y;
            double zDelta = a.Z - b.Z;

            return xDelta * xDelta + yDelta * yDelta + zDelta * zDelta;
        }

        public bool Equals(CieXyzPixelData a, CieXyzPixelData b)
        {
            return Math.Abs(a.X - b.X) < Epsilon &&
                Math.Abs(a.Y - b.Y) < Epsilon &&
                Math.Abs(a.Z - b.Z) < Epsilon;
        }

        public void FromPixelData(CieXyzPixelData[] sourcePixelData, byte[] targetRgbPixels, int targetPixelIndex)
        {
            int sourceIndex = targetPixelIndex / 4;
            StandardRgbPixelData rgbData = sourcePixelData[sourceIndex].ToLinearRgb().ToStandardRgb();
            targetRgbPixels[targetPixelIndex] = rgbData.B;
            targetRgbPixels[targetPixelIndex + 1] = rgbData.G;
            targetRgbPixels[targetPixelIndex + 2] = rgbData.R;
            targetRgbPixels[targetPixelIndex + 3] = 0xFF;
        }

        public CieXyzPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double x, out double y, out double z);
            return new CieXyzPixelData { X = x, Y = y, Z = z };
        }

        public void ToPixelData(byte[] sourceRgbPixels, CieXyzPixelData[] targetPixelData, int sourcePixelIndex)
        {
            int targetIndex = sourcePixelIndex / 4;
            StandardRgbPixelData rgbPixel = new StandardRgbPixelData { B = sourceRgbPixels[sourcePixelIndex], G = sourceRgbPixels[sourcePixelIndex + 1], R = sourceRgbPixels[sourcePixelIndex + 2] };
            targetPixelData[targetIndex] = rgbPixel.ToLinearRgb().ToCieXyz();
        }
    }

    internal struct CieXyzPixelData
    {
        public double X;
        public double Y;
        public double Z;
    }
}
