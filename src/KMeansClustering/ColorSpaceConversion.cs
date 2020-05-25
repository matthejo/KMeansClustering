using System;
using System.Numerics;

namespace KMeansClustering
{
    internal static class ColorSpaceConversion
    {
        private static class CieConstants
        {
            public const float Xn = 95.0489f;
            public const float Yn = 100.0f;
            public const float Zn = 108.8840f;
            public const float delta = 6.0f / 29.0f;
            public const float uN = 0.2009f;
            public const float vN = 0.4610f;
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

        public static StandardRgbColor ToStandardRgb(this LinearRgbColor source)
        {
            return new StandardRgbColor
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

        public static StandardRgbColor ToStandardRgb(this CieXyzColor source)
        {
            return source.ToLinearRgb().ToStandardRgb();
        }

        public static StandardRgbColor ToStandardRgb(this CieLabColor source)
        {
            return source.ToCieXyz().ToStandardRgb();
        }

        public static StandardRgbColor ToStandardRgb(this CieLuvColor source)
        {
            return source.ToCieXyz().ToStandardRgb();
        }

        public static LinearRgbColor ToLinearRgb(this StandardRgbColor source)
        {
            float sR = source.R / 255.0f;
            float sG = source.G / 255.0f;
            float sB = source.B / 255.0f;

            return new LinearRgbColor
            {
                R = convertGammaToLinear(sR),
                G = convertGammaToLinear(sG),
                B = convertGammaToLinear(sB)
            };

            float convertGammaToLinear(float u)
            {
                return u <= 0.04045f ? u / 12.92f : (float)Math.Pow((u + 0.055) / 1.055, 2.4);
            }
        }

        public static LinearRgbColor ToLinearRgb(this CieXyzColor source)
        {
            Vector4 xyz = new Vector4((float)source.X, (float)source.Y, (float)source.Z, 1.0f);
            Vector4 rgb = Vector4.Transform(xyz, cieToLinearRgbTransform);

            return new LinearRgbColor
            {
                R = rgb.X,
                G = rgb.Y,
                B = rgb.Z
            };
        }

        public static LinearRgbColor ToLinearRgb(this CieLabColor source)
        {
            return source.ToCieXyz().ToLinearRgb();
        }

        public static LinearRgbColor ToLinearRgb(this CieLuvColor source)
        {
            return source.ToCieXyz().ToLinearRgb();
        }

        public static CieXyzColor ToCieXyz(this StandardRgbColor source)
        {
            return source.ToLinearRgb().ToCieXyz();
        }

        public static CieXyzColor ToCieXyz(this LinearRgbColor source)
        {
            Vector4 rgb = new Vector4((float)source.R, (float)source.G, (float)source.B, 1.0f);
            Vector4 xyz = Vector4.Transform(rgb, linearRgbToCieTransform);

            return new CieXyzColor
            {
                X = xyz.X,
                Y = xyz.Y,
                Z = xyz.Z
            };
        }

        public static CieXyzColor ToCieXyz(this CieLabColor source)
        {
            float X = CieConstants.Xn * fInverse((source.L + 16.0f) / 116.0f + source.a / 500.0f);
            float Y = CieConstants.Yn * fInverse((source.L + 16.0f) / 116.0f);
            float Z = CieConstants.Zn * fInverse((source.L + 16.0f) / 116.0f - source.b / 200.0f);

            return new CieXyzColor
            {
                X = X,
                Y = Y,
                Z = Z
            };

            float fInverse(float t)
            {
                return t > CieConstants.delta ? t * t * t : 3 * CieConstants.delta * CieConstants.delta * (t - 4.0f / 29.0f);
            }
        }

        public static CieXyzColor ToCieXyz(this CieLuvColor source)
        {
            float uPrime = source.u / (13.0f * source.L) + CieConstants.uN;
            float vPrime = source.v / (13.0f * source.L) + CieConstants.vN;

            float Y = source.L <= 8 ? CieConstants.Yn * source.L * (3.0f / 29.0f) * (3.0f / 29.0f) * (3.0f / 29.0f) : CieConstants.Yn * (float)Math.Pow((source.L + 16.0) / 116.0, 3);
            float X = Y * (9.0f * uPrime) / (4.0f * vPrime);
            float Z = Y * (12.0f - 3 * uPrime - 20.0f * vPrime) / (4.0f * vPrime);

            return new CieXyzColor
            {
                X = X,
                Y = Y,
                Z = Z
            };
        }

        public static CieLabColor ToCieLab(this StandardRgbColor source)
        {
            return source.ToLinearRgb().ToCieLab();
        }

        public static CieLabColor ToCieLab(this LinearRgbColor source)
        {
            return source.ToCieXyz().ToCieLab();
        }

        public static CieLabColor ToCieLab(this CieXyzColor source)
        {
            float L = 116.0f * f(source.Y / CieConstants.Yn) - 16.0f;
            float a = 500.0f * (f(source.X / CieConstants.Xn) - f(source.Y / CieConstants.Yn));
            float b = 200.0f * (f(source.Y / CieConstants.Yn) - f(source.Z / CieConstants.Zn));

            return new CieLabColor
            {
                L = L,
                a = a,
                b = b
            };

            float f(float t)
            {
                return t > CieConstants.delta * CieConstants.delta * CieConstants.delta ? (float)Math.Pow(t, 1.0 / 3.0) : t / (3 * CieConstants.delta * CieConstants.delta) + 4.0f / 29.0f;
            }
        }

        public static CieLabColor ToCieLab(this CieLuvColor source)
        {
            return source.ToCieXyz().ToCieLab();
        }

        public static CieLuvColor ToCieLuv(this StandardRgbColor source)
        {
            return source.ToLinearRgb().ToCieLuv();
        }

        public static CieLuvColor ToCieLuv(this LinearRgbColor source)
        {
            return source.ToCieXyz().ToCieLuv();
        }

        public static CieLuvColor ToCieLuv(this CieLabColor source)
        {
            return source.ToCieXyz().ToCieLuv();
        }

        public static CieLuvColor ToCieLuv(this CieXyzColor source)
        {
            const float inflectionPoint = (6.0f / 29.0f) * (6.0f / 29.0f) * (6.0f / 29.0f);
            float inflectionTest = source.Y / CieConstants.Yn;
            float denominator = (source.X + 15.0f * source.Y + 3.0f * source.Z);
            float uPrime = denominator == 0.0f ? 0.0f : (4.0f * source.X) / denominator;
            float vPrime = denominator == 0.0f ? 0.0f : (9.0f * source.Y) / denominator;

            float L = inflectionTest <= inflectionPoint ? (29.0f / 3.0f) * (29.0f / 3.0f) * (29.0f / 3.0f) * source.Y / CieConstants.Yn : 116.0f * (float)Math.Pow(source.Y / CieConstants.Yn, 1.0f / 3.0f) - 16.0f;
            float u = 13.0f * L * (uPrime - CieConstants.uN);
            float v = 13.0f * L * (vPrime - CieConstants.vN);

            return new CieLuvColor
            {
                L = L,
                u = u,
                v = v
            };
        }
    }
}
