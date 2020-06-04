using System.Numerics;

namespace KMeansClustering
{
    internal sealed class CieLuvColorSpace : IColorSpace
    {
        public string Name => "CIELUV";

        public Vector3 ConvertFromStandardRgb(StandardRgbColor pixel)
        {
            return (Vector3)pixel.ToCieLuv();
        }

        public StandardRgbColor ConvertToStandardRgb(Vector3 pixel)
        {
            CieLuvColor cieLuv = (CieLuvColor)pixel;
            return cieLuv.ToStandardRgb();
        }
    }

    public struct CieLuvColor
    {
        public float L;
        public float u;
        public float v;

        public static explicit operator Vector3(CieLuvColor source)
        {
            return new Vector3(source.L, source.u, source.v);
        }

        public static explicit operator CieLuvColor(Vector3 source)
        {
            return new CieLuvColor
            {
                L = source.X,
                u = source.Y,
                v = source.Z
            };
        }
    }
}
