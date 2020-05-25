using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class CieLuvPixelRepresentation : IPixelRepresentation<CieLuvPixelData>
    {
        private readonly double Epsilon = 0.00001;

        public void AddSample(ref PixelDataMeanAccumulator accumulator, CieLuvPixelData sample)
        {
            accumulator.AddSample(sample.L, sample.u, sample.v);
        }

        public CieLuvPixelData ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return pixel.ToCieLuv();
        }

        public StandardRgbPixelData ConvertToStandardRgb(CieLuvPixelData pixel)
        {
            return pixel.ToStandardRgb();
        }

        public double DistanceSquared(CieLuvPixelData a, CieLuvPixelData b)
        {
            double lDelta = a.L - b.L;
            double uDelta = a.u - b.u;
            double vDelta = a.v - b.v;

            return lDelta * lDelta + uDelta * uDelta + vDelta * vDelta;
        }

        public bool Equals(CieLuvPixelData a, CieLuvPixelData b)
        {
            return Math.Abs(a.L - b.L) < Epsilon &&
                Math.Abs(a.u - b.u) < Epsilon &&
                Math.Abs(a.v - b.v) < Epsilon;
        }

        public CieLuvPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double x, out double y, out double z);
            return new CieLuvPixelData { L = x, u = y, v = z };
        }
    }

    internal struct CieLuvPixelData
    {
        public double L;
        public double u;
        public double v;
    }
}
