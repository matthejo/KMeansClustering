﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansClustering
{
    public sealed class WeightedColor
    {
        public int PixelCount { get; set; }
        public StandardRgbColor Color { get; set; }

        public WeightedColor(int pixelCount, StandardRgbColor color)
        {
            PixelCount = pixelCount;
            Color = color;
        }
    }

    public sealed class WeightedColorSet
    {
        public List<WeightedColor> Colors { get; set; }
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }

        public int PixelCount => PixelWidth * PixelHeight;

        public WeightedColorSet(int pixelWidth, int pixelHeight, List<WeightedColor> colors)
        {
            Colors = colors;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }
    }
}
