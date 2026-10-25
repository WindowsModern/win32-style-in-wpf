using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Converts ProgressBar Value/Minimum/Maximum and the track's ActualHeight into
    /// an explicit vertical indicator height.  WPF's ProgressBar normally sizes the
    /// indicator by Width, which is wrong for a custom vertical template.
    /// </summary>
    public sealed class ProgressBarVerticalHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return 0.0;

            double value = ToDouble(values[0]);
            double minimum = ToDouble(values[1]);
            double maximum = ToDouble(values[2]);
            double trackHeight = ToDouble(values[3]);

            if (maximum <= minimum || trackHeight <= 0)
                return 0.0;

            double progress = (value - minimum) / (maximum - minimum);
            if (progress < 0)
                progress = 0;
            if (progress > 1)
                progress = 1;

            return progress * trackHeight;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }

        private static double ToDouble(object value)
        {
            if (value == null)
                return 0.0;

            try
            {
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return 0.0;
            }
            catch (InvalidCastException)
            {
                return 0.0;
            }
        }
    }
}
