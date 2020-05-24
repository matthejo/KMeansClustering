using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    internal struct LinearRgbPixelData
    {
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

        public double R;
        public double G;
        public double B;

        public CieXyzPixelData ToCieXyz()
        {
            Vector4 rgb = new Vector4((float)R, (float)G, (float)B, 1.0f);
            Vector4 xyz = Vector4.Transform(rgb, linearRgbToCieTransform);

            return new CieXyzPixelData
            {
                X = xyz.X,
                Y = xyz.Y,
                Z = xyz.Z
            };
        }

        public RgbPixelData ToStandardRgb()
        {
            return new RgbPixelData
            {
                R = (byte)Math.Round(convertLinearToGamma(R) * 255),
                G = (byte)Math.Round(convertLinearToGamma(G) * 255),
                B = (byte)Math.Round(convertLinearToGamma(B) * 255)
            };

            double convertLinearToGamma(double u)
            {
                return u <= 0.003308 ? u * 12.92 : 1.055 * Math.Pow(u, 5.0 / 12.0) - 0.055;
            }
        }
    }
}
