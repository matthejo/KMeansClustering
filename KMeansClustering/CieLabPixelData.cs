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

        public CieLabPixelData ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return pixel.ToCieLab();
        }

        public StandardRgbPixelData ConvertToStandardRgb(CieLabPixelData pixel)
        {
            return pixel.ToStandardRgb();
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

        public CieLabPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double x, out double y, out double z);
            return new CieLabPixelData { L = x, a = y, b = z };
        }
    }

    internal struct CieLabPixelData
    {
        public double L;
        public double a;
        public double b;
    }
}
