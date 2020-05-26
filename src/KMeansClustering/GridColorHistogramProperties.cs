using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KMeansClustering
{
    public static class GridColorHistogramProperties
    {
        public static readonly DependencyProperty ColumnWidthsProperty = DependencyProperty.RegisterAttached("ColumnWidths", typeof(IList<int>), typeof(GridColorHistogramProperties), new PropertyMetadata(OnColumnWidthsChanged));

        public static IList<int> GetColumnWidths(DependencyObject o)
        {
            return (IList<int>)o.GetValue(ColumnWidthsProperty);
        }

        public static void SetColumnWidths(DependencyObject o, IList<int> value)
        {
            o.SetValue(ColumnWidthsProperty, value);
        }

        private static void OnColumnWidthsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;
            IList<int> values = (IList<int>)e.NewValue;
            grid.ColumnDefinitions.Clear();

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(values[i], GridUnitType.Star)
                    });
                }
            }
        }

        public static readonly DependencyProperty ColorValuesProperty = DependencyProperty.RegisterAttached("ColorValues", typeof(IList<Color>), typeof(GridColorHistogramProperties), new PropertyMetadata(OnColorValuesChanged));

        public static IList<Color> GetColorValues(DependencyObject o)
        {
            return (IList<Color>)o.GetValue(ColorValuesProperty);
        }

        public static void SetColorValues(DependencyObject o, IList<Color> value)
        {
            o.SetValue(ColorValuesProperty, value);
        }

        private static void OnColorValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;
            IList<Color> values = (IList<Color>)e.NewValue;
            grid.Children.Clear();

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    var rectangle = new Rectangle
                    {
                        Fill = new SolidColorBrush(values[i]),
                        Margin = new Thickness(2, 0, 0, 0)
                    };
                    Grid.SetColumn(rectangle, i);
                    grid.Children.Add(rectangle);
                }
            }
        }
    }
}
