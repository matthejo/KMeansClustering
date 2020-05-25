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

        public CieXyzPixelData ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return pixel.ToCieXyz();
        }

        public StandardRgbPixelData ConvertToStandardRgb(CieXyzPixelData pixel)
        {
            return pixel.ToStandardRgb();
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

        public CieXyzPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            accumulator.GetAverage(out double x, out double y, out double z);
            return new CieXyzPixelData { X = x, Y = y, Z = z };
        }
    }

    internal struct CieXyzPixelData
    {
        public double X;
        public double Y;
        public double Z;
    }
}
