using System.Numerics;

namespace KMeansClustering
{
    internal static class PixelRepresentations
    {
        public static readonly StandardRgbPixelRepresentation Rgb = new StandardRgbPixelRepresentation();
        public static readonly CieLabPixelRepresentation CieLab = new CieLabPixelRepresentation();
        public static readonly CieLuvPixelRepresentation CieLuv = new CieLuvPixelRepresentation();
    }

    internal interface IPixelRepresentation
    {
        Vector3 ConvertFromStandardRgb(StandardRgbPixelData pixel);
        StandardRgbPixelData ConvertToStandardRgb(Vector3 pixel);
    }
}
