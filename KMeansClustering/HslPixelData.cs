using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal sealed class HslPixelRepresentation : IPixelRepresentation<HslPixelData>
    {
        private const double sWeight = 1.0;
        private const double lWeight = 2.0;
        private const double hWeight = 1.0;

        public void AddSample(ref PixelDataMeanAccumulator accumulator, HslPixelData sample)
        {
            double alw = sample.L < 0.5 ? (sample.L / 0.5) : (1 - sample.L) / 0.5;

            accumulator.XTotal += Math.Sin(sample.H * Math.PI / 180.0);
            accumulator.YTotal += Math.Cos(sample.H * Math.PI / 180.0);
            accumulator.ZTotal += sample.S * alw;
            accumulator.WTotal += sample.L;
            accumulator.Count++;
        }

        public double DistanceSquared(HslPixelData a, HslPixelData b)
        {
            double alw = a.L < 0.5 ? (a.L / 0.5) : (1 - a.L) / 0.5;
            double blw = b.L < 0.5 ? (b.L / 0.5) : (1 - b.L) / 0.5;

            double hDelta = Math.Abs(a.H - b.H);
            double sDelta = a.S * alw - b.S * b.L;
            double lDelta = a.L - b.L;

            if (hDelta > 180)
            {
                hDelta = 360 - hDelta;
            }

            hDelta /= 180;

            hDelta *= hWeight;
            sDelta *= sWeight;
            lDelta *= lWeight;

            return hDelta * hDelta + sDelta * sDelta + lDelta * lDelta;
        }

        public bool Equals(HslPixelData a, HslPixelData b)
        {
            return a.ToRgb() == b.ToRgb();
        }

        public void FromPixelData(HslPixelData[] sourcePixelData, byte[] targetRgbPixels, int targetPixelIndex)
        {
            int sourceIndex = targetPixelIndex / 4;

            RgbPixelData rgb = sourcePixelData[sourceIndex].ToRgb();

            targetRgbPixels[targetPixelIndex] = rgb.B;
            targetRgbPixels[targetPixelIndex + 1] = rgb.G;
            targetRgbPixels[targetPixelIndex + 2] = rgb.R;
            targetRgbPixels[targetPixelIndex + 3] = 0xFF;
        }

        public HslPixelData GetAverage(PixelDataMeanAccumulator accumulator)
        {
            return new HslPixelData
            {
                H = Math.Atan2(accumulator.XTotal / accumulator.Count, accumulator.YTotal / accumulator.Count) * 180.0 / Math.PI,
                S = accumulator.ZTotal / accumulator.Count,
                L = accumulator.WTotal / accumulator.Count
            };
        }

        public void ToPixelData(byte[] sourceRgbPixels, HslPixelData[] targetPixelData, int sourcePixelIndex)
        {
            int targetIndex = sourcePixelIndex / 4;
            byte b = sourceRgbPixels[sourcePixelIndex];
            byte g = sourceRgbPixels[sourcePixelIndex + 1];
            byte r = sourceRgbPixels[sourcePixelIndex + 2];

            byte max = Math.Max(r, Math.Max(g, b));
            byte min = Math.Min(r, Math.Min(g, b));
            double floatMax = (double)max / byte.MaxValue;
            double floatMin = (double)min / byte.MaxValue;

            double h;
            double s;
            double l;
            if (max == min)
            {
                h = 0;
            }
            else if (max == r)
            {
                h = ((60.0 * (g - b) / (double)(max - min)) + 360.0) % 360.0;
            }
            else if (max == g)
            {
                h = (60.0 * (b - r) / (double)(max - min)) + 120.0;
            }
            else
            {
                h = (60.0 * (r - g) / (double)(max - min)) + 240.0;
            }

            l = 0.5 * (floatMax + floatMin);

            if (max == min)
            {
                s = 0;
            }
            else if (l <= 0.5)
            {
                s = (floatMax - floatMin) / (2 * l);
            }
            else
            {
                s = (floatMax - floatMin) / (2 - (2 * l));
            }

            targetPixelData[targetIndex].H = h;
            targetPixelData[targetIndex].S = s;
            targetPixelData[targetIndex].L = l;
        }
    }

    internal struct HslPixelData
    {
        public double H;
        public double S;
        public double L;

        public RgbPixelData ToRgb()
        {
            double q = L < 0.5 ? (L * (1 + S)) : (L + S - (L * S));
            double p = (2 * L) - q;
            double hK = H / 360.0;
            double tR = ModOne(hK + (1.0 / 3.0));
            double tG = hK;
            double tB = ModOne(hK - (1.0 / 3.0));

            byte r = (byte)(ComputeRGBComponent(p, q, tR) * byte.MaxValue);
            byte g = (byte)(ComputeRGBComponent(p, q, tG) * byte.MaxValue);
            byte b = (byte)(ComputeRGBComponent(p, q, tB) * byte.MaxValue);

            return new RgbPixelData
            {
                R = r,
                G = g,
                B = b
            };
        }

        private static double ComputeRGBComponent(double p, double q, double tC)
        {
            if (tC < 1.0 / 6.0)
            {
                return p + ((q - p) * 6 * tC);
            }
            else if (tC < 1.0 / 2.0)
            {
                return q;
            }
            else if (tC < 2.0 / 3.0)
            {
                return p + ((q - p) * 6 * ((2.0 / 3.0) - tC));
            }
            else
            {
                return p;
            }
        }

        private static double ModOne(double value)
        {
            if (value < 0)
            {
                return value + 1;
            }
            else if (value > 1)
            {
                return value - 1;
            }
            else
            {
                return value;
            }
        }
    }
}
