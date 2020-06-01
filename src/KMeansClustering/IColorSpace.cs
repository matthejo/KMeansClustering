using System.Numerics;

namespace KMeansClustering
{
    internal static class ColorSpaces
    {
        public static readonly StandardRgbColorSpace Rgb = new StandardRgbColorSpace();
        public static readonly CieLabColorSpace CieLab = new CieLabColorSpace();
        public static readonly CieLuvColorSpace CieLuv = new CieLuvColorSpace();
    }

    internal interface IColorSpace
    {
        string Name { get; }
        Vector3 ConvertFromStandardRgb(StandardRgbColor pixel);
        StandardRgbColor ConvertToStandardRgb(Vector3 pixel);
    }
}
