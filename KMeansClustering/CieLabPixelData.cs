using System.Numerics;

namespace KMeansClustering
{
    internal sealed class CieLabPixelRepresentation : IPixelRepresentation
    {
        public Vector3 ConvertFromStandardRgb(StandardRgbPixelData pixel)
        {
            return (Vector3)pixel.ToCieLab();
        }

        public StandardRgbPixelData ConvertToStandardRgb(Vector3 pixel)
        {
            CieLabPixelData labPixel = (CieLabPixelData)pixel;
            return labPixel.ToStandardRgb();
        }
    }

    internal struct CieLabPixelData
    {
        public float L;
        public float a;
        public float b;

        public static explicit operator Vector3(CieLabPixelData source)
        {
            return new Vector3(source.L, source.a, source.b);
        }

        public static explicit operator CieLabPixelData(Vector3 source)
        {
            return new CieLabPixelData
            {
                L = source.X,
                a = source.Y,
                b = source.Z
            };
        }
    }
}
