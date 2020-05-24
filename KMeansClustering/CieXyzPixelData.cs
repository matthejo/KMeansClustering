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
            RgbPixelData rgbData = sourcePixelData[sourceIndex].ToLinearRgb().ToStandardRgb();
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
            RgbPixelData rgbPixel = new RgbPixelData { B = sourceRgbPixels[sourcePixelIndex], G = sourceRgbPixels[sourcePixelIndex + 1], R = sourceRgbPixels[sourcePixelIndex + 2] };
            targetPixelData[targetIndex] = rgbPixel.ToLinearRgb().ToCieXyz();
        }
    }

    internal struct CieXyzPixelData
    {
        private static readonly Matrix4x4 cieToLinearRgbTransform = Matrix4x4.Transpose(new Matrix4x4
        {
            M11 = 3.24096994f,
            M12 = -1.53738318f,
            M13 = -0.49861076f,
            M14 = 0.0f,

            M21 = -0.96924364f,
            M22 = 1.8759675f,
            M23 = 0.04155506f,
            M24 = 0.0f,

            M31 = 0.05563008f,
            M32 = -0.20397696f,
            M33 = 1.05697151f,
            M34 = 0.0f,

            M41 = 0.0f,
            M42 = 0.0f,
            M43 = 0.0f,
            M44 = 1.0f
        });

        public double X;
        public double Y;
        public double Z;

        public LinearRgbPixelData ToLinearRgb()
        {
            Vector4 xyz = new Vector4((float)X, (float)Y, (float)Z, 1.0f);
            Vector4 rgb = Vector4.Transform(xyz, cieToLinearRgbTransform);

            return new LinearRgbPixelData
            {
                R = rgb.X,
                G = rgb.Y,
                B = rgb.Z
            };
        }
    }
}
