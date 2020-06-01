using System.Numerics;

namespace KMeansClustering
{
    internal sealed class CieLabColorSpace : IColorSpace
    {
        public string Name => "CIELAB";

        public Vector3 ConvertFromStandardRgb(StandardRgbColor pixel)
        {
            return (Vector3)pixel.ToCieLab();
        }

        public StandardRgbColor ConvertToStandardRgb(Vector3 pixel)
        {
            CieLabColor labPixel = (CieLabColor)pixel;
            return labPixel.ToStandardRgb();
        }
    }

    internal struct CieLabColor
    {
        public float L;
        public float a;
        public float b;

        public static explicit operator Vector3(CieLabColor source)
        {
            return new Vector3(source.L, source.a, source.b);
        }

        public static explicit operator CieLabColor(Vector3 source)
        {
            return new CieLabColor
            {
                L = source.X,
                a = source.Y,
                b = source.Z
            };
        }
    }
}
