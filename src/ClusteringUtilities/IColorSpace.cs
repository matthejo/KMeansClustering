using System.Numerics;

namespace KMeansClustering
{
    public static class ColorSpaces
    {
        public static readonly IColorSpace Rgb = new StandardRgbColorSpace();
        public static readonly IColorSpace CieLab = new CieLabColorSpace();
        public static readonly IColorSpace CieLuv = new CieLuvColorSpace();
    }

    public interface IColorSpace
    {
        string Name { get; }
        Vector3 ConvertFromStandardRgb(StandardRgbColor pixel);
        StandardRgbColor ConvertToStandardRgb(Vector3 pixel);
    }
}
