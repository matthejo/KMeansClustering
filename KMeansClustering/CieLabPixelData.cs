using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class CieLabPixelRepresentation : IPixelRepresentation<CieLabPixelData>
    {
        private readonly double Epsilon = 0.00001;

        public void AddSample(ref PixelDataMeanAccumulator accumulator, CieLabPixelData sample)
        {
            accumulator.AddSample(sample.L, sample.a, sample.b);
        }

        public double DistanceSquared(CieLabPixelData a, CieLabPixelData b)
        {
            double lDelta = a.L - b.L;
            double aDelta = a.a - b.a;
            double bDelta = a.b - b.b;

            return lDelta * lDelta + aDelta * aDelta + bDelta * bDelta;
        }

        public bool Equals(CieLabPixelData a, CieLabPixelData b)
        {
            return Math.Abs(a.L - b.L) < Epsilon &&
                Math.Abs(a.a - b.a) < Epsilon &&
                Math.Abs(a.b - b.b) < Epsilon;
        }

        public void FromPixelData(CieLabPixelData[] sourcePixelData, byte[] targetRgbPixels, int targetPixelIndex)
        {
            int sourceIndex = targetPixelIndex / 4;
            StandardRgbPixelData rgbData = sourcePixelData[sourceIndex].ToStandardRgb();
            targetRgbPixels[targetPixelIndex] = rgbData.B;
            targetRgbPixels[targetPixelIndex + 1] = rgbData.G;
            targetRgbPixels[targetPixelIndex + 2] = rgbData.R;
            targetRgbPixels[targetPixelIndex + 3] = 0xFF;
        }

        public CieLabPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double x, out double y, out double z);
            return new CieLabPixelData { L = x, a = y, b = z };
        }

        public void ToPixelData(byte[] sourceRgbPixels, CieLabPixelData[] targetPixelData, int sourcePixelIndex)
        {
            int targetIndex = sourcePixelIndex / 4;
            StandardRgbPixelData rgbPixel = new StandardRgbPixelData { B = sourceRgbPixels[sourcePixelIndex], G = sourceRgbPixels[sourcePixelIndex + 1], R = sourceRgbPixels[sourcePixelIndex + 2] };
            targetPixelData[targetIndex] = rgbPixel.ToCieLab();
        }
    }

    internal struct CieLabPixelData
    {
        public double L;
        public double a;
        public double b;
    }
}
