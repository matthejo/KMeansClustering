using Newtonsoft.Json;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace KMeansClustering
{
    internal sealed class StandardRgbColorSpace : IColorSpace
    {
        public string Name => "sRGB";

        public Vector3 ConvertFromStandardRgb(StandardRgbColor pixel)
        {
            return (Vector3)pixel;
        }

        public StandardRgbColor ConvertToStandardRgb(Vector3 pixel)
        {
            return (StandardRgbColor)pixel;
        }
    }

    [JsonConverter(typeof(StandardRgbColorConverter))]
    public struct StandardRgbColor
    {
        public byte R;
        public byte G;
        public byte B;

        public static explicit operator Vector3(StandardRgbColor source)
        {
            return new Vector3(source.R, source.G, source.B);
        }

        public static explicit operator StandardRgbColor(Vector3 source)
        {
            return new StandardRgbColor
            {
                R = (byte)Math.Max(0, Math.Min(255, Math.Round(source.X))),
                G = (byte)Math.Max(0, Math.Min(255, Math.Round(source.Y))),
                B = (byte)Math.Max(0, Math.Min(255, Math.Round(source.Z)))
            };
        }

        public override string ToString()
        {
            return Color.FromRgb(R, G, B).ToString();
        }

        public static StandardRgbColor Parse(string v)
        {
            if (!TryParse(v, out StandardRgbColor color))
            {
                throw new InvalidOperationException();
            }

            return color;
        }

        public static bool TryParse(string raw, out StandardRgbColor color)
        {
            if (ColorConverter.ConvertFromString(raw) is Color c)
            {
                color.R = c.R;
                color.G = c.G;
                color.B = c.B;
                return true;
            }
            else
            {
                color = default(StandardRgbColor);
                return false;
            }
        }
    }

    internal class StandardRgbColorConverter : JsonConverter<StandardRgbColor>
    {
        public override StandardRgbColor ReadJson(JsonReader reader, Type objectType, StandardRgbColor existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return StandardRgbColor.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, StandardRgbColor value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
