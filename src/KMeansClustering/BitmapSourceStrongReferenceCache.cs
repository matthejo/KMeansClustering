using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace KMeansClustering
{
    internal static class BitmapSourceStrongReferenceCache
    {
        private static BitmapSource[] strongCache = new BitmapSource[24];
        private static int nextIndex = 0;

        public static void RefreshReference(BitmapSource bitmapSource)
        {
            strongCache[nextIndex] = bitmapSource;
            nextIndex = (nextIndex + 1) % strongCache.Length;
        }
    }
}
