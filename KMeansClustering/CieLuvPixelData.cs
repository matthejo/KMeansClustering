using System.Numerics;

namespace KMeansClustering
{
    internal sealed class CieLuvPixelRepresentation : IPixelRepresentation
    {
        public Vector3 ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return (Vector3)pixel.ToCieLuv();
        }

        public StandardRgbPixelData ConvertToStandardRgb(Vector3 pixel)
        {
            CieLuvPixelData cieLuv = (CieLuvPixelData)pixel;
            return cieLuv.ToStandardRgb();
        }
    }

    internal struct CieLuvPixelData
    {
        public float L;
        public float u;
        public float v;

        public static explicit operator Vector3(CieLuvPixelData source)
        {
            return new Vector3(source.L, source.u, source.v);
        }

        public static explicit operator CieLuvPixelData(Vector3 source)
        {
            return new CieLuvPixelData
            {
                L = source.X,
                u = source.Y,
                v = source.Z
            };
        }
    }
}
