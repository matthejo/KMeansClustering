using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal static class ColorSpaceConversion
    {
        private static class CieLab
        {
            public const double Xn = 95.0489;
            public const double Yn = 100;
            public const double Zn = 108.8840;
            public const double delta = 6.0 / 29.0;
        }

        private static readonly Matrix4x4 linearRgbToCieTransform = Matrix4x4.Transpose(new Matrix4x4
        {
            M11 = 0.41239080f,
            M12 = 0.35758434f,
            M13 = 0.18048079f,
            M14 = 0.0f,

            M21 = 0.21263901f,
            M22 = 0.71516868f,
            M23 = 0.07219232f,
            M24 = 0.0f,

            M31 = 0.01933082f,
            M32 = 0.11919478f,
            M33 = 0.95053215f,
            M34 = 0.0f,

            M41 = 0.0f,
            M42 = 0.0f,
            M43 = 0.0f,
            M44 = 1.0f
        });

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

        public static StandardRgbPixelData ToStandardRgb(this LinearRgbPixelData source)
        {
            return new StandardRgbPixelData
            {
                R = (byte)Math.Round(convertLinearToGamma(source.R) * 255),
                G = (byte)Math.Round(convertLinearToGamma(source.G) * 255),
                B = (byte)Math.Round(convertLinearToGamma(source.B) * 255)
            };

            double convertLinearToGamma(double u)
            {
                return u <= 0.003308 ? u * 12.92 : 1.055 * Math.Pow(u, 5.0 / 12.0) - 0.055;
            }
        }

        public static StandardRgbPixelData ToStandardRgb(this CieXyzPixelData source)
        {
            return source.ToLinearRgb().ToStandardRgb();
        }

        public static StandardRgbPixelData ToStandardRgb(this CieLabPixelData source)
        {
            return source.ToCieXyz().ToStandardRgb();
        }

        public static LinearRgbPixelData ToLinearRgb(this StandardRgbPixelData source)
        {
            double sR = source.R / 255.0;
            double sG = source.G / 255.0;
            double sB = source.B / 255.0;

            return new LinearRgbPixelData
            {
                R = convertGammaToLinear(sR),
                G = convertGammaToLinear(sG),
                B = convertGammaToLinear(sB)
            };

            double convertGammaToLinear(double u)
            {
                return u <= 0.04045 ? u / 12.92 : Math.Pow((u + 0.055) / 1.055, 2.4);
            }
        }

        public static LinearRgbPixelData ToLinearRgb(this CieXyzPixelData source)
        {
            Vector4 xyz = new Vector4((float)source.X, (float)source.Y, (float)source.Z, 1.0f);
            Vector4 rgb = Vector4.Transform(xyz, cieToLinearRgbTransform);

            return new LinearRgbPixelData
            {
                R = rgb.X,
                G = rgb.Y,
                B = rgb.Z
            };
        }

        public static LinearRgbPixelData ToLinearRgb(this CieLabPixelData source)
        {
            return source.ToCieXyz().ToLinearRgb();
        }

        public static CieXyzPixelData ToCieXyz(this StandardRgbPixelData source)
        {
            return source.ToLinearRgb().ToCieXyz();
        }

        public static CieXyzPixelData ToCieXyz(this LinearRgbPixelData source)
        {
            Vector4 rgb = new Vector4((float)source.R, (float)source.G, (float)source.B, 1.0f);
            Vector4 xyz = Vector4.Transform(rgb, linearRgbToCieTransform);

            return new CieXyzPixelData
            {
                X = xyz.X,
                Y = xyz.Y,
                Z = xyz.Z
            };
        }

        public static CieXyzPixelData ToCieXyz(this CieLabPixelData source)
        {
            double X = CieLab.Xn * fInverse((source.L + 16.0) / 116.0 + source.a / 500.0);
            double Y = CieLab.Yn * fInverse((source.L + 16.0) / 116.0);
            double Z = CieLab.Zn * fInverse((source.L + 16.0) / 116.0 - source.b / 200.0);

            return new CieXyzPixelData
            {
                X = X,
                Y = Y,
                Z = Z
            };

            double fInverse(double t)
            {
                return t > CieLab.delta ? t * t * t : 3 * CieLab.delta * CieLab.delta * (t - 4.0 / 29.0);
            }
        }

        public static CieLabPixelData ToCieLab(this StandardRgbPixelData source)
        {
            return source.ToLinearRgb().ToCieLab();
        }

        public static CieLabPixelData ToCieLab(this LinearRgbPixelData source)
        {
            return source.ToCieXyz().ToCieLab();
        }

        public static CieLabPixelData ToCieLab(this CieXyzPixelData source)
        {
            double L = 116.0 * f(source.Y / CieLab.Yn) - 16.0;
            double a = 500.0 * (f(source.X / CieLab.Xn) - f(source.Y / CieLab.Yn));
            double b = 200.0 * (f(source.Y / CieLab.Yn) - f(source.Z / CieLab.Zn));

            return new CieLabPixelData
            {
                L = L,
                a = a,
                b = b
            };

            double f(double t)
            {
                return t > CieLab.delta * CieLab.delta * CieLab.delta ? Math.Pow(t, 1.0 / 3.0) : t / (3 * CieLab.delta * CieLab.delta) + 4.0 / 29.0;
            }
        }
    }
}
